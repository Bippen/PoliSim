# pbr (K293's filing) - the pension benefit ratio in every book: the design and the pass rules, registered before any code and any measurement of pbr's tree (2026-10-30)

Drafted by the implementer of `wave/pbr` and committed ALONE on that branch (branched from main at 54c23ca9, which records K292-K298 and K310),
after the sources commit and BEFORE any code of the item exists. Nothing of pbr's tree has been built or run, in or out of the engine. The build
(phase 2) starts after fisc2 lands on main, rebased onto it; every code site named below is read on `wave/fisc2` at 9d9b16ea. The integrator commits
this text into the plan before its own Unity run.

**What K293 filed.** "The fiscal reaction stays in the player's book ... Retired there, Italy's and Poland's idle books pass 300 % of GDP from about
year 60 on both seeds, their pension lines going from 13.6 to 20.6 and from 10.4 to 21.7 % of GDP ... FILED: each country's pension benefit ratio (the
Ageing Report 2024; CBO's Social Security path); after it, retiring the reaction is measured again under the prereg's (B)." K292 DECLARED: "the
pension line has no benefit ratio" - the line is the statutory headcount at the going wage, so the average pension keeps its ratio to the average wage
for ever. Both sources project that ratio falling under the law as legislated.

**Disclosed before this rule: what was measured and computed, and on what.** (1) On fisc2's tree (9d9b16ea, extracted with `git archive`, not checked
out), out of the engine: `Tools/headless_hold/player_book.sh <tree> <play|nofrf|unchanged> Sweden,Germany,France,Italy,Poland,USA 100 777,424242`, and
`Tools/headless_hold/hold.sh <tree>`. These are the BEFORE figures below. (2) From the sources alone: the benefit-ratio and expenditure indices in Part 1.
(3) A first-order prediction, computed from (1) and (2) and not from any run of pbr: the after line is the before line times the benefit-ratio index,
and the debt shift is the summed difference with the interest rate equal to growth. It is a prediction, stated as one; it measures nothing.

## Part 1 - the sources (all byte-exact on disk, each held against its folder's SHA256SUMS.txt)

**The EU five: the 2024 Ageing Report (Institutional Paper 279, cut-off 1 December 2023).** Its statistical annex,
`ElectionsData/macro/raw/q29_longrun/ec_2024_Ageing_Report-Statistical_annex_all_horizontal_tables.xlsx` (SHA-256 3507c375...96378f6, already on disk
since q29), sheet `cross-country_tables` (the workbook's `xl/worksheets/sheet4.xml`), one figure a year 2022-2070, rows SE, DE, FR, IT, PL:
- **Table II.1.64** "Public pensions, gross expenditure as % of GDP";
- **Table II.1.79** "Benefit ratio (total public pensions, gross, %)" - the report: "The benefit ratio expresses the average pension as a share of the
  average gross wage" (IP279 Table I.1.16's note) and, in Box I.1.1, "the average pension (public pension expenditure divided by the number of
  pensioners) ... relative to the average wage";
- **Table II.1.76** "Pensioners (public, in thousands)", **Table II.1.8** "Population (million)" and **Table II.1.11** "Share of elderly population
  (+65y) as % of total population", from which the **coverage ratio** is derived: II.1.76 / (II.1.8 x II.1.11 x 10) x 100, "the total number of public
  pensioners relative to the population 65 and over" (IP279 Table I.1.12). The derived figure equals Table I.1.12 to its printed tenth at all 30 of its
  points for the five (IP279, `ElectionsData/macro/raw/k276_pension_age/ec_2024_Ageing_Report_ip279_en.pdf`, page 43);
- cross-checks: IP279 Table I.1.16 (benefit ratio 2022, 2045, 2070, "Public pensions: total") equals II.1.79 to the tenth; Table I.1.10 (the
  breakdown 2022-2070) for the attribution.

| | 2022 | 2030 | 2040 | 2050 | 2070 |
|---|---|---|---|---|---|
| **II.1.64 expenditure, % of GDP** | | | | | |
| Sweden | 7.39 | 7.55 | 7.16 | 7.02 | 7.22 |
| Germany | 10.20 | 10.80 | 11.09 | 11.00 | 11.39 |
| France | 14.45 | 14.32 | 14.10 | 13.73 | 13.57 |
| Italy | 15.57 | 16.64 | 17.06 | 15.47 | 13.68 |
| Poland | 10.24 | 11.30 | 10.59 | 10.72 | 10.06 |
| **II.1.79 benefit ratio, %** | | | | | |
| Sweden | 35.96 | 36.44 | 34.16 | 32.37 | 30.43 |
| Germany | 42.99 | 41.61 | 37.59 | 36.00 | 35.00 |
| France | 47.06 | 46.57 | 43.47 | 40.61 | 37.28 |
| Italy | 69.30 | 70.57 | 64.77 | 56.60 | 55.69 |
| Poland | 44.52 | 43.85 | 35.07 | 28.80 | 24.71 |
| **coverage ratio, % (derived = Table I.1.12)** | | | | | |
| Sweden | 126.6 | 122.7 | 117.7 | 116.9 | 110.3 |
| Germany | 124.4 | 117.5 | 120.6 | 124.0 | 121.4 |
| France | 141.2 | 127.9 | 121.6 | 121.3 | 120.6 |
| Italy | 104.5 | 98.0 | 91.2 | 90.5 | 86.1 |
| Poland | 132.7 | 126.2 | 127.9 | 121.8 | 115.9 |

**The USA: CBO's 2026 Long-Term Projections for Social Security** (17 September 2026, publication 62556; "consistent with the projections in The
Long-Term Budget Outlook Data: 2026 to 2056"), fetched for this item through the Internet Archive (cbo.gov answers a JavaScript challenge), stored in
`ElectionsData/usa/raw/pbr_social_security/` with the response headers and `fetch_log.md`:
- the projections workbook (`wayback_20260921152701_cbo_62556-2026-Long-Term-Social-Security-Projections.xlsx`): **Table 3** "Social Security's Tax
  Revenues and Outlays, With Scheduled Benefits, Measured as a Percentage of Gross Domestic Product", calendar years 1986-2100 (actual to 2025),
  column "Outlays"; **Table 2** "Projected Gross Domestic Product and Taxable Payroll" (trillions);
- the additional-information workbook (`wayback_20260919143154_cbo_62556-2026-Additional-Info.xlsx`), 2026-2100: **1. Covered Workers** (total,
  thousands), **2. Covered and Taxable Earnings** (covered earnings, trillions), **3. OASDI Beneficiaries** (total, thousands);
- already on disk, `ElectionsData/usa/raw/k274_fiscal_reaction/wayback_20260923101450_cbo_62044-2026-LTBO.xlsx` (LTBO 2026): sheet "4. Outlays, by
  Category" (Social Security, fiscal years, 2026 5.222 -> 2056 5.991 % of GDP), sheet "7. Composition of Growth in Outlays" (Social Security without
  the aging of the population 5.222 -> 4.780 % of GDP, 2026-2056; aging +1.211), sheet "11. Pop, by Age Group" (65 or older, to 2056).

CBO publishes no benefit ratio. **The USA's benefit ratio is DERIVED from CBO's five figures alone**, as the Ageing Report defines its own: the average
benefit (Table 3's outlays x Table 2's GDP / the beneficiaries) over the average covered wage (covered earnings / covered workers). DECLARED: the
outlays include administrative costs (CBO's note: "Outlays consist of scheduled benefits and administrative costs"), and CBO's beneficiaries count
DI and auxiliary beneficiaries, as the Ageing Report's pensioners count disability and survivors' pensions. The coverage-like ratio is the
beneficiaries over CBO's 65+ (LTBO sheet 11), to 2056.

| USA | 2022 | 2026 | 2030 | 2040 | 2050 | 2056 | 2070 | 2100 |
|---|---|---|---|---|---|---|---|---|
| outlays, % of GDP (Table 3) | 4.77 (actual) | 5.262 | 5.609 | 5.828 | 5.670 | 5.793 | 6.378 | 7.103 |
| benefit ratio, % (derived) | n/a | 32.28 | 32.23 | 31.61 | 29.24 | 28.62 | 27.87 | 27.00 |
| beneficiaries per 65+, % | n/a | 110.1 | 108.0 | 106.7 | 109.3 | 109.5 | n/a | n/a |

(2024 actual outlays 5.07, 2025 5.23. Beneficiaries 72.2 M in 2026, 84.5 M in 2040, 106.6 M in 2070; covered workers 186.3 M, 193.4 M, 194.8 M.)

## Part 2 - the design (what the build will be)

**The rule.** The pension line (`SpendingCategory.SocialSecurity`, the model's one pension line) is pensioners x the average pension, and the average
pension is the benefit ratio x the average wage. The pensioners are PN-1's statutory headcount (`SpendingDriver.StatutoryPensionAge`: the cohorts at
or above the age in force, `PensionPayment.PensionersMillions`); the average wage is the model's (the price level x the real wage index, K292's
caseload term). So at each boundary the line's factor becomes

    prices x driver ratio x real wage ratio x B(c, y) / B(c, y_last)

where B is the country's benefit-ratio path and y the calendar year the boundary indexes into (`Country.CalendarYear` at `IndexSpendingLines`, the key
K276's participation and PN-1's statute read). **Only the ratio is taken** (D-19 (b)'s idiom, `PopulationCohorts.RebasedTarget`): the reference is
captured at the opening (`Country.CaptureStructuralBases`), so the path is rebased on each country's own opening year (Sweden 2026, Germany 2024,
France 2024, Italy 2022, Poland 2023, the USA 2024 on today's starts) and the seed's sourced line is untouched at year 0. With B held fixed it is K292's
rule to the bit.

- **The paths.** The EU five: Table II.1.79, 2022-2070, as published. The USA: the derived ratio, 2026-2100; for years before 2026 (the USA opens in
  2024) B is 2026's [AUTHORED-DRAFT: CBO's additional tables start in 2026; the two years' index is 1].
- **Past the source's last year B holds** (2070 for the EU five, 2100 for the USA) [AUTHORED-DRAFT]. Neither source publishes beyond it. Held, the line
  follows K292's caseload rule exactly, the continuation every other caseload line has. A trend carried past 2070 would be an authored projection; the
  five ratios' declines are slowing by the 2060s and Italy's turns up (53.2 in 2060, 55.7 in 2070).
- **Every book**, the player's, the AI's and the no-policy-change book (K292's one rule). A player's change to the line sets its level and the path
  carries it from there, proportionally; a pinned line stays pinned and its reference advances (unpinning resumes from today, as the driver's does).
- **What a player's statute does to it.** (i) A pension-age bill (`BudgetBill.PensionAge`, `Country.PensionAgeOverride`) moves the headcount (the
  driver) and K276's participation, exactly as today. B does not respond to the age [DECLARED: in the notional accounts of Sweden, Italy and Poland a
  later retirement buys a larger annuity, and in Germany and France more accrual; the Ageing Report's own scenarios move spending through coverage, and
  the benefit's response to a moved age is not modelled]. (ii) Indexation: the game has no pension-indexation lever; B IS each statute's indexation and
  benefit formula as legislated at the cut-off (inkomstindex and the balancing, the Rentenwert with its sustainability factor, prices in France and
  Italy, Poland's prices plus at least a fifth of real wage growth and its NDC annuity, the US COLA on the CPI with wage-indexed initial benefits) - DECLARED; a
  pension-indexation verb is FILED. (iii) MF-14's guarantees read the per-pensioner term with B inside it: Germany's SGB VI 68a floor becomes prices x
  the real wage ratio x B's ratio >= 1 (the Rentenwert does not fall, whatever moves it); the USA's 42 U.S.C. 415(i) floor stays on the price term
  alone, B's ratio still applying (a falling ratio is CPI indexation, not a cut).
- **What is sourced and what is not.** SOURCED: B's values and definition (EU), CBO's five series (USA). DERIVED: the USA's ratio, and the coverage
  ratios. [AUTHORED-DRAFT]: B held past the last year and before the first; the year key. CONFIRMED-DIRECTION: the Ageing Report's average gross wage
  carried as the model's real wage index x prices (the report's wages follow prices and labour productivity; the model's real wage follows productivity
  per worker). DECLARED: B is the ratio for ALL public pensions (old age, disability, survivors) and the EU line is old-age cash benefits (Eurostat COFOG
  GF1002/D62 at the seed); only B's index is applied, so the perimeter's level never enters.
- **What is NOT carried: the coverage ratio** [DECLARED]. The headcount stays PN-1's statutory one, which a bill moves. The Ageing Report's coverage
  falls 1-18 % from each start to 2070 as effective exit ages rise, early routes close, and Sweden's and Italy's statutes keep indexing past the dates
  the game's statute publishes (the game carries the last published figure: board 15c-r2, K276 review 1's M1). It is the gap (A2) attributes. Carrying
  it would count the statute twice, and would override the carried law the game shows the player.
- **The FRF.** If (B) below passes, fisc2 Part 2's retirement follows in the build: `GetFiscalReactionMultiplier` returns 1 in every book, and fisc2
  Part 3's "retired everywhere" branch applies to the three fiscal-framework laws (the anchor has no consumer; the laws move the framework's reference
  the chart draws, DECLARED display only; a mechanism of their own is filed as an ask - K296). If (B) fails, the reaction stays, its doc comment updated
  with pbr's measurement.
- **Save format.** One new stored field (`Country.PensionBenefitRatioAtLastIndex`, B at the last index; 0 on an older save) changes what the pension
  line's next index MEANS: the format steps from fisc2's 70 to 71 (or the next free number at the rebase) and refuses the older save (ruling A).

**The code sites the build touches** (as read on `wave/fisc2` 9d9b16ea):
- `Assets/Scripts/Simulation/SimulationManager.cs` - `IndexSpendingLines` (8483-8529: B's ratio into the pension line's factor at 8520, read before
  the guarantee at 8521, the reference advanced as `PriceLevelAtLastIndex` is at 8491-8492); `PensionNominalGuarantee` (8543-8549: the per-pensioner
  term with B's ratio); `ProjectLineNextYear` / `ProjectedRealTerm` (8556-8573: the NEXT YEAR projection carries B(y+1)/B(y)); the preview clone's
  field copy (7684 / 7885: the new field copied with them); the reaction's doc and `GetFiscalReactionMultiplier` (698-714) per (B).
- `Assets/Scripts/Data/Country.cs` - the new field beside `PriceLevelAtLastIndex` / `RealWageIndexAtLastIndex` (177-181), captured in
  `CaptureStructuralBases` (291, beside 316).
- New `Assets/Scripts/Data/PensionBenefitRatio.cs` (B(c, y): the path, held before and after, the probe switch) and
  `Assets/Scripts/Data/Generated/PensionBenefitRatioPath.cs`, written by a new `Tools/pbr_prep.pl [--check]` from the byte-exact files only (q29's
  and K276's discipline: a table title matched exactly once, a country carried once, a year missing or a value outside 5-100 stops it; `--check`
  regenerates and compares byte for byte), with its line in `.gitattributes`.
- `Assets/Scripts/Data/SpendingLine.cs` (`ProjectNextYear`'s doc), `Assets/Scripts/Data/SpendingDrivers.cs` (the class doc), `Assets/Scripts/Data/
  PensionPayment.cs` (the readout's doc: the average benefit and the replacement rate now fall against the wage).
- `Assets/Scripts/Persistence/SaveGameService.cs` - `CurrentSaveVersion` 70 -> 71 with its reason.
- `Assets/Editor/SpendingIndexationDiagnostic.cs`, `Assets/Editor/LineIndexationDiagnostic.cs` - the pension line's expected figure gains B's ratio,
  computed in the check from the generated table (not from the recompute); every other line's rule and tolerance unchanged.
- New `Assets/Editor/PensionBenefitRatioDiagnostic.cs` (the simulation bar): (A1)/(A2) on `NoPolicyCentury.Unchanged`, seed 777; and
  `Tools/headless_hold/PlayerBook.cs` gains the B index and the statutory headcount per 65+ in its rows (both seeds out of the engine).
- `Assets/Editor/DebtPathHoldDiagnostic.cs` - its `Registered` table only if a registration lifts; `docs/reference/MODEL_REFERENCE.md` - the pension
  line's rule.

## Part 3 - acceptance, DECLARED before any measurement of pbr's tree

**The instruments.** Out of the engine: `player_book.sh` (`unchanged`, `play`, `nofrf`; six players; seeds 777 and 424242; each on its own start) and
`frf_play.sh <root> on none 100 <seed>` (the dump's all-AI world). In the engine: `PensionBenefitRatioDiagnostic` (seed 777), the named checks, the
integrator's dump. A figure "at year y" is the record whose calendar year is y.

**(A0) The mechanism - asserted, a miss FAILS.**
1. `Tools/pbr_prep.pl --check` reproduces the generated table byte for byte from the stored files.
2. `SpendingIndexationDiagnostic`: each player's pension line at year five equals seed x driver ratio x prices x real wage ratio x B(y5)/B(y0) within
   its 1e-4; every other line as before, its tolerance unchanged.
3. `LineIndexationDiagnostic`: an AI country's pension line reads the compounded real wage x B's index within its 2 %; every other line as before.
4. **Neutrality**: with B held at 1 (the probe switch), `frf_play.sh on none` on both seeds and `player_book.sh` in all three modes print
   byte-identically to the tree before pbr (main after fisc2), on every column that tree prints. B is then the only expression pbr changes.

**(A1) The no-policy pension line against its source** - `NoPolicyCentury.Unchanged` / `player_book.sh unchanged`, both seeds, each country on its own
start. M(y) = the pension line's share of GDP at year y over its share at the opening; S(y) = the source's expenditure at y over its value at the
opening's year: Table II.1.64 for the EU five, at 2030, 2040, 2050 and 2070; CBO's Table 3 for the USA (actual for 2024), at 2030, 2040, 2050, 2070
and 2100. **Band: |M(y) / S(y) - 1| <= 0.10** [AUTHORED]. The reason: the line carries the source's benefit ratio exactly and shares its demography
(the EU pyramid is EUROPOP2023's path rebased, D-19; the US pyramid CBO's, K299) and K276's participation (the labour force within +/-3 %), but not
its coverage ratio, which moves the Ageing Report's line by -0.2 (Germany) to -3.0 (Italy) points of GDP over 2022-2070 (Table I.1.10). A tenth holds
the line where the coverage term is small - Germany throughout, every country to 2030, the USA throughout (CBO's beneficiaries per 65+ move about 3 %
to 2056) - and may miss where the Ageing Report's coverage falls by more than a tenth from the start (Italy from 2040; France from 2040, in part
offset by its statute's own steps to 64; Sweden and Poland by 2070).

**(A2) The attribution - printed beside every (A1) line.** The same comparison with the coverage term moved to the source's: M(y) x [C_src(y) /
C_src(y0)] / [C_m(y) / C_m(y0)], where C_m is the model's statutory headcount over its own 65+ and C_src the source's coverage ratio (the EU five, Part
1; the USA, beneficiaries per 65+, to 2056 only - (A2) is not computed for the USA at 2070 and 2100). **Band: +/-0.05** [AUTHORED]: what is left is
the dependency and labour-market terms the model shares with the source (the labour force's +/-3 % and two points for the model's wage against
the source's productivity per hour). Printed beside it: the three-way split of the (A1) gap - coverage, the benefit ratio (zero by construction),
and the rest.

**Disposition of (A), registered now.** An (A1) line inside its band is ok. **An (A1) miss with (A2) inside is a REGISTERED miss** - the declared
coverage term, printed with its figures, not a failure. **An (A1) miss with (A2) also outside is a stopping result**: it is reported with the
three-way split and the build is held for a ruling. No band is widened and no year is dropped.

**(B) fisc2's (B), re-measured.** The rule is fisc2's prereg Part 2, verbatim: the reaction retires from the player's book (`GetFiscalReactionMultiplier`
returning 1 in every book) **if and only if**, with pbr in and the reaction retired (`player_book.sh nofrf`), every one of the twelve idle player
books (six players, seeds 777 and 424242) over years 1-100:
- **(B1)** has its runaway guards absorb nothing (`SovereignCrisisState.GuardAbsorbedBillions` 0 at year 100);
- **(B2)** keeps its debt ratio within [-100 %, +300 %] at every year [fisc2's AUTHORED-DRAFT band, unchanged];
- **(B3)** keeps (A1)'s band - fisc2's (A1): total spending lines at years 10, 30, 50 and 100 within +/-3.0 points of GDP of the no-policy-change book's
  (`unchanged`) at the same year.

Otherwise the reaction STAYS in the player's book, and every failing book is named with its cause (its revenue at unit multiplier against its lines,
year 1 to year 100). **It differs from fisc2's in nothing.** Stated so it cannot be misread: (B3) is fisc2's band as worded, so K310's registered (A1)
misses (Sweden at year 100, Poland at years 50 and 100) fail (B3) unless pbr closes them; K310 registered them for R1's landing, not for (B). Printed
beside it, unjudged: each book's (A1) gap with the reaction in (`play`) and out (`nofrf`), so the part retiring the reaction makes is visible.

**What else must not move** (each a stopping result on a miss; nothing re-banded):
- `DebtPathHoldDiagnostic`: every asserted band holds - (h1) the five; (h2) Sweden, France, Italy and Poland (lifted by fisc2); (h3) the USA's ten and
  thirty years. Germany (h2) stays registered unless it holds (then it lifts). The hold reads the no-policy-change book, which pbr moves: every
  pension line slows, and the DSM 2024's cost of ageing IS the Ageing Report 2024's, so the hold's premise comes closer to its source. A newly missing
  asserted band stops the item.
- `LongRunAnchorsDiagnostic`: (a)-(e) and K298's registration conditions unchanged; its acceptance stays 0 FAIL. The no-policy marks read the shared
  idle century, whose fiscal path pbr moves.
- `ParticipationCohortDiagnostic`: (c) and (d) inside their bands, unchanged rules (participation does not read the pension line; it moves only
  through the economy).
- `SpendingIndexationDiagnostic`, `LineIndexationDiagnostic`: (A0) 2-3 - the pension line's term added, nothing else changed.
- `PensionPaymentDiagnostic`: its seed gate byte-identical (B's index is 1 at the opening).
- The all-AI world's other lines: their indexation expression unchanged ((A0) 2-4); their levels move only through the economy the pension line
  moves (prices, the real wage, potential growth, their drivers). Reported, not banded: the largest move of any other line's share of GDP at years 10,
  30, 50 and 100 in `frf_play on none`.

**Expected movement.** The dump moves from the first boundary in every country: the pension line is B's index below today's - at year 10 about -7 %
in Sweden and Germany, -8 % in Poland, -4 % in France, +1 % in Italy, 0 in the USA - in every book, so the AI books' deficits fall and the treaty rule (rates
before lines, K251) and the US debt-limit rule answer. A new family `pbr`, its dump diff, the review ledger's rows for both moved digests, save format
71. FT-10 slack ceilings may need lowering by the check's own rule.

**Stopping rules.** A band missed is reported, never re-banded; no coefficient, year or source is re-chosen to pass. B's values come from the
generator alone. An (A0) miss is a defect: the mechanism is fixed, the rule is not. A guarded check newly failing, or an (A1) miss with (A2) outside,
holds the item for a ruling with its measurement. (B) decides the reaction mechanically. Anything written after a measurement is an amendment,
dated, marked "not preregistered", and committed alone before the code it governs.

## Part 4 - before figures and predictions (fisc2's tree, out of the engine), stated before any measurement of pbr

**Before** (seed 777; 424242 within 0.4 points of GDP on every line below). The no-policy-change book's pension line, % of GDP, at its opening and at
2030 / 2040 / 2050 / 2070: Sweden 7.00 -> 7.05 / 7.38 / 7.76 / 8.84; Germany 9.00 -> 9.60 / 10.80 / 11.11 / 11.86; France 12.40 -> 12.93 / 13.70 / 14.46 /
14.83; Italy 13.60 -> 14.63 / 17.52 / 19.32 / 19.36; Poland 10.40 -> 11.09 / 12.28 / 15.22 / 18.97; the USA 5.28 -> 5.81 / 6.47 / 6.66 / 7.71 (2100: 8.95).
As M / S - 1 before: Italy +18 / +43 / +62 % at 2040 / 2050 / 2070, Poland +16 / +42 / +89 %, Sweden +13 / +22 / +35 %, Germany +13 / +17 / +20 %, France
+12 / +21 / +26 %, the USA +7 / +13 / +16 % (+21 % at 2100). The idle player book with the reaction retired (`nofrf`), debt at year 100 (777 / 424242):
Sweden -84 / -50, Germany 209 / 218, France 216 / 216, Italy 399 / 397 (past 300 % at year 59 on both), Poland 460 / 449 (year 61), the USA 148 / 148.
`hold.sh` (seed 777): Sweden (h2) -11.2 points 2026-2035 against the DSM's -5.8 +/-5.6 (inside by 0.2); Germany (h2) +20.8 against +3.0 +/-7.6
(REGISTERED); France (h2) +36.2 against +29.8 +/-10.7; Italy (h2) +25.1 against +18.5 +/-15.8; Poland (h2) +45.3 against +44.9 +/-11.2; every (h1)
inside; the USA (h3) +13.3 against +19.6 +/-10 (ten years) and +64.7 against +74.5 +/-25 (thirty).

**Predicted (A1)** (the before line x B's index, against S): inside the band everywhere except **Italy at 2050 (about +17 %) and 2070 (about +30 %)**,
with Italy 2040 (+10 %) and Sweden 2070 (+9 %) at its edge; the USA inside at every year (-1 to +4 %). **Predicted (A2)**: Italy 2050 inside (about
+1 %), **Italy 2070 near or past its band (about +7 %)** - which would make Italy 2070 a stopping result - and Poland's 2040-2050 lines about -8 to -9 %
on (A1)'s side of the tenth (inside it, so (A2) does not decide them).

**Predicted hold** (the summed pension difference over each window, r = g): Sweden (h2) about -13.6 - **past its band's floor (-11.4), a
predicted stopping result**: the model's Swedish no-policy book already ran 5.4 points below the DSM before pbr, and pbr adds the DSM's own pension
decline on top; Germany (h2) about +17 (still registered); France (h2) about +33, Poland (h2) about +42.5 (both inside, nearer the DSM); Italy (h2)
about +29.6 (inside; Italy's ratio rises to 2024 before it falls); the USA (h3) thirty years about +57 (inside), ten years unchanged.

**Predicted (B): FAILS.** The summed savings over the century (r = g, crude) take the nofrf books' year-100 debt to about Sweden -210 to -245, Poland
-265 to -275, France -55, Germany +6 to +16, Italy about +80, the USA about +53. So Italy and Poland no longer pass 300 %, but **Sweden and Poland cross
(B2)'s floor** (-100 %): once the pension line stops outgrowing revenue, the income tax's creep the player's statute keeps (K271, K275; fisc2 Part 2:
Sweden 10.4 -> 18.5 % of GDP, Poland 4.5 -> 13.5) runs the idle book to a net creditor; and Sweden still misses (B3) (its K310 gap shrinks by only
the pension line's share, about 3.86 -> 3.6 points). If so, the reaction stays, and its documented job becomes absorbing the creep, not the pension
line's drift.

## Amendment 1 (2026-10-30, written after the rebase onto main 030d8d17 and BEFORE any code of pbr; nothing of pbr's tree has been measured)

The text above is the prereg as committed alone (2dc17b69, rebased as 8024a783). fisc2 landed on main as 030d8d17. Its final tree differs from
9d9b16ea, the tree the text above was read on, in `Assets/Editor` only (`ParticipationCohortDiagnostic.cs`, `TrajectorySentinelCheck.cs`,
`ReviewLedgerCheck.cs`) and in records; `Assets/Scripts` and `Tools/headless_hold` are identical, so every code site and line number named above
holds, and so do Part 4's before figures (the runtime they were measured on is the same). What changes in the rule's reading, and three rulings:

1. **ParticipationCohortDiagnostic carries K313.** "What else must not move" reads: (c) and (d) inside their bands with the rules unchanged; Poland
   2050's (c) line is REGISTERED under K313 only while K313's two bounds hold - (i) the line with the health and education terms removed inside +/-3 %,
   (ii) their part at most one point - and fails once either breaks; any other (c) miss fails.
2. **TrajectorySentinelCheck.** The baseline family is `fisc2` (seed 777 `4e5611c4...`, 424242 `d3aaee0a...`); pbr moves it (Part 3's "Expected
   movement") and the integrator re-pins it to `pbr` after its own dump. FT-10's ceilings now stand at France household 5.5, Poland household 2.5 and
   Poland industry 2.5 (fisc2's integrator); any change is the check's own rule, never a re-band. `ReviewLedgerCheck.GrandfatheredCeiling` is 7.
3. **The stored field is the YEAR of the last index**, `Country.PensionBenefitRatioYear` (0 on an older save), not B's value: B at the last index is
   read from the generated table at that year. The NEXT YEAR projection needs B one year past the last index, which a stored value cannot give. The
   factor is the same: B(c, y) / B(c, the stored year). The reference is the epoch's year at `CaptureStructuralBases` (`SimulationManager.EpochDate.Year`,
   K276's idiom there), i.e. each country's own opening year.
4. **Two more files than Part 2 named**, both of the build's own instruments, neither a rule: the generator's second output
   `Assets/Editor/Generated/PensionBenefitRatioAnchors.cs` (the (A1)/(A2) source indices - II.1.64 and the coverage ratio for the EU five, CBO's Table 3
   and beneficiaries per 65+ for the USA), and `GeneratedCatalogCheck`'s drift rule for the generator's source files (UsDemographicOutlook's idiom).
   `NoPolicyCentury.Year` gains the pension line's share, the statutory headcount, the 65+ population and the calendar year, read only.
5. **Ruled now** (Code's recommendation, Elias's standing instruction of 2026-10-27; the coordinator, 2026-10-30), recorded in the prereg's terms:
   - B holds flat past the source's last year (2070 for the EU five, 2100 for the USA) - DECLARED (was [AUTHORED-DRAFT] above);
   - a pension-age bill does not move B - DECLARED;
   - there is no pension-indexation lever - DECLARED, and the lever is FILED.
   The USA's B before 2026 (2026's value) stays [AUTHORED-DRAFT]. No band, year or source above changes.

## Amendment 2 (2026-10-30, written while building and BEFORE any code is committed or any of pbr's tree is measured)

Three readings the build needed and the text above leaves open; none changes a band, a year or a source.

1. **The USA's (A2) at its opening years.** CBO's beneficiaries (and so C_src) start in 2026 and the USA opens in 2024: C_src(2024) is read as 2026's,
   as B is (Part 2). (A2) is not computed past 2056 (CBO's 65+ ends there), as the text says.
2. **An (A1) miss with no (A2) to read** (the USA at 2070 and 2100) is a stopping result, as an (A1) miss with (A2) outside is; and a stated year with no
   record of that calendar year in the run FAILS.
3. **The neutrality instrument (A0) 4.** The probe is `PensionBenefitRatio.ProbeSuspended`, set out of the engine by `PBR_PROBE=1` in `PlayerBook.cs` and
   `FrfPlay.cs`; their outputs are compared with main's (030d8d17) on every column main prints. Beside them, as a stronger reading and not a
   replacement, a full-precision state hash (every country, every year: GDP, nominal GDP, the debt ratio, the price level, the real wage, unemployment,
   approval and every spending line's amount, round-trip formatted) from the same worlds on both trees.

## Amendment 3 (2026-10-30, written AFTER the measurement): the two stopping results, registered and bounded as ruled (K318, K319)

**Not preregistered.** Both rules below were written after pbr's tree was measured (out of the engine, at 065ede95), and they are dated so. The
measurement stopped on two lines, as Part 3 says it must; the coordinator ruled both under Elias's standing instruction of 2026-10-27, as K298, K313
and K295 were ruled. No band is widened, no year dropped, no source re-chosen.

**K318 - Italy 2070's (A1).** Measured: (A1) +30.2 % (seed 777) and +30.8 % (424242) against +/-10 %; (A2) +6.0 % and +6.5 % against +/-5 %. The split
of the (A1) gap: coverage x1.2288 (the model's statutory headcount per 65+ against the Ageing Report's coverage ratio, 104.5 -> 86.1 % 2022-2070 - the
game carries Italy's statute at 67 y 3 m where the Ageing Report indexes the age), the benefit ratio x1 (by construction), the rest x1.0597 / x1.0647
(the labour-market term: Table I.1.10's employment-rate and career-shift effects, -2.8 points of GDP for Italy, the five's largest). Italy 2050 is
inside (A2) and REGISTERED by Part 3's own disposition ((A1) +16.8 / +17.1 %, (A2) -1.0 / -0.8 %). The rule: Italy 2070's (A1) line is REGISTERED
while both hold - (i) its (A2) stays at or below +7.5 % [AUTHORED-DRAFT] (read with Part 3's lower edge unchanged: (A2) inside [-5 %, +7.5 %]); (ii)
coverage remains the largest of the three factors in the printed split (the largest |ln factor|; the benefit ratio's is 0). It fails once either
breaks. Any other (A1) miss whose (A2) misses still stops. The Ageing Report's indexed pension age is NOT fed into Italy's headcount: the carried law
the game shows stays.

**K319 - Sweden (h2), DebtPathHoldDiagnostic.** Measured (seed 777): the change 2026-2035 -13.0 points against the DSM baseline's -5.8, band +/-5.6
(its edge -11.4: 1.6 points beyond it). The decomposition 2026-2035: the model -pb -8.5 (before pbr -6.7), interest +5.5, real growth -4.8, inflation
-5.1, the rest +0.0; the DSM -pb -1.1 (of which the cost of ageing -0.7), interest +4.6, real growth -4.4, inflation -5.0, stock-flow +0.0. The rule:
Sweden (h2) is REGISTERED in the hold's table on K218's mechanism, its cause re-attributed to Sweden's primary balance before ageing (as K295 did for
Germany), while both hold - (i) the decomposition's largest gap term is the primary balance outside the pension line; (ii) the miss beyond the band's
edge is at most 2.5 points [AUTHORED-DRAFT]. It fails once either breaks. The terms (i) reads, each the model's against the DSM's over the window:
the primary balance outside the pension line - the model's -pb less the pension line's own part (the sum over the window of its share of GDP less its
share at the opening) against the DSM's -pb less its cost of ageing; the pension line's part against the DSM's cost of ageing; interest; real growth;
inflation; the rest against the stock-flow. FILED: Sweden's primary balance before ageing, a calibration item of its own.
