# PoliSim — Real-World Seed Data: Macro Data & Release Calendar Overhaul

**Why this file exists:** every real-world figure the Round-4 macro stats and the release calendar
were seeded from, with its source, basis, retrieval date and confidence — so a seed can be audited and
re-sourced rather than trusted. The project's standing discipline is "ground new mechanics in real
data, label anything stylized honestly" (rule 5); `[GAP]` figures are Elias's to source, never to invent.
*(The original "Claude Code has no web search" rationale is history — this file's own 2026-08-02 API
sessions pulled most of the figures live; what it remains is the seed authority `ReleaseCalendar.cs` and
`PublishedData.cs` cite by name. Corrected 2026-08-27.)*

**How to read the confidence markers (seven kinds in use at HEAD, 2026-08-27):**
- `[VERIFIED]` — sourced directly, use as-is
- `[ESTIMATED]` — rung 3 of Part 4's fallback ladder: a stated method with a band, replaced the moment a
  same-basis figure exists (`MISSING_PREREQUISITES.md` §B)
- `[GAP]` — not yet sourced, must NOT be invented; flag to Elias for sourcing before that stat ships
- `[PARTIAL]` — a set with some members sourced and some not (no live data entry carries it today; it
  survives in audit prose)
- `[PROVISIONAL]` — the source's own provisional flag (Eurostat `p`), carried into the seed comment
- `[BOUNDED]` — a range rather than a value; a bound is not a value and must not be seeded as one
- `[DERIVE]` — should be computed from existing tracked state, not seeded as an independent variable

---

## PART 0 — THE BASIS: what unit every money seed in this document is in (C-C6, 2026-08-31)

**Every money seed in this file, and in `WorldFactory`, is in USD BILLIONS — for all six countries,
including the five that do not use dollars.** Measured from the seed itself rather than inferred: USA
29 000 · Germany 4 700 · France 3 200 · Italy 2 300 · Poland 840 · **Sweden 620**, against
`WorldFactory`'s own comment *"Sweden's real GDP (~$620B)"*. Sweden's real GDP is ~6 500 **billion SEK**,
so the stored 620 is dollars — Playtest-1's finding 6 (*"the Desk shows Sweden's GDP as $620B"*) was
reading the basis correctly.

**⚠ The model does not care what the unit is, and a re-basing is still a seed change. Both are true and
`MoneyBasisDiagnostic` measures both.**

| scale | turns | worst ratio difference | worst level difference |
|---|---:|---|---|
| ×2 | 1 | **0.000E+000** | **0.000E+000** |
| ×2 | 12 | **0.000E+000** | **0.000E+000** |
| ×10 | 1 | 4.165E−005 | 1.567E−004 |
| ×10 | 12 | 9.212E−001 | 1.975E+000 |

**(a) Unit-agnostic.** At ×2 — exactly representable in binary floating point — every ratio and every
level is invariant to **zero** at both horizons. **No constant anywhere on the macro path carries an
absolute money scale**, so the stored unit is a *convention*, not a modelling choice.

**(b) But a real re-basing is not a power of two.** SEK/USD is ~10.5, and at ×10 the float path diverges —
small after one turn, order-unity after twelve. **That is rounding, not economics** — but it means a
re-based seed set *would* produce different trajectories.

**So the ruling's cheap branch is not available.** Re-basing to national units is a **seed change under
the full sim-math bar with per-country diffs explained**, and the honest explanation of those diffs is
**float-path divergence, not a change in what the model believes.** ⚠ *"The model is unit-agnostic, so
re-basing is free"* would be true about the model and false about the build.

**Billed, not invented:** re-basing needs a **sourced, vintage-dated FX rate per country** (the ruling's
own words). ~~None is on disk. No rate is authored here.~~

⚠ **FETCHED 2026-09-01 — the rates are REACHABLE, and taking them is still not correct yet.** The ECB's
own daily reference file (`ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml`, HTTP 200) carries, at
**vintage 2026-08-31**: **EUR/USD 1.1596 · EUR/SEK 11.1100 · EUR/PLN 4.3280**. One fetch, one publisher,
one date. The three pairs the six countries need follow by a single stated derivation — cross through the
euro, `USD/X = (EUR/X) ÷ (EUR/USD)`:

| pair | derivation | value |
|---|---|---|
| **USD/SEK** | 11.1100 ÷ 1.1596 | **9.58089** |
| **USD/PLN** | 4.3280 ÷ 1.1596 | **3.73232** |
| **USD/EUR** | 1 ÷ 1.1596 | **0.862366** (Germany, France, Italy) |

⚠ **AND THE VINTAGE IS THE PROBLEM, WHICH IS WHY NOTHING IS SEEDED ON THEM.** A 2026-08-31 rate applied
to seed levels published at **2024 and 2025 vintages** is exactly the **basis-mixing the cross-check gate
forbids** — the same rule that kept the 2022 party-leader set from being half-refreshed. So the bill is
**not discharged; it is SHARPENED.** What it needs is not "an FX rate" but *the rate at each seed's own
vintage* — and the ECB publishes that too (`eurofxref-hist.xml`), so it is obtainable the day this file's
vintages are settled as a set. **No rate is authored here, and none is seeded.**

⚠ **A SECOND CURRENCY IS ALREADY IN THE GAME WITH NO CONVERSION.** The campaign layer prices in
**kronor** — the war chest is 2 400 000 kr, a television buy 500 000, a social post 5 000 — while the
macro layer is in **USD billions**. The two never meet today, because a campaign is staged rather than
funded from the state's budget, so nothing converts and nothing is wrong yet. **The day a campaign is
paid for out of anything the macro model holds, one of the two is wrong by a factor of ~10 500 000 000.**
Recorded because it is invisible until it is expensive.

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
| USA | 65.3 | [VERIFIED] OECD AHD HM1.3 — ACS 2023 (25.98 outright + 39.32 mortgage = 65.31) |
| France | 58.6 | [VERIFIED] OECD AHD HM1.3 — EU-SILC 2024 (34.63 + 23.94 = 58.56; the 58.5 that stood here was this figure rounded down) |
| Germany | 41.0 | [VERIFIED] OECD AHD HM1.3 — EU-SILC 2024 (24.25 + 16.71 = 40.96) — lowest among major economies, a genuine structural outlier |
| Poland | 84.7 | [VERIFIED] OECD AHD HM1.3 — EU-SILC 2024 (74.09 + 10.66 = 84.74); *was `[ESTIMATED]` 86.8, band 78.4–95.2 — inside the band* |
| Italy | 75.2 | [VERIFIED] OECD AHD HM1.3 — EU-SILC 2024 (65.17 + 10.03 = 75.20); *was `[ESTIMATED]` 74.4, band 66.8–82.1 — inside the band* |
| Sweden | 58.2 | [VERIFIED] OECD AHD HM1.3 — EU-SILC 2024 (17.79 + 40.40 = 58.19); *was `[ESTIMATED]` 62.1, band 54.9–69.4 — inside the band* |

**✅ RE-SOURCED 2026-08-28 (R-C5 of the continuation kickoff) — all six from ONE file on ONE basis.** The
Affordable Housing Database's HM1.3 workbook is reachable on the OECD's file host (the www host serves
a bot-check page; the `webfs` host serves the file): `https://webfs.oecd.org/Els-com/Affordable_Housing_Database/HM1-3-Housing-tenures.xlsx`
(OECD 2025 edition, 862,240 bytes, SHA-256 prefix `04aaa3407d55ee09`; the PDF beside it). Sheet
`HM1.3.1` "Share of households in different tenure types, in percent, 2024 or latest year available";
owner = *own outright* + *owner with mortgage*; the vintage note on the sheet: 2024 (EU-SILC 2024) except
Korea, Switzerland and the United States 2023, Canada/Chile/Mexico/UK 2022, Australia 2021, Türkiye and
Iceland 2020; Hungary withheld under revision. **The basis is confirmed to the digit against the anchors
that already stood here:** Switzerland 38.20 (2023 column), Slovak Republic 93.45, OECD 70.07, Canada 68.67,
Australia 62.66 — so the four bridge anchors' vintage, the debt this file flagged, is: this edition's
2024 values (Switzerland 2023). The by-year annex (sheet `HM1.3.A1`) carries 2010–2024 per country:
Sweden 58.27 / 58.28 / 58.19 (2022 / 2023 / 2024), Italy 73.10 / 73.95 / 75.20, Poland 84.14 / 84.72 / 84.74,
France 60.47 / 60.17 / 58.56, Germany 40.74 / 41.25 / 40.96. A seed change on four countries (three
estimates and France's rounding) — the sim-math bar ran on it (the continuation's Phase 4 record).

#### 📐 THE ESTIMATE that stood here 2026-08-02 → 2026-08-28 — a four-point regression from the Eurostat population basis (superseded; kept as the method's record)

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
the AHD vintage should record it here.** *(✅ Found 2026-08-28: the anchors are the HM1.3 workbook's 2024
column — Switzerland its 2023 — see the re-sourcing note above; the estimate's residual against the real
figures: Poland −2.1, Italy +0.8, Sweden −3.9 pp, all inside the 95% bands it quoted.)*

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

**House Price Index:** ✅ CLOSED BY CONVENTION (R4-2/R4-3, 2026-08-16/17 — `WorldFactory.cs`
`HousePriceIndex = 100f`, "the R4-2 index convention, third member"): all six seed at an index value
of 100 at game start and divergence emerges from simulation; no per-country level figures were ever
needed, so this was never a `[GAP]` to source. *(The recommendation that stood here was adopted verbatim;
the marker was corrected 2026-08-27.)*

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

*(⚠ The two paragraphs below are the 2026-08-01 pre-audit reading, kept as history; the 2026-08-02
Eurostat API audit revised the seeds — Sweden 22.5, Italy 20.0, France 19.0 — and the audit table above
is what `WorldFactory.cs` carries. Corrected 2026-08-27, not silently.)*

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
| USA | **absent BY RULING** — option 3 below taken 2026-08-17 (`WorldFactory.cs` `usa.TracksHousingOverburden = false`); homeownership 65.3 carries the USA housing slot | ruled, not a gap |

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

**USA methodology warning (this is a DECISION, not a lookup):** Eurostat measures >40% of disposable income; US sources conventionally measure >30% ("cost-burdened") or >50% ("severely cost-burdened"). No US figure is directly comparable. Three options, none free — **✅ DECIDED 2026-08-17 (R4-3, `9f12c96`): option 3; the USA does not track overburden and homeownership carries its housing slot** (`COMPLETED.md` §11 C1a/C1b; corrected here 2026-08-27):
1. Import a US figure with the bias documented (same approach the file already takes for Gini)
2. Mark USA `[GAP]` and seed only the five EU countries
3. Use homeownership rate for USA instead, where a genuinely comparable figure exists (65.3%) — **taken**

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

**✅ THE SAME-BASIS SET, RECORDED 2026-08-28 (R-C5 of the continuation kickoff) — derived from two OECD
SDMX series, nothing seeded from it (R4-2 stands: the index opens at 100).** One basis for all six: the
Taxing Wages single worker at 100% of the average wage, gross earnings before taxes (GEBT) and net income
after taxes (NIAT), national currency (XDC), from `OECD.CTP.TPS,DSD_TAX_WAGES_COMP@DF_TW_COMP,2.1`
(`…/USA+SWE+DEU+FRA+ITA+POL.GEBT+NIAT.XDC.S_C0.AW100._Z.A?startPeriod=2022`; seven key dimensions, all
stated; every observation status A; retrieved 2026-08-28), deflated by the national CPI's annual-average
growth from `OECD.SDD.TPS,DSD_PRICES@DF_PRICES_ALL,1.0` (`…/USA+SWE+DEU+FRA+ITA+POL.A.N.CPI.PA._T.N.GY`;
eight key dimensions, all stated; status A). real = (1 + nominal) / (1 + CPI) − 1.

| Country | GEBT nominal 2024 | NIAT nominal 2024 | CPI 2024 | **real gross 2024** | **real net 2024** |
|---|---|---|---|---|---|
| USA | 4.38 | 4.28 | 2.95 | 1.39 | 1.29 |
| Sweden | 4.53 | 5.70 | 2.84 | 1.65 | 2.78 |
| Germany | 5.13 | 4.81 | 2.26 | 2.81 | 2.50 |
| France | 3.93 | 3.48 | 2.00 | 1.89 | 1.45 |
| Italy | 3.12 | 0.32 | 0.98 | 2.12 | −0.66 |
| Poland | 12.16 | 11.43 | 3.65 | 8.21 | 7.51 |

**Basis (the variant-axis rule):** earnings = Taxing Wages' modelled average worker (S_C0, AW100), NOT
economy-wide earnings; net = after income tax and employee social contributions, cash transfers included;
deflator = national CPI, annual average over annual average (Sweden's KPI is interest-inclusive — the
factor-of-two note below applies); year 2024 over 2023 (the dataflow also carries 2025). **The set does NOT
reproduce the press-cited Taxing Wages figures the table above carries for Italy, Germany and France
(2.7 / 2.2 / 0.7)** — the report computes its real change with its own deflator vintage and, for Italy, the
2024 net figure moves only 0.32% nominal (the 2024 cuneo-fiscale reshaping is in the modelled net), so the
press figure's basis is not this one; the derived set is recorded as a coherent set and adopted for
nothing. Tag: components `[VERIFIED]` (status A, SDMX, queries stated), the subtraction ours — the same
class as Sweden's row below, now for all six on one basis.

### 🟡 THE THREE-BASES MIX: BLOCKER → CONVENTION, BY RULING (Elias, 2026-08-16, R4-2)

**The row below must still never be seeded as levels** — that half stands unchanged. What the ruling
resolves is that it no longer needs to be: **the real wage stat seeds as an INDEX, base 100 at epoch
per country** (the HPI convention, applied for the HPI reason). The simulation consumes **growth**
(nominal minus inflation), which the three bases agree on directionally; the level series is display
furniture, and cross-country level comparison is explicitly not claimed. The figures below therefore
serve as **directional validation anchors** for the growth model, never as seeds. If a future build
finds the model genuinely needs a level, that is a RULINGS NEEDED stop naming the specific need —
the OECD Taxing Wages re-sourcing recommendation below remains the path to a coherent level set if
one is ever actually required.

### The original finding (kept verbatim — the incoherence is real, only its consequence changed)

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

*(⚠ SUPERSEDED 2026-08-02 — the six were re-sourced from OECD SDMX on one basis, one year, one vintage
(the table at the head of §6; `WorldFactory.cs` carries them with the full basis in each comment). The
warning below described the pre-audit mixed set and is kept as history; corrected 2026-08-27.)*
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

OECD average: ~$67.5/hour (2022) — *not an anchor: the live SDMX series gives 72.59 on the current
basis (this file's own note above); the marker that stood here was wrong and is withdrawn, 2026-08-27.*
Ireland tops the ranking at ~$151 but is heavily distorted by multinational accounting — a good example of why raw cross-country comparison misleads.

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
✅ **ACTED ON — `733ac8c` 2026-08-02, "Six-anchor calibration: Poland fails by four notches, on
purpose":** the sixth anchor is in the check, Poland fails as predicted, and that expected failure is
the standing tripwire (5 of 6). *The sentence that stood here — "currently passes 5 of 5" — was false
from the day it was written; corrected 2026-08-27.* A check that passes because the hard case was never
in it is the kind of confirmation this project has learned to distrust.

*(This is a finding about the model, not about the data. Logged here because the anchor is what surfaced
it; the work belongs to Step C4's closure.)*

Also worth modeling: France carries a *negative outlook* while southern European sovereigns are stable — outlook is a real signal distinct from the rating itself, and a cheap way to telegraph a downgrade before it lands.

### 8. Sector regulation and the welfare portfolio — the §F seed spread (sourced 2026-08-28 under R-K9; mapping and caveats CONFIRMED by Elias 2026-08-28, R-C4)

**Status:** every figure `[PROVISIONAL - session-sourced 2026-08-28; mapping confirmed by Elias 2026-08-28]`.
Not `[VERIFIED]`: that upgrade is the §B database session's (a second retrieval from a session with
database access, the rule every other row of this file follows). Seeded in `WorldFactory.cs`
(`SeedSectorRegulation` / `SeedWelfarePrograms`, `915c800`) in the ANCHORED form (`6df94de`;
`CLAUDE.md` "Playtest 3, the rulings" §1; confirmed R-C3): the seeds move what a country IS — its dial
positions, the programs on its tabs, its place on the compass — never the no-policy trajectory
(byte-identical 6 of 6 through the sourcing). The session's working record: `phase4_sourcing` in the
omnibus report; the queries below are reproducible as written.

#### 8a. Regulation — OECD Product Market Regulation, 2023-24 vintage on the 2023 methodology

- **Source A, economy-wide:** the OECD's own workbook `PMR-Indicator_Econwide_2023-24-and-2018_02.02.2026.xlsx`
  (`https://www.oecd.org/content/dam/oecd/en/topics/policy-sub-issues/product-market-regulation/`…, retrieved
  2026-08-28, 116,589 bytes, SHA-256 prefix `D0EBCFC71A2103B5`); sheet `PMR_Econwide_2023-24`, column E
  "PMR (2023 methodology)", row 64 "OECD average" = 1.3464008.
- **Source B, the cross-check and the sector series:** SDMX dataflow `OECD.ECO.GCRD,DSD_PMR@DF_PMR,1.3`,
  `https://sdmx.oecd.org/public/rest/data/OECD.ECO.GCRD,DSD_PMR@DF_PMR,1.3/all?startPeriod=2018&format=csvfilewithlabels`
  (5,264 rows, years 2018 and 2023). Economy-wide identical to the workbook to seven decimals for all six
  (USA 1.5785896, SWE 0.8063377, DEU 1.2080490, FRA 1.2296512, ITA 1.2310206, POL 1.0663764). The API carries
  no OECD-aggregate row; the published average reproduces as the 38-member simple mean to 0.0004, so the
  simple mean is the sector denominator: ENERGY 1.3134 (n=38), ECOMM 1.3056 (n=38), RETAIL_TRADE 1.0409 (n=38).
- **Basis (the variant-axis rule):** indicator = the composite PMR on the **2023 methodology** — NOT
  comparable with the 2018-methodology series the OECD also publishes (the workbook's 2018 column is the
  2018 vintage recomputed on the 2023 methodology: USA 1.5851, SWE 0.8673, DEU 1.4316, FRA 1.2554, ITA 1.2859,
  POL 1.2797); scale 0–6, lower = less regulated; economy-wide = the composite; sectors: ENERGY (the OECD's
  composite of electricity and natural gas), ECOMM (fixed and mobile), RETAIL_TRADE (general retail — the
  medicines indicator is NOT folded in); reference year 2023 (in force 2023-01-01 for five, 2024-01-01 the
  USA).
- **Mapping (§F's own proposal, followed as written):** level = 50 × PMR / average, clamped 10–90, so 50
  keeps its meaning (OECD-average stringency); the five sectors without a PMR series take the country-wide
  level.

| country | PMR 2023 | level | ENERGY → Energy | ECOMM → Telecommunications | RETAIL_TRADE → Retail |
|---|---|---|---|---|---|
| USA | 1.5786 | 58.6 | 0.9855 → 37.5 | 1.4606 → 55.9 | 1.5714 → 75.5 |
| Sweden | 0.8063 | 29.9 | 1.0959 → 41.7 | 1.5459 → 59.2 | 0.5714 → 27.4 |
| Germany | 1.2080 | 44.9 | 0.4543 → 17.3 | 1.3928 → 53.3 | 0.8929 → 42.9 |
| France | 1.2297 | 45.7 | 0.8027 → 30.6 | 1.3188 → 50.5 | 3.0000 → 90 (144.1, clamped) |
| Italy | 1.2310 | 45.7 | 0.7207 → 27.4 | 0.7426 → 28.4 | 1.9286 → 90 (92.6, clamped) |
| Poland | 1.0664 | 39.6 | 1.3779 → 52.5 | 0.9784 → 37.5 | 1.0612 → 51.0 |

- **Basis note (caveat 6, confirmed):** France's RETAIL_TRADE jumped 1.99 → 3.00 between the 2018 and 2023
  vintages and clamps at 90; Italy's 1.93 clamps too. The clamp is the mapping's, not the data's: both are
  seeded at the ceiling and a future retail move is measured from 90.

#### 8b. Welfare — OECD SOCX public social expenditure by policy area, % of GDP, 2021

- **Source:** SDMX dataflow `OECD.ELS.SPD,DSD_SOCX_AGG@DF_SOCX_AGG,1.0`,
  `https://sdmx.oecd.org/public/rest/data/OECD.ELS.SPD,DSD_SOCX_AGG@DF_SOCX_AGG,1.0/USA+SWE+DEU+FRA+ITA+POL.A.SOCX.PT_B1GQ.ES10..._Z?startPeriod=2015&format=csvfilewithlabels`
  (3,120 rows; retrieved 2026-08-28; every observation status A). The DSD's arity was read from the API
  first (Part 4 §5 d-bis): eight key dimensions, the price base `_Z` included.
- **Basis (the variant-axis rule):** PUBLIC expenditure only (EXPEND_SOURCE ES10 — mandatory-private and
  voluntary-private excluded); UNIT_MEASURE PT_B1GQ = percentage of GDP; policy-area codes TP41 Health,
  TP51/K Family services and in-kind, TP82 Housing, TP91 Other social policy areas; reference year 2021 =
  the latest year all six report the programme breakdown (the USA runs to 2023, France to 2022, the other
  four to 2021).
- **The FACT half (which programs a country really runs), §F's proposal confirmed:** universal statutory
  health coverage — the five, not the USA (Medicare/Medicaid are not universal coverage; that public spending
  stays in the sourced Healthcare budget line); a national means-tested cash social-assistance scheme, a
  national housing allowance and a public childcare/ECEC entitlement — all six; UBI and a negative income
  tax — none.
- **The FIGURE half:** generosity = clamp(spend / CostShareOfGdp × 100, 0, 100) with the cost shares the
  budget already books (`WelfareProgramCostShares`: healthcare 10, means-tested 6, housing 1.5, childcare 1).

| country | TP41 Health → healthcare | TP51/K Family in-kind → childcare | TP82 Housing → housing | TP91 Other → means-tested |
|---|---|---|---|---|
| USA | 9.496 — not implemented (stays in the Healthcare budget line) | 0.568 → 56.8 | 0.236 → 15.7 | 0.900 → 15.0 |
| Sweden | 6.954 → 69.5 | 2.049 → 100 (204.9, clamped) | 0.378 → 25.2 | 0.529 → 8.8 |
| Germany | 9.994 → 99.9 | 1.436 → 100 (143.6, clamped) | 0.528 → 35.2 | 0.156 → 2.6 |
| France | 9.654 → 96.5 | 1.353 → 100 (135.3, clamped) | 0.632 → 42.1 | 1.216 → 20.3 |
| Italy | 6.880 → 68.8 | 0.588 → 58.8 | 0.041 → 2.7 | 1.559 → 26.0 |
| Poland | 4.613 → 46.1 | 0.808 → 80.8 | 0.024 → 1.6 | 0.127 → 2.1 |

**Basis notes — the six caveats of the confirmation (R-C4, 2026-08-28; each a one-literal change if it
were ever struck):**
1. **Means-tested = the TP91 total.** §F's "cash social-assistance component of income support" was read as
   TP91's cash half (TP911 income maintenance + TP912), which the TP91 total already contains — nothing is
   counted twice. (The USA's SNAP sits in TP922, in kind, 0.52 of its 0.900; TANF in TP911.)
2. **Germany's minimum income (Bürgergeld / ALG II) is booked under Unemployment (TP71) in SOCX,** not
   under TP91, so Germany's 2.6 understates the real scheme, and the aggregate dataflow cannot separate the
   social-assistance part of TP711; Poland's 2.1 is the same class, smaller.
3. **Childcare follows TP51/K (family services and in-kind),** which includes home help and other in-kind
   family services; ECEC alone (TP521) would give USA 31.5, SWE 100 (149), DEU 81.0, FRA 100 (124), ITA 49.4,
   POL 67.8 — three countries clamp at 100 under either reading (see the standing note below).
4. **2021 is a pandemic-affected year** (the USA's health line 9.496 in 2021 against 8.956 in 2023); §F's
   "latest common year" rule was followed as written (see the re-source trigger below).
5. **Poland's housing line is TP822 "other benefits in kind"** (SOCX carries no TP821 housing-assistance entry
   for Poland); the fact half's ✓ is kept at the figure.
6. The regulation clamp — 8a's basis note.

**Two standing notes (R-C4):**
- **Re-source trigger:** the day SOCX publishes a post-pandemic year common to all six (2022 needs Sweden,
  Germany, Italy and Poland to report the breakdown; 2023 needs those and France), re-pull the four lines on
  the same basis (ES10, PT_B1GQ, the same policy-area codes) and re-derive the tuples — one literal per
  slot, the no-policy trajectories unaffected by construction (the anchored form).
- **The childcare-clamp compression, known and accepted:** three countries' real family in-kind spend
  (Sweden 2.05, Germany 1.44, France 1.35 % of GDP) exceeds the booked full-generosity cost
  (`WelfareProgramCostShares` childcare = 1.0 % of GDP), so the clamp at 100 compresses the real spread
  between them and Poland (0.81) / Italy (0.59) / the USA (0.57). Recorded as known; revisited ONLY if the
  Welfare tab reads wrong or a mechanic needs childcare differentiation — because the constant is what the
  budget books, and moving it to fit a seed would be tuning the model to the data (R-K2's shape).

---

## PART 3 — Tier 0 derived stats — RETIRED 2026-08-27

A design conclusion, not a figure, so it does not belong in a seed reference: Tier 0 stats (GDP per
capita, tax/spend/deficit as % of GDP, real GDP growth, sector shares) are display-time derivations,
never state — shipped as `DerivedStats` and on screen since 2026-08-02. Record: `COMPLETED.md` §9.
The heading stays so Parts 4 and 5 keep their numbers.

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

---

## PART 5 — ELECTORAL SEED DATA: findings that constrain any future design (2026-08-11)

*(This part is task-shaped, not a figure reference: it waits on the politics/elections stream — item
10, `MISSING_PREREQUISITES.md` §D0, which points here. Kept in this file because the constraints are
properties of real electoral systems; nothing in it is independently confirmed.)*

⚠ **Findings only, extracted onto `main` deliberately.** The electoral code that produced them lives on
`stranded/politics-elections`, a branch marked *preserved, not endorsed* — uninspected, unvalidated, and
possibly never merged. **These constraints hold regardless of whether that code ever lands**, because
they are properties of the real electoral systems rather than of one implementation. Leaving them on a
branch nobody is required to read would mean rediscovering them at seed-design time.

⚠ **Re-derived 2026-08-31 (C-0.3): the branch was inspected once and DISPOSED, and this paragraph's
judgement was vindicated on both halves.** Its code never landed and never will — superseded by
`Assets/Scripts/Elections/` and W-G1 — while the constraints extracted from it here **did** hold, and
three of the four have since been re-derived independently on `main` by code that owes the branch
nothing. Row-by-row status in the provenance table below; full disposal record in `COMPLETED.md` §86.

All were produced by porting the allocator to a standalone script and running it against real published
results **before** the engine was depended on — no Unity, no compile. Two of three countries disagreed
with the plan, and both disagreements surfaced in minutes.

⚠⚠ **PROVENANCE — READ BEFORE RELYING ON ANY ROW ABOVE. The four results are NOT equally supported, and
this section originally read as though they were.** Audited 2026-08-11 by reading
`seat_allocation_check.py` (385 lines) in full:

| Claim | Backing artifact | Status |
|---|---|---|
| Sweden 2022 exact | `seat_allocation_check.py` — on `stranded/politics-elections` only (NOT on `main`; `git ls-tree` confirms, 2026-08-27), readable | ✅ **SUPERSEDED — independently re-derived on `main`, 2026-08-31 (C-0.3).** `SeatConversionHarness` reproduces Sweden 2022 **8 of 8** through the full two-tier procedure on the REAL per-constituency counts fetched at W-F1. The branch claim was right; it is no longer what the repo rests on |
| Sweden 2014, 6 seats of error | same script | ⚠ **STILL NOT re-derived on `main` (2026-08-31, C-0.3), and it is the most useful thing the branch said.** Sweden 2014 does NOT reproduce through the same allocator — 6 seats absolute error, byte-identical at divisor 1.4 and 1.2 — which narrows every "reproduces exactly" claim to *2022*. Resolving it needs all 29 constituencies 2014 data, never fetched. Register row S-6 |
| Germany 2025, off by 1 | **none — throwaway script, discarded** | ✅ **SUPERSEDED — `SeatAllocationBacktest` on `main` runs Germany 2025 at 630 seats, Sainte-Laguë/Schepers, 5 % with the SSW exemption, and reports the expected off-by-~1 at share precision.** The branch finding (seed data must carry exact vote COUNTS, never published percentages) was correct and is now the standing rule |
| Poland 2023, off by 70 | **none — throwaway script, discarded** | ✅ **SUPERSEDED — `SeatAllocationBacktest` on `main` carries the deliberate national-Poland signature at exactly 70** and also runs the real system, d Hondt in 41 districts, with the 5 %/8 % party-vs-coalition bar computed at the CALLER and MN exempt. The branch finding (national d Hondt is a different system) was correct |

**`seat_allocation_check.py` does not test Germany or Poland**, and says so in its own *"WHAT IT DOES NOT
TEST"* section. It covers Sweden plus three synthetic cases — including one built so the first divisor is
decisive, because neither real Swedish election exercises it.

⚠ **AND NONE OF IT HAS BEEN RUN HERE. Python is not installed on this machine.** So even the two Sweden
rows amount to *"a script says so"* — the script has been read and its logic is sound and unusually
honest about its own limits, but no output has been reproduced. **Nothing in this section is
independently confirmed.**

**Kept rather than retired** because the constraints are probably right, cheap to honour, and expensive
to rediscover at seed-design time. **Not treated as established** because three separate defects this
week came from a green result whose environment did not contain the claim, and *"a script reported this
once, on a machine that can no longer run it"* is that shape exactly.

**Before any of it is relied on:** port `seat_allocation_check.py` to C# and reproduce its numbers
exactly — the treatment `screenshot_edge_check.py` received — and **re-derive Germany and Poland from
scratch**, since no artifact for them exists to port.

⚠ **One code defect the script found, which is not a seed-data question and is recorded here only
because nothing on `main` records it at all:** `ThresholdRule.CoalitionShare` exists on the struct
(Poland 8%, Italy 10%) and **`SeatAllocation.ApplyThreshold` never reads it, for any country.** Coalition
thresholds are unenforced. On the branch, so it blocks nothing today.

### The four results

| Country | Method | Outcome |
|---|---|---|
| **Sweden 2022** | National modified Sainte-Laguë, first divisor 1.2, 349 seats | **EXACT** — all eight parties, 0 seats of error |
| **Sweden 2014** | Same pipeline, pre-2018 law (first divisor 1.4) | **6 seats of absolute error** (S −1, M +1, SD −2, FP +1, KD +1) |
| **Germany 2025** | National Sainte-Laguë/Schepers, 630 seats | Off by **1**: CDU 165, SPD 119 |
| **Poland 2023** | National D'Hondt, 460 seats | Off by **70**: PiS 169 (−25), Konfederacja 34 (+16) |

### What each one constrains

**⚠ SEEDS MUST CARRY VOTE COUNTS, NEVER PUBLISHED PERCENTAGES.** Germany is an input-precision problem,
not an algorithm problem — confirmed rather than assumed by re-running across the ±0.05 band that
one-decimal published shares permit, where the exact real result is reachable at CDU 22.55–22.58%. **A
rounded share is enough to move a Bundestag seat**, and it would have looked like an allocator bug
forever. This is the single most load-bearing constraint here and it applies to every country.

**⚠ POLAND AND ITALY REQUIRE CONSTITUENCY-LEVEL MODELLING.** National D'Hondt is **not** an approximation
of D'Hondt run 41 times — it is a different and far more proportional system. The real Sejm is much more
disproportionate than a national calculation suggests, because each of the 41 constituencies rounds in
the large parties' favour independently. That means per-constituency seed data (41 × ~5 parties) or an
explicitly modelled and explicitly labelled disproportionality correction. **Italy's 28 Camera
constituencies and 20 Senato regions are the same open question and must be measured the same way before
its allocator is trusted.**

**⚠ "A NATIONAL ALLOCATION REPRODUCES THE REAL CHAMBER" IS CONFIRMED FOR SWEDEN 2022 ONLY.** The other
session recorded this narrowing against its own earlier claim, which is why it is trustworthy: Sweden
2014 does not reproduce, and the error is byte-identical whether the divisor is 1.4 (historically correct
for 2014) or 1.2. Votes and real seats were each cross-checked against three independent sources before
the discrepancy was trusted over the code. The leading explanation is not an allocator bug — the same
code reproduced 2022 exactly — but the same national-vs-constituency gap Poland shows.

**Sweden 2022 is the one solid anchor**, and it is solid for a structural reason: the Riksdag's 39
levelling seats exist precisely to make the national result proportional, so a national allocation
reproduces it exactly. Sweden needs no constituency model; that property does not generalise.

### Also recorded: B3 recurring in the politics code

The branch's election display prints `46,77% of the vote`, `44,6%`, `55,4%` — **decimal commas on a
sv-SE machine**, beside money that goes through `UiFormat`'s InvariantCulture pinning. This is B3's exact
defect in new code written after `UiFormat.Number` existed. Logged here so it is known before that stream
resumes rather than found in a capture later.
