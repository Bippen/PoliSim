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
| Poland | **86.8** | ⚠ **[ESTIMATED]** — 95% band 78.4–95.2 |
| Italy | **74.4** | ⚠ **[ESTIMATED]** — 95% band 66.8–82.1 |
| Sweden | **62.1** | ⚠ **[ESTIMATED]** — 95% band 54.9–69.4 |

#### 📐 THE ESTIMATE — a four-point regression from the Eurostat population basis

**Rung 3 of the fallback ladder.** The OECD Affordable Housing Database is absent from SDMX; its HM1.3
note is reachable and confirms the basis exactly (*"share of households in different tenure types, in
percent, 2024 or latest year available"*), but the values live in charts and a companion worksheet that
do not parse.

**Four countries have a value on BOTH bases, so the bridge is FITTED rather than assumed** — and that is
the deliberate fix for C5's known weakness, whose France-only bridge missed Germany by 5.6% and Italy by
6.7% exactly where its stated limitation predicted:

| | Eurostat (population) | OECD AHD (households) | fit | residual |
|---|---|---|---|---|
| Switzerland | 42.0 | 38.2 | 36.91 | +1.29 |
| Germany | 47.2 | 41.0 | 42.67 | −1.67 |
| France | 61.2 | 58.5 | 58.16 | +0.34 |
| Slovakia | 93.1 | 93.5 | 93.46 | +0.04 |

```
household = 1.1065 × population − 9.5604      R² = 0.9977, residual sd 1.51 pp, df = 2
```

**All seven Eurostat inputs re-verified against the API 2026-08-02** (`ilc_lvho02`, `tenure=OWN`,
`hhcomp=TOTAL`, `rskpovth=TOTAL`, `unit=PC`, 2024, no status flags) — FR 61.2, DE 47.2, CH 42.0, SK 93.1,
IT 75.9, SE 64.8, PL 87.1, every one exact. The OECD side of the bridge could not be re-verified; the AHD
is not queryable.

**The relationship has a mechanism, which is why it is trusted at all:** owner households are larger than
renter households, so a population base overstates ownership relative to a household base — and the two
converge as ownership approaches universal. Slovakia's gap is +0.4pp; Germany's is −6.2pp. A fit with a
physical story behind it is worth more than a high R² without one.

🔴 **LEAD WITH THE 95% BAND, NOT THE 68% ONE.** Four calibration points leave two degrees of freedom, so
the formal prediction interval is ±7pp however tight R² looks. **C5's ±3% band was falsified for two of
five countries; quoting ±2.5pp here would repeat that mistake with better arithmetic behind it.**

**No directional correction applied to Sweden, and that is a decision.** The tempting move is to shade
Sweden down by Germany's residual, since Sweden is structurally Germany-like (high-renting, mortgage-heavy).
The evidence does not support it: the two low-ownership countries sit on **opposite** sides of the fit
(Switzerland +1.29, Germany −1.67), so the residuals show no structural pattern to correct for.

⚠ **The real residual risk is VINTAGE, not fit.** The four AHD anchors come from this file and **their year
is not recorded**, while the Eurostat side is 2024. Homeownership moves slowly so the effect is small — but
it is unquantified, and an unrecorded vintage is precisely what produced the 90.86 problem. **Whoever finds
the AHD vintage should record it here.**

⚠ **The old indicative ranges (Italy ~72–73, Sweden ~63–65) were deliberately NOT used**, not even as a
sanity check, per this file's own instruction that they sit on unknown bases. That the estimates land near
them is noted and **must not be treated as corroboration** — agreement between a fitted value and an
unknown-basis one is coincidence until the basis is known.

**Sanity checks that WERE used:** OECD average 70.1 — Italy and Poland above, Sweden below, correct for
these countries. Poland 86.8 below Slovakia's 93.5, consistent with "top 10 globally but not the top".
Sweden below Canada 68.6 and above Switzerland 38.2, consistent with a high-renting Nordic market.

Anchors on the same basis [VERIFIED]: OECD average 70.1, Slovakia highest at 93.5, Canada 68.6, Australia 62.7, Switzerland lowest at 38.2. OECD-wide, 71% of households owned outright or with a mortgage in 2022 versus 24% renting.

**⚠ THREE-WAY BASIS SPREAD — this is why the earlier set was unusable.** Germany appears in sources as:
- **41.0%** — OECD, share of *households* owning
- **~46.7%** — a 2022 dwelling-based figure
- **52.3%** — Eurostat, *nationals only*

An 11.3-point spread across three definitions of "the German homeownership rate," every one correct for its own source. A set mixing these would encode differences that are measurement artifacts rather than real. Eurostat additionally measures share of *population* in owner-occupied dwellings (68.4% EU 2024), a fourth basis again.

USA and France above independently match figures already recorded as OECD-sourced, confirming the basis is coherent — Germany was simply captured from a different one.

#### ⚠ EUROSTAT TENURE FIGURES — A SEPARATE SET ON A DIFFERENT BASIS. DO NOT MERGE INTO THE TABLE ABOVE.

Sourced from the Eurostat API 2026-08-02 **because they were reachable, not because they close C1's gaps
— they do not.** These are the *fourth basis* the warning above names: **share of POPULATION in
owner-occupied dwellings**, not share of HOUSEHOLDS owning. Italy, Sweden and Poland remain `[GAP]` on
C1's OECD household basis.

| Country | Owner | of which: with mortgage | outright | Tenant |
|---|---|---|---|---|
| Italy | **75.9** | 12.7 | 63.2 | 24.1 |
| Sweden | **64.8** | 49.6 | 15.2 | 35.2 |
| Poland | **87.1** | 11.7 | 75.4 | 12.9 |

```
https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/ilc_lvho02
    ?lang=EN&rskpovth=TOTAL&hhcomp=TOTAL&unit=PC&tenure=OWN&tenure=OWN_L&tenure=OWN_NL&tenure=RENT
    &geo=IT&geo=SE&geo=PL&time=2024
```

| Dimension | Code | Returned label |
|---|---|---|
| `rskpovth` | `TOTAL` | Total |
| `hhcomp` | `TOTAL` | Total |
| `tenure` | `OWN` / `OWN_L` / `OWN_NL` / `RENT` | Owner / with mortgage or loan / no outstanding mortgage / Tenant |
| `unit` | `PC` | Percentage |

**No status flags.** Dataset updated 2026-06-11.

**Gated on internal arithmetic, since no Eurostat tenure anchor exists in this file.** With no known value
to reproduce, the decode was proven structurally instead: `OWN_L + OWN_NL = OWN` and `OWN + RENT = 100`
both hold exactly for all three countries. A mis-decoded position would break those identities. *This is
the substitute gate when the anchor gate is unavailable — a weaker check than an anchor, and recorded as
such.*

**Poland 87.1 sits directly beside the file's existing `[PARTIAL]` note** that Poland is "confirmed in the
global top 10" with "87.9 (nationals)" — 87.1 is the whole-population figure against 87.9 for nationals
only, which is a coherent pair rather than a contradiction, and confirms the top-10 claim.

**Sweden's split is the interesting one and worth preserving:** 64.8% own, but 49.6 of that carries a
mortgage against only 15.2 outright — the inverse of Italy (12.7 mortgaged, 63.2 outright) and Poland
(11.7 / 75.4). Same headline ownership rate, completely different household balance sheets, which is a
real difference a fiscal model could plausibly care about.

**🔴 `ilc_lvho02` IS WHERE THE "TWO ADULTS" VARIANT ACTUALLY LIVES.** Its `hhcomp` dimension carries 17
household compositions including `A2="Two adults"`. The overburden correction above was right that
`ilc_lvho07a` has no such dimension — this is the dataset that does. Anyone chasing that old note now has
somewhere real to look.

**Poland caution:** the ~87.9% figure elsewhere in this file is a Eurostat *nationals* line, not the OECD household basis. Directionally right (Poland genuinely is among the highest globally) but not a same-basis value.

**House Price Index:** [GAP] — no per-country figures sourced yet. Recommend seeding all six at an index value of 100 at game start (a standard index convention) and letting divergence emerge from simulation, rather than inventing differing starting levels. This is honest and avoids fake precision.

### 2. Inequality — Gini coefficient
| Country | Value | Confidence |
|---|---|---|
| Italy | 32.2 | [VERIFIED] Eurostat 2024, equivalised disposable income |
| France | 30.0 | [VERIFIED] Eurostat 2024 |
| Germany | 29.5 | [VERIFIED] Eurostat 2024 |
| Sweden | 27.6 | [VERIFIED] Eurostat 2024 |
| Poland | **26.0** | [VERIFIED] Eurostat API 2026-08-02 — **replaces a [PARTIAL] Statista ~29, which was 3 points too high** |
| USA | **39.5** | ⚠ **[ESTIMATED]** — OECD IDD, disposable income (post-tax post-transfer), **reference year 2019** carried forward. Band 38.5–41.0 |

**METHODOLOGY WARNING:** the Eurostat figures are equivalised disposable income on a 0–100 scale. US figures commonly appear on a 0–1 scale and from a different source (OECD/World Bank) with different methodology. Normalize to one scale and document which, or the US will look artificially different for measurement reasons rather than real ones.

**✅ THE SCALE HALF OF THAT WARNING IS CLOSED (2026-08-02).** 39.5 is already on Eurostat's 0–100 scale
(OECD IDD publishes 0.395), so **the seed needs no conversion step** — which removes the most likely place
for a factor-of-100 error. It confirms rather than replaces the old "~0.39–0.40".

⚠ **`[ESTIMATED]`, not `[VERIFIED]`, for two separate reasons — neither is fixable by finding a better number:**

1. **Reference year 2019**, carried forward to the seed year. Rung 2 of the fallback ladder was the newest
   reachable; the carry-forward is what makes it rung 3.
2. **🔴 THE EQUIVALENCE SCALES DIFFER AND CANNOT BE RECONCILED.** OECD IDD uses the **square-root** scale;
   Eurostat EU-SILC uses the **modified-OECD** scale. The two produce different Ginis *from identical
   data*. The US figure is therefore comparable **in spirit** to the five Eurostat figures, not identical
   in construction. Under a point of difference, but a real one, and it must be documented rather than
   assumed away — this is exactly the "correct figures, incoherent set" trap this file keeps finding.

**What survives the caveat:** USA ~39.5 against Italy 32.2, France 30.0, Germany 29.5, Sweden 27.6, Poland
26.0, EU average 29.4. **The US gap is far larger than any equivalence-scale artefact**, so the qualitative
claim the game needs — the US is a distinct outlier on inequality — is safe even though the exact number
is not.

EU average: 29.4; Euro area: 29.9 (2024) [VERIFIED] — useful sanity anchors.

**✅ POLAND CLOSED, AND ALL FOUR EXISTING FIGURES CONFIRMED (2026-08-02).**

```
https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/ilc_di12
    ?lang=EN&age=TOTAL&statinfo=GINI_HND&geo=PL&geo=IT&geo=FR&geo=DE&geo=SE&time=2024
```

| Dimension | Code | Returned label |
|---|---|---|
| `age` | `TOTAL` | Total |
| `statinfo` | `GINI_HND` | **Gini coefficient (scale from 0 to 100)** — the scale question answered by the source itself |
| `time` | `2024` | 2024 |

**No status flags.** Dataset updated 2026-06-08. **Germany 29.5, France 30.0, Italy 32.2 and Sweden 27.6
all reproduced exactly** — four anchors in the one query, which both confirms those figures against the
primary source and proves the index decode for Poland's position.

⚠ **The Statista figure was 3 points too high, and the error had a direction.** ~29 placed Poland *at* the
EU average (29.4); the true 26.0 places it **well below** — and below Sweden's 27.6, making Poland the
most equal of the five European countries in this set rather than a middling one. That is a different
country to model. **The file's own instruction to "prefer a Eurostat figure if one can be obtained" was
right, and the reason it was right is now measurable.**

**Note the scale trap is closed at source:** `statinfo=GINI_HND` returns the label *"Gini coefficient
(scale from 0 to 100)"*, so the 0–100 vs 0–1 confusion the methodology warning describes is settled by the
API rather than by convention. The USA figure remains on a different source and basis — the warning below
still applies to it.

### 3. Youth unemployment rate (%, under 25, share of labour force)

**AUDITED AND RE-SOURCED FROM THE API 2026-08-02. This is the REVISION case, not the error case** — see
the audit note below, and rule 5f-bis on why the two get opposite treatment.

| Country | Jun 2025 | Feb 2026 | Confidence |
|---|---|---|---|
| Italy | **20.0** | 17.7 | [VERIFIED] Eurostat API — *was recorded 20.1; revised* |
| France | **19.0** | 21.1 | [VERIFIED] Eurostat API — *was recorded 18.7; revised* |
| Germany | **6.9** | 7.3 | [VERIFIED] Eurostat API — closes a `[GAP]` |
| Poland | **12.2** | 11.9 | [VERIFIED] Eurostat API — closes a `[GAP]` |
| Sweden | 23.5 | **22.5** | [VERIFIED] Eurostat API — *was recorded 22.2 for Feb 2026; revised* |
| USA | **10.0** | **9.5** | [VERIFIED] BLS CPS `LNS14024887` via FRED, 2026-08-02 — **16–24, see below** |

**✅ C3's last gap closed 2026-08-02.** BLS Current Population Survey, series `LNS14024887`, both reference
periods. Rate (% of labour force) ✅ and seasonally adjusted ✅ — matching the five Eurostat figures on
both axes that matter most.

⚠ **The age bracket differs and must NOT be "corrected" later: US is 16–24, Eurostat is 15–24.** This is
not a variant error. US labour-force statistics do not cover under-16s at all, so 16–24 *is* the
OECD-harmonised US equivalent — there is no 15–24 US figure to find. **Record the bracket beside the
value**; a future session that spots the mismatch and "fixes" it will be manufacturing a number.

⚠ **October 2025 is missing at source** (federal shutdown gap), not a broken pull. Worth knowing before
anyone re-pulls the series and concludes something is wrong.

**Sanity:** USA 10.0 sits just below the file's OECD-wide anchor of 11.2% (Jul 2025), far below Sweden
23.5 / Italy 20.0 / France 19.0, above Germany 6.9. Coherent across all six.

```
https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/une_rt_m
    ?lang=EN&age=Y_LT25&unit=PC_ACT&sex=T&s_adj=SA&geo=DE&geo=PL&geo=IT&geo=FR&time=2025-06&time=2026-02
```

| Dimension | Code | Returned label |
|---|---|---|
| `age` | `Y_LT25` | Less than 25 years |
| `unit` | `PC_ACT` | **Percentage of population in the labour force** — i.e. the RATE |
| `sex` | `T` | Total |
| `s_adj` | `SA` | Seasonally adjusted data, not calendar adjusted data |

**No status flags on any value.** Dataset updated 2026-07-31.

**🔴 The rate/ratio trap is now closed by construction, not by care.** `unit=PC_ACT` *is* Eurostat's rate
definition — percentage of the labour force, not of the population. The file previously warned that
Germany 3.6 and Poland 3.5 encountered during sourcing were **ratios**; the API returns **6.9 and 12.2**
for those countries on the rate basis, roughly double, exactly as the warning predicted. Stating `unit`
explicitly makes the wrong measure unreachable rather than merely discouraged.

⚠ **`s_adj` was the undeclared dimension, and it matters more than expected.** The old entries recorded
neither adjustment nor a query. For Italy in June 2025 the two variants read **SA 20.0 vs NSA 21.9** — a
1.9-point spread, larger than any revision, and either could have been written down as "the" figure. SA is
the correct choice (it is what Eurostat headlines and what the old figures were closest to) but that was
established by testing both against the anchors, not assumed.

#### Audit result: REVISION, not error — and why that verdict differs from life expectancy

Neither Italy 20.1 nor France 18.7 nor Sweden 22.2 reproduces exactly. All three are nevertheless treated
as *originally sound and since revised*, on this evidence:

- **They sit 0.1–0.3 from the current values at the exact months claimed** (IT 20.1→20.0, FR 18.7→19.0,
  SE 22.2→22.5). Life expectancy's 84.1 was 0.4 off and matched **nothing anywhere**.
- **The variant is right.** They align with SA, not NSA — a 1.9-point difference for Italy — so whoever
  sourced them pulled the correct series.
- **Every qualitative claim survives** *on the Jun 2025 cross-section these anchors belong to*: Sweden
  genuinely high (22–25 across the year), Italy around 20, France around 19, Germany strikingly low.
  ⚠ **The Italy-above-France ordering is specific to Jun 2025 and reverses by Feb 2026** — see the ruling
  above. Sweden-highest and Germany-lowest hold on both.

**Monthly unemployment is a revisable series, so a value without a vintage is incomplete.** Both retrieval
date and reference period are now recorded above; the earlier entries had a period but no retrieval date,
which is why nobody could tell revision from error until the primary source was reachable.

### ✅ RULED (Elias, 2026-08-02): **SEED FEBRUARY 2026.** One period, all six.

| Germany | USA | Poland | Italy | France | Sweden |
|---|---|---|---|---|---|
| 7.3 | **9.5** | 11.9 | 17.7 | 21.1 | 22.5 |

USA is `[VERIFIED]` on this period — BLS CPS `LNS14024887`, 16–24, rate, seasonally adjusted.
⚠ Feb 2026 is the less settled vintage; **record the retrieval date (2026-08-02)**, per the restated-series
rule.

🔴 **TWO NARRATIVE CLAIMS IN THIS FILE ARE NOW STALE AND WOULD CONTRADICT THE SEEDS.** On the Feb 2026
cross-section **France (21.1) is ABOVE Italy (17.7)** — the reverse of Jun 2025, where Italy 20.0 led
France 19.0. Any prose describing "Italy around 20, France around 19" is describing the *other*
cross-section and must be rewritten with the seeds rather than left to disagree with them.

**What survives unchanged:** Sweden remains highest and Germany lowest, so the counterintuitive Nordic
finding this file wanted preserved — a strong overall labour market alongside one of Europe's worst youth
rates — holds on this period too.

EU average 14.8%, euro area 14.4% (Sept 2025) [VERIFIED] — *not re-checked; a different series to the
per-country figures above.*

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

🔴 **THE TWO PREVIOUS `[VERIFIED]` FIGURES HERE WERE WRONG. Corrected 2026-08-02 from the primary source.**
See the verification-integrity entry below the table — this is the single most important thing on this page.

| Country | Value | Confidence |
|---|---|---|
| USA | **79.0** (2024; up from 78.4 in 2023) | ✅ [VERIFIED] **CDC/NCHS FINAL data** — re-checked 2026-08-02 |
| Italy | **83.7** | [VERIFIED] Eurostat API 2026-08-02 — *replaces an incorrect 84.1* |
| Sweden | **83.8** | [VERIFIED] Eurostat API 2026-08-02 — *replaces an incorrect 84.1* |
| France | **83.0** | ⚠ **[PROVISIONAL]** — status flag `p`. Not `[VERIFIED]` |
| Germany | **81.2** | [VERIFIED] Eurostat API 2026-08-02 — no status flag |
| Poland | **78.5** | ⚠ **[PROVISIONAL]** — status flag `ep` (estimated, provisional). Not `[VERIFIED]` |

```
https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/demo_mlexpec
    ?lang=EN&unit=YR&sex=T&age=Y_LT1&geo=FR&geo=DE&geo=PL&geo=IT&geo=SE&time=2024
```

| Dimension | Code | Returned label |
|---|---|---|
| `unit` | `YR` | Year |
| `sex` | `T` | Total |
| `age` | `Y_LT1` | Less than 1 year *(= at birth)* |
| `time` | `2024` | 2024 |

**France and Poland are NOT `[VERIFIED]`, per the status-flag rule.** `p` and `ep` mean provisional and
estimated-provisional; both will move. They are good enough to seed a game and must not be quoted as
settled figures.

#### 🔴 VERIFICATION-INTEGRITY INSTANCE — a `[VERIFIED]` figure that was simply wrong

**Claim:** *"Italy 84.1, Sweden 84.1, [VERIFIED] Eurostat 2024, joint highest in EU."*
**Reality:** Italy 83.7, Sweden 83.8, and neither leads the EU — **Spain does, at 84.0** (Liechtenstein and
Switzerland read 84.2 but are EFTA, not EU).

Every part of that entry was wrong: both values, the claim they were equal, and the claim either was
highest. Three independent checks, and the third is what made the verdict safe:

1. **84.1 appears in no year** for either country — Italy 2021→24 runs 82.6, 82.8, 83.3, 83.7.
2. **84.1 appears nowhere in the entire 2024 cross-section**, for any country in the dataset.
3. **The structural claim failed on its own terms** — Italy 83.7 ≠ Sweden 83.8, so "joint" was false
   independently of what the right numbers were.

**Root cause: sourced from a summary article rather than the primary database**, then marked `[VERIFIED]`.
Same class as the `ilc_lvho07a` household-type error recorded above — a plausible structural claim written
alongside a number, where the claim was never checked against the source and the number was quietly wrong.
**A secondary source can be accurate about a number and still invent the frame around it.**

**What makes this catchable now and not before:** the API. A summary article gives one number with no way
to interrogate it; the database gives the whole cross-section, which is how "nowhere in any year, for any
country" became a checkable statement rather than a suspicion.

#### ✅ USA 79.0 RE-CHECKED AND CONFIRMED (2026-08-02) — the guilt by association is discharged

**CDC/NCHS, *Mortality in the United States, 2024*, NCHS Data Brief No. 548 — FINAL mortality data, not
provisional.** 79.0 years, total population, up 0.6 from 78.4 in 2023, which this file also records
correctly. Female 81.4, male 76.5. Source: National Vital Statistics System.

It was flagged only because it came from the sourcing session that produced the 84.1 error. **It is exactly
right, on the right basis, from the right agency** — which is itself evidence that the 84.1 failure was a
single bad secondary source rather than a session-wide problem.

🔴 **AND IT IS THE STRONGEST FIGURE IN THIS ROW, which the file should say plainly.** France 83.0 carries
`p` and Poland 78.5 carries `ep` — both provisional, both expected to move. **The USA is the only fully
final figure here.** The habitual assumption that the US number is the shaky one because it comes from
outside Eurostat is, in this row, backwards.

⚠ **ONE CLAIM REMOVED, NOT CONFIRMED: "highest-ever".** The brief states the 0.6-year rise from 2023 and
says nothing about a record. It has been dropped rather than left standing — **an unchecked structural
claim written alongside a correct number is precisely the shape of the 84.1 failure**, and repeating that
pattern in the very entry that corrects it would be indefensible. Re-add it only with a source.

EU average: 81.7 (2024) [VERIFIED] — *not re-checked against the API; it was not one of the disputed
values.* The US sitting ~3 years below comparable countries is real and worth preserving.

### 5. Real wage growth (%, 2024)
| Country | Value | Confidence |
|---|---|---|
| Poland | 9.0 | [VERIFIED] EU DG EMPL — among the EU's biggest increases, alongside Romania (10.2) and Hungary (8.7); driven by strong nominal growth (12.3%) plus rapidly falling inflation |
| Italy | 2.7 | [VERIFIED] OECD Taxing Wages 2025 — highest among Europe's five largest economies |
| Germany | 2.2 | [VERIFIED] OECD Taxing Wages 2025 |
| France | 0.7 | [VERIFIED] OECD Taxing Wages 2025 — lowest among the major economies |
| Sweden | **1.3** | ⚠ **[ESTIMATED]** — nominal 4.1% (Medlingsinstitutet, whole economy, full-year 2024) minus 2.84% KPI annual average. The nominal figure is sourced, the deflator is secondary, **the subtraction is derived** |
| USA | **1.0** | [VERIFIED] BLS Real Earnings (released 2025-01-15) — real average hourly earnings, all employees, Dec 2023→Dec 2024, SA, CPI-U deflated |

### 🔴 THIS ROW NOW MIXES THREE BASES AND MUST NOT BE SEEDED AS-IS

Every figure is correct; the **set** is incoherent — the same class of defect as the housing-overburden
variant error, and worth as much attention:

| Countries | Source | What it actually measures |
|---|---|---|
| Italy 2.7, Germany 2.2, France 0.7 | OECD Taxing Wages 2025 | real **net (after-tax)** wage, single worker at average earnings |
| Poland 9.0 | EU DG EMPL | economy-wide real wage |
| USA 1.0, Sweden 1.3 | BLS / Medlingsinstitutet | economy-wide real **gross** average earnings |

**These are not interchangeable: a tax change moves the first and not the third.** Recommendation:
re-source all six from **OECD Taxing Wages 2025**, which covers every OECD country on one basis. Its
country notes are `robots.txt`-blocked, but the underlying data is in SDMX — reachable from a session with
OECD access, which is the same unlock C5's anchor needs.

⚠ **Sweden's deflator choice is a factor-of-two decision, and it is sourced.** Medlingsinstitutet's own
15-month figure for the same agreement period reads **3.8% excluding housing interest costs vs 1.9%
including them**. Sweden's KPI includes mortgage interest where most countries' headline indices do not,
and 2023–24 is exactly when Swedish mortgage rates moved hardest. **The 1.3% above is the KPI
(interest-inclusive) basis; a KPIF basis would be materially higher.** Record which, or the number means
nothing. *Cross-check: Medlingsinstitutet reports real wages +3.2% for December 2024 alone against a
December KPI of 0.8% — the full-year figure is lower because inflation fell through the year, which is the
expected shape rather than a discrepancy.*

**USA note:** real average **weekly** earnings rose 0.7% against hourly's 1.0%, the gap being a 0.3% fall
in the average workweek — a real effect, not a conflict between two sources.

Useful anchors: OECD average real household income per capita growth 1.8% in 2024 [VERIFIED]. Note Germany and Italy both saw *declining* real household income in 2024 even while real wages rose — wages and household income are different measures; don't conflate them.

### 6. Productivity — GDP per hour worked (USD, PPP)

✅ **ALL SIX SOURCED EXACTLY FROM OECD SDMX, 2026-08-02. One year, one basis, one vintage.**

| Country | **2022** | Confidence |
|---|---|---|
| Germany | **94.54** | ✅ `[VERIFIED]` |
| USA | **90.83** | ✅ `[VERIFIED]` |
| Sweden | **89.95** | ✅ `[VERIFIED]` |
| France | **86.32** | ✅ `[VERIFIED]` |
| Italy | **78.20** | ✅ `[VERIFIED]` |
| Poland | **54.09** | ✅ `[VERIFIED]` |

**Basis, in full — this must never be a bare number again:** `OECD DSD_PDB` · `MEASURE=GDPHRS` ·
`ACTIVITY=_T` · `UNIT_MEASURE=USD_PPP_H` · `PRICE_BASE=V` (current prices) · `FREQ=A` · reference year
**2022** · **live vintage retrieved 2026-08-02**.

🔴 **RECORD THE RETRIEVAL DATE, NOT JUST THE REFERENCE YEAR.** This series restates wholesale — see the
verification-integrity instance below. A value without a retrieval date cannot be distinguished from an
error by any test in this file.

```
https://sdmx.oecd.org/public/rest/data/OECD.SDD.TPS,DSD_PDB@DF_PDB,2.0/
    DEU+SWE+USA+ITA+POL+FRA.A.GDPHRS._T.USD_PPP_H.V.N._Z.PPP?startPeriod=2022&endPeriod=2022
```

**All NINE key dimensions specified**, read from the DSD: `REF_AREA · FREQ=A · MEASURE=GDPHRS ·
ACTIVITY=_T · UNIT_MEASURE=USD_PPP_H · PRICE_BASE=V · TRANSFORMATION=N · ASSET_CODE=_Z ·
CONVERSION_TYPE=PPP`. Labels returned as intended: *"GDP per hour worked"*, *"US dollars per hour, PPP
converted"*, *"Current prices"*, *"Total - all activities"*.

**Seed 2022, not 2024.** It is the newest year with a complete same-basis cross-section for all six —
rung 2 of the fallback ladder. Only France and the USA have 2024 values; mixing them with 2022 figures
for the other four would fabricate a cross-section that never existed on any single date, which is the
rule this file already states for youth unemployment.

#### 🟢 THE ANCHOR WAS FOUND, AND 90.86 WAS RIGHT ALL ALONG (2026-08-02, session 4)

**`[PRIMARY-UNANCHORED]` is retired for these six.** Rule 5f-bis condition 2 is satisfied: **both**
original seeds reproduce exactly against an independent archive of the same key.

| Seed in this file | Archived OECD series | |
|---|---|---|
| France **90.86** (2024) | France 2024 = **90.8595608458969** | ✅ exact to 2dp |
| USA **~97** | USA 2023 = **97.0466946503153** | ✅ |

**Everything about the original sourcing now makes sense, including the bit that looked like
carelessness.** The seed carried a year label for France and none for the USA **because the USA series
ends at 2023 in that vintage while France runs to 2024.** Whoever sourced these pulled one coherent
cross-section and labelled it honestly.

**The route — record it, it is the only working path found in four sessions.** `db.nomics.world`
(CEPREMAP) mirrors OECD SDMX with its own ingestion pipeline and dimension mapping, serves plain HTML that
parses, and is reachable where `sdmx.oecd.org` is not. Append `?tab=table` to a series URL for every
observation at full precision. ⚠ `api.db.nomics.world` is `robots.txt`-blocked; `db.nomics.world` is not.

```
https://db.nomics.world/OECD/DSD_PDB@DF_PDB_LV/FRA.A.GDPHRS._T.USD_PPP_H.V._Z._Z._Z?tab=table
   snapshot retrieved by DBnomics 2026-04-07
```

**Why the live query could not find it: the live series is the SAME series, restated.** Live 2026-08-02
against the 2026-04-07 archive, at 2022 — systematic, one-directional for five of six, USA alone revised
down. That is a national-accounts and PPP-benchmark revision, not a basis difference:

| | live | archive | Δ |
|---|---|---|---|
| Germany | 94.54 | 92.4008 | **+2.32%** |
| Italy | 78.20 | 76.8155 | +1.80% |
| France | 86.32 | 84.8632 | +1.72% |
| Poland | 54.09 | 53.2791 | +1.52% |
| Sweden | 89.95 | 89.0469 | +1.01% |
| USA | 90.83 | 92.2214 | **−1.51%** |

**Confirmed structurally, not just numerically:** the archive reproduces France's idiosyncratic 2021 dip
(83.96 → 82.53 → 84.86) that the live pull described independently, and **old years are revision-stable
as predicted** — live France 2010 reads **56.7042** against the archive's 56.71, a gap of **0.01%** where
2022 differs by 1.72%. *(Queried live 2026-08-02. Not bit-exact, so this corroborates the key rather than
formally gating it — but a different series or basis would differ by percent, not by a hundredth.)*

**France 90.86 and USA ~97 are SUPERSEDED, NOT CORRECTED.** They were right on their vintage. **Do not log
them as errors** — the error path triggers a re-check of every sibling figure from that sourcing session,
and there is now positive evidence that session was working correctly.

##### The archived vintage, kept as the audit trail

| | 2022 | 2023 | 2024 |
|---|---|---|---|
| Germany | 92.4008 | 93.7210 | 98.3940 |
| USA | 92.2214 | **97.0467** | — *(series ends 2023)* |
| Sweden | 89.0469 | 89.5980 | 95.4554 |
| France | 84.8632 | 87.2951 | **90.8596** |
| Italy | 76.8155 | 77.0889 | 79.3402 |
| Poland | 53.2791 | 54.1595 | 59.2708 |

⚠ **The OECD average "~$67.5/hour (2022)" is still not an anchor** — live gives 72.59 on current prices.
Likely explanation, untested: **67.5 is an unweighted mean across members where the SDMX aggregate is
GDP-weighted.** With Ireland ~151 and Mexico ~25 in the set, the two diverge by about that much. If so it
is not an anchor for the aggregate series at all and should be relabelled rather than re-hunted.

**Sanity checks pass:** all six above the OECD 2022 average of 72.59 except Poland and — narrowly — Italy
at 78.20; ordering Germany > USA > Sweden > France > Italy > Poland is consistent with the qualitative
claims already recorded here (Italian stagnation 2012–2022, Polish catch-up from a low base).

**SOURCE-CONFLICT WARNING on Sweden/Poland:** those two figures come from a different source (Statista) than the USA/France figures (OECD PPP) and are almost certainly NOT PPP-adjusted on the same basis — Poland at $24.5 is implausibly low against an OECD PPP average of $67.5. Do not mix them into one table as-is. Either source all six from OECD PPP consistently, or treat these two as placeholders needing replacement.

#### ⚠ OECD API ATTEMPTED 2026-08-02 — GATE FAILED, NOTHING RECORDED

**`sdmx.oecd.org` IS reachable** (HTTP 200 with `Accept: application/vnd.sdmx.structure+json`), and the
right dataset exists: `OECD.SDD.TPS,DSD_PDB@DF_PDB,2.0`, measure `GDPHRS` *("GDP per hour worked")*, unit
`USD_PPP_H` *("US dollars per hour, PPP converted")*. So C5 is **technically** self-serviceable. It is not
yet **actually** self-serviceable, because the gate did not pass:

| | France | USA |
|---|---|---|
| Seed | 90.86 | ~97 |
| `PRICE_BASE=V` (current prices) | 92.74 | 100.12 |
| `PRICE_BASE=LR` | 81.56 | 84.11 |

`V` is close but reproduces neither. **Not recorded** — per rule 5f, a query that cannot reproduce a known
value is of unknown basis, and "close" is exactly the state in which a wrong variant is most convincing.

**🔴 I WALKED INTO THIS FILE'S OWN RULE 5b, WHICH IS WORTH RECORDING.** The DSD has **nine** key
dimensions; I specified five and left `PRICE_BASE`, `TRANSFORMATION`, `ASSET_CODE` and `CONVERSION_TYPE`
blank. The response came back with multiple variants per country — including a stray USA reading of
**2.24** from some other asset/conversion combination — and **not one label was wrong**, because none of
them was wrong. They were all real OECD figures for combinations I had failed to exclude. This is exactly
the prevention-vs-detection distinction in rule 5c, demonstrated against the person who wrote it down an
hour earlier. **OECD's DSDs are wider than Eurostat's; the dimension count must be read from the DSD, not
assumed from the Eurostat pattern.**

#### FULL SPECIFICATION DONE 2026-08-02. THE GATE STILL FAILS — and the reason is instructive.

The nine-dimension key was read from the DSD and every dimension specified. There are exactly **two**
`USD_PPP_H` series for France, and both were enumerated:

```
https://sdmx.oecd.org/public/rest/data/OECD.SDD.TPS,DSD_PDB@DF_PDB,2.0/
    FRA.A.GDPHRS._T.USD_PPP_H.V.N._Z.PPP        <- current prices, PPP converted
    FRA.A.GDPHRS._T.USD_PPP_H.LR.N._Z.PPP       <- chain linked volume (rebased)
```

| | 2022 | 2023 | 2024 |
|---|---|---|---|
| `V` current prices | 86.32 | **91.18** | **92.74** |
| `LR` chain linked volume | 81.54 | 81.54 | 81.56 |

**The seed's 90.86 appears NOWHERE**: not in France's full `V` series 2010–2024 (56.70 → 92.74,
monotonic apart from 2021), and not in any of the **41 countries** in the 2024 cross-section.

⚠ **NOT RECORDED AS AN ERROR — the revision-vs-error test cannot be completed.** Rule 5f-bis needs both
conditions; condition 1 holds exhaustively, condition 2 fails because **no OECD anchor has ever
reproduced**. See rule 5f-bis's own note on the bootstrapping problem: a first contact with a source
cannot declare an error, by design.

#### 🔴 THE 90.86 PROVENANCE QUESTION — the France story is WEAKENED, Germany is the stronger candidate

**The "pre-revision France 2023" hypothesis had the DIRECTION WRONG.** A secondary OECD-derived series on
an older vintage reads France 2022 = 87.7 and 2023 = 92.8, against SDMX's 86.32 and 91.18 — so the older
vintage runs **above** the current one. A pre-revision France 2023 would therefore be ~92.8, **not**
90.86. Two vintages of France 2023 are now visible, 92.8 and 91.18, and 90.86 is neither.

**Stronger candidate: 90.86 is GERMANY 2022 on the old vintage.** That same secondary series reads
Germany 2022 = **90.9**, which is exactly what 90.86 rounds to at one decimal — country, year and vintage
lining up at once, where the France story needs a value appearing in no known vintage. The adjacent row
in this table was Germany's `[GAP]`, which is precisely where a transcription slip lands.

**Tested 2026-08-02, and the test neither confirms nor refutes it.** Current-vintage Germany 2022 is
**94.54** — so Germany was revised UP ~4% where France was revised DOWN ~1.6%. That is consistent with the
hypothesis (revisions are country-specific, as PPP benchmark updates are) but cannot verify it, **because
the old vintage is not queryable from SDMX.** It would need the OECD Compendium edition the figure
originally came from.

**Status: unresolved, and now unresolvable from the API alone.** Logged here rather than as a
verification-integrity instance, because attributing a `[VERIFIED]` figure to the wrong country is a
serious claim and the evidence is a one-decimal secondary source.

#### The `[ESTIMATED]` C5 set was superseded within hours — and the exercise still paid for itself

Under the fallback ladder (Part 4 rule 4), a session without OECD access built a rung-3 `[ESTIMATED]` set
from a secondary aggregator, bridged to the SDMX vintage by a France-calibrated factor of 0.98426 with a
stated ±3% band. A later session with SDMX access replaced all of it with rung-1b figures. **The
estimates are gone; two findings from them are not:**

| | `[ESTIMATED]` | Actual | Error |
|---|---|---|---|
| Poland | 53.5 | 54.09 | +1.1% ✅ |
| USA | 90.1 | 90.83 | +0.8% ✅ |
| Sweden | 88.8 | 89.95 | +1.3% ✅ |
| **Germany** | 89.5 | **94.54** | **+5.6% ❌** |
| **Italy** | 73.3 | **78.20** | **+6.7% ❌** |

1. **Three of five landed inside the ±3% band; two did not.** The method was sound and the band was too
   narrow — and it failed **precisely where its author said it would**. The estimate's stated weakness was
   that the vintage bridge was calibrated on one country while *"PPP benchmark revisions are
   country-specific"*. Germany's revision (+4%) and France's (−1.6%) differ in sign, so a France-derived
   factor could not describe Germany. **A correctly-labelled limitation predicted the exact failure**,
   which is the strongest argument in this file for stating uncertainty rather than hiding it.
2. **The Sweden/Poland source-conflict warning is now quantified.** Statista's ~70 and ~24.5 against
   OECD's 89.95 and 54.09 — Poland off by more than **2×**. The `[PARTIAL]` markers were right, and the
   scale of the error justifies the file's refusal to mix sources.

**Rung-3 estimates earn their place when access is unavailable, and they are cheap to discard when it
returns.** The failure mode to avoid was never "estimating" — it was estimating *silently*.

**Separately: OECD homeownership on the household basis (C1) does NOT appear to be in SDMX at all.** The
full dataflow list was searched for housing and tenure; it returns regional housing, housing transactions
and *job* tenure, but no Affordable Housing Database equivalent. **C1's OECD-basis gaps for Italy, Sweden
and Poland are therefore NOT closed by API access** and remain Elias's, unless the AHD is published
somewhere outside SDMX.

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
| Poland | ~59% | **A− (S&P) · A2 (Moody's) · A− (Fitch)** — [VERIFIED] 2026-08-02 |

**✅ THE C4 CALIBRATION ANCHOR IS CLOSED (2026-08-02).** Poland sits in the A range, as expected. Outlooks
deteriorated through 2025 — Moody's to negative (2025-09-19), Fitch to negative (Sept 2025), S&P affirming
A− stable (Nov 2025). **Treat the outlooks as far more perishable than the ratings**; the level is the
calibration input, the outlook is a signal with a shelf life.

**KEY INSIGHT for the mapping:** the curve is nearly monotonic in debt-to-GDP — *except the USA*, which carries HIGHER debt than France yet rates BETTER. That's the reserve-currency premium. This project already models exactly that effect (`BaseDebtInterestRateOverride` = 3.3% and reduced `RiskPremiumSensitivity` for USA). The rating derivation should reuse that SAME reserve-currency factor rather than introducing a second, parallel notion of it.

### 🔴 POLAND BREAKS THE MONOTONICITY TOO — in the OPPOSITE direction, and C4 will over-rate it

This anchor is worth more than "one more calibration point". **Poland carries LOWER debt than Germany
(~59% vs ~63%) and rates FOUR NOTCHES WORSE (A− against AAA).** The USA exception is a country rating
*better* than its debt implies; Poland is a country rating *worse*. One factor cannot produce both.

**What this means for the implemented `CreditRatingSystem`:** its curve reads debt-to-GDP through
`RiskPremiumSensitivity`, which is the reserve-currency term — the USA's 0.05 discounts debt above the
reference. **There is no term that penalises**, so a low-debt country cannot rate below the curve, and
Poland will come out near AAA. The missing factor is some combination of currency status (Poland is
outside the euro and borrows partly in it), institutional quality, and an EU-periphery risk premium.

⚠ **Run the 5-anchor calibration as a SIX-anchor calibration and expect it to fail on Poland first.**
`CreditRatingAnchorCheck` currently passes 5 of 5 — that is a statement about five countries, and the
sixth is the one carrying the new information. A check that passes because the hard case was never in it
is the kind of confirmation this project has learned to distrust.

*(This is a finding about the model, not about the data. Logged here because the anchor is what surfaced
it; the work belongs to Step C4's closure.)*

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

   🔴 **AMENDED 2026-08-02 (Elias) — THE FALLBACK LADDER, which overrides the above FOR SEEDING ONLY.**

   > **If data cannot be found for recent years, take the newest available. If still unavailable, estimate.**

   This does **not** override the honesty discipline underneath rule 4; it changes what a gap licenses.
   A game cannot ship a blank, and a blank is not more honest than a labelled approximation. What keeps
   the rule safe is that each rung carries its own marker:

   | Rung | Means | Marker |
   |---|---|---|
   | 1 | Sourced, current year, gate passed | `[VERIFIED]` |
   | 1b | Sourced exactly from the primary database, **but no anchor reproduced in-session** | `[PRIMARY-UNANCHORED]` — see below |
   | 2 | Sourced, but an **older year** than wanted | `[VERIFIED]` *with the year stated* |
   | 3 | **Not sourced** — derived by a stated, reproducible method from stated inputs, with an uncertainty band | `[ESTIMATED]` |

   **`[ESTIMATED]` is never `[VERIFIED]` and is never silently promoted.** It is a placeholder that plays
   correctly, not a fact, and it is replaced the moment a real figure exists.

   **Climb the ladder in order, and record which rung each value came from.** A value on rung 3 that could
   have come from rung 2 is a failure of the rule, not an application of it. *Demonstrated the same day:
   an `[ESTIMATED]` C5 set built on rung 3 was superseded within hours by rung-1b figures once a session
   with SDMX access ran the query — see section 6.*

5. 🔴 **API REACHABILITY IS A PROPERTY OF THE SESSION, NOT OF THE PROJECT.** A previous entry in this file
   read *"the access problem is solved"*. It was true of the session that wrote it and false as a general
   claim: the very next session had **no route to OECD data at all** — `curl` egress proxied to an
   allowlist (`CONNECT tunnel failed, 403`), `WebFetch` returning SDMX as unreadable binary, OECD web
   pages JS-rendered and empty, Compendium PDFs blocked by `robots.txt`, no browser connected.

   **Record which route worked, from where, on what date** — the same discipline this file already applies
   to values. And note the consequence for rule 5f-bis condition 2: a session that cannot run the method
   does not merely fail that condition, **it cannot test it**. Finding an anchor and reproducing it must
   happen in ONE session with live access; splitting that work across sessions cannot close the gate.

   **Routes observed as of 2026-08-02 — a starting point, not a guarantee:**

   | Route | Status | Note |
   |---|---|---|
   | `ec.europa.eu/eurostat/api/...` | ✅ **Works via both `curl` and `WebFetch`** | Returns JSON-stat, which parses cleanly as text |
   | `fred.stlouisfed.org/data/<SERIES>.txt` | ✅ Works | Plain text, full series — the reliable US route |
   | `bls.gov` news releases | ✅ Works | |
   | `webfs.oecd.org` AHD notes | ✅ Works | Basis/definition text only, **not the data** |
   | `sdmx.oecd.org` | ⚠ **Session-dependent** | Worked via `curl` in one session, blocked by proxy allowlist (`CONNECT tunnel failed, 403`) in two others. **`WebFetch` cannot read it at all** — SDMX returns compressed binary |
   | `oecd.org` publication PDFs | ❌ Blocked | `robots.txt`, including the Taxing Wages country notes |
   | `oecd.org` data pages | ❌ Empty | JS-rendered; navigation and metadata only |

   **The generalisable point: JSON-stat survives a text-only fetch and SDMX does not.** Where a provider
   offers both, prefer the JSON-stat endpoint — it is readable from strictly more sessions.

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

**d-bis. 🔴 READ THE DIMENSION COUNT FROM THE DSD. NEVER CARRY IT OVER FROM ANOTHER API.**

#### VERIFICATION-INTEGRITY INSTANCE — the rule was right, understood, and written down an hour earlier

Rule 5c (prevention vs detection) was recorded at roughly 17:00 on 2026-08-02. The OECD productivity query
failed for exactly the reason 5c describes at roughly 17:45, **written by the same session that had just
written the rule.**

**What happened.** `DSD_PDB` has **nine** key dimensions —
`REF_AREA · FREQ · MEASURE · ACTIVITY · UNIT_MEASURE · PRICE_BASE · TRANSFORMATION · ASSET_CODE ·
CONVERSION_TYPE`. Five were specified. The response returned several variants per country, and **not one
label was wrong, because none of them was wrong**: every row was a genuine OECD figure for a combination
that had simply not been excluded. Detection had nothing to detect.

**The tell was a stray USA reading of `2.24`** against an expected ~97 — three orders of magnitude off,
perfectly labelled, sitting in the same response as plausible values. **That is what an unfiltered
dimension looks like**: not an error message, not a bad label, just an extra row that belongs to a
question nobody asked.

**Why knowing the rule did not prevent the mistake.** The rule was learned on Eurostat, whose datasets run
4–7 dimensions and whose omissions were caught by anchors. **OECD's DSDs are wider.** The habit that
transferred was "state the dimensions I know about" — which is not the rule. The rule is "state every
dimension the DSD declares", and the DSD is the only thing that knows how many that is.

**Knowing a rule and applying it under a different API's shape are different skills.** The standing form:

> **Fetch the DSD, count its key dimensions, and specify all of them — every time, per dataset, per API.
> A dimension count is a property of the dataset, never of the provider and never of habit.**

A cheap mechanical check: SDMX rejects a key with the wrong arity outright — *"Not enough key values in
query, expecting 9 got 8"* — so **deliberately sending one too few is a free way to make the API state
its own arity** before building the real query.

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

**f-bis. THE ONE CONDITION UNDER WHICH THE FILE LOSES (Elias, 2026-08-02).** "The query is wrong, not the
file" is the right DEFAULT and must stay the default — it is what stops a bad query rewriting good data.
It is overturned only when **both** hold:

1. **The disputed value appears NOWHERE in the dataset** — not in any year, not for any country, not
   under any variant. *A wrong variant still surfaces its number somewhere; that is exactly what makes
   variant errors detectable. A value absent from the entire source is not a variant mismatch.*
2. **The method has already reproduced other anchors in the same session**, so the technique is known
   good rather than assumed good.

   ⚠ **This condition CANNOT be met on first contact with a new source, by design.** Anchors reproduced
   against one API prove nothing about queries built against another — the OECD attempt below satisfied
   condition 1 exhaustively and still could not declare an error, because no OECD figure had ever been
   reproduced. **A new source therefore starts unable to overturn anything**, and must earn that standing
   by reproducing one known value first. Conservative on purpose: the alternative is letting an unproven
   query rewrite verified data on its first outing.

Both held for the life expectancy 84.1 error below, and corroboration arrived from a third direction (its
"joint highest in EU" claim failed independently). **Neither condition alone is sufficient.**

**f-ter. 🔴 THE REVISION-vs-ERROR TEST (Elias, 2026-08-02). Run this on EVERY mismatch.** A figure that
fails to reproduce is one of two completely different things, and they get opposite treatment:

| | **REVISION** — the seed was right, the source moved | **ERROR** — the seed was never right |
|---|---|---|
| Value vs current | Close, at the **exact period claimed** | Matches nothing |
| Variant | Correct — it aligns with a real series | Often correct too; irrelevant |
| **Elsewhere in the dataset** | n/a | **Appears in NO year and NO cross-section** |
| Qualitative claims | Still hold | Fail independently |
| Treatment | Update the value, keep the vintage, keep confidence | Replace, log as verification-integrity, re-check its siblings |

**🔴 MAGNITUDE ALONE DOES NOT DISTINGUISH THEM, and this is the whole point of the rule.** The evidence
from the day both were found:

- **Youth unemployment: 0.1–0.3 off → REVISION.** Italy 20.1→20.0, France 18.7→19.0, Sweden 22.2→22.5.
- **Life expectancy: 0.4 off → ERROR.** Italy 84.1→83.7 — a *larger* gap, and the wrong one.

**The bigger discrepancy was the honest figure.** Anyone triaging by "how far off is it" would have
reached the wrong verdict on both. The discriminator is not distance, it is **whether the number exists
anywhere in the source at all** — a revision leaves the old value in the historical record or adjacent
periods; an error leaves no trace because there was never anything to leave.

**The failure modes are asymmetric, which is why the test must be run rather than guessed:**
- *Revision misread as error* → good data churned, sourcing effort repeated, confidence markers lowered on
  figures that deserved them.
- *Error misread as revision* → the fiction is preserved, updated to a new wrong value, and its
  `[VERIFIED]` marker renewed. **This is the worse one, because it launders a mistake into fresh
  confidence.**

#### 🔴 CORRECTION 2026-08-02 — THE "APPEARS NOWHERE" DISCRIMINATOR IS FALSE FOR RESTATED LEVEL SERIES

The test above rests on *"a revision leaves the old value in the historical record or adjacent periods;
an error leaves no trace."* **That holds only if the source preserves vintages. It does not for a level
series that gets restated.**

When OECD revises GDP-per-hour it **overwrites every year at once**. The pre-revision value vanishes from
the live API completely — every year, every cross-section. **A correct-but-superseded figure therefore
produces exactly the fingerprint this rule assigns to an ERROR, and the more exhaustively you search the
live API the more confident you become of the wrong verdict.**

That is not hypothetical. France's 90.86 was searched across its full 2010–2024 series *and* a 41-country
cross-section, found nowhere, and judged absent. **Every observation was right; the conclusion was wrong.**
The figure was correct on its own vintage all along.

**What saved it was NOT this test — the test pointed at ERROR. It was the GATE.** Condition 2 could not be
met on first contact with OECD, so the default held and the file won. **The conservative rule that felt
like an obstruction is the only reason a correct `[VERIFIED]` figure was not overwritten.**

> **STANDING FORM: "appears nowhere in the source" distinguishes error from revision ONLY where the source
> preserves vintages. For a restated level series it distinguishes nothing. Check a third-party archive
> with per-snapshot retrieval dates — DBnomics — BEFORE concluding a value never existed.**

**Two hypotheses built on the bad verdict, both dead, both recorded so they are not revived:**
- *"Pre-revision France 2023 ≈ 92.8, so 90.86 is on the wrong side."* The vintage used to reason about
  direction was a **secondary aggregator's** unstated vintage, not OECD's. Against the real archived OECD
  vintage the direction reverses. **A secondary source's vintage cannot calibrate a primary source's
  revision.**
- *"90.86 may be Germany 2022, pre-revision."* Dead — archived Germany 2022 is 92.4008. The 90.9 that made
  it attractive was a one-decimal coincidence in an aggregator, the exact "close but wrong" state rule 5f
  warns about. **Declining to log it as a verification-integrity instance on that evidence was correct.**

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
