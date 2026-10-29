# fisc2 - K269 (a) the player's lines, K269 (d) the framework's line, K218's five registered bands: the pass rules, registered before the code they govern (2026-10-29)

Drafted by the implementer of `wave/fisc2` and committed on that branch ALONE, BEFORE any code it governs. Every "before" figure below was measured on the tree at
cf6bd007 (main, the family `k276`), out of the engine, with the two loop instruments named in each part (.NET's runtime, not Mono's: a prediction to the printed
decimal at best - `Tools/headless_hold/hold.sh`'s caveat). Nothing after the change has been measured when this is written. The integrator commits this text into
the plan before its own Unity run.

**The instruments.** `Tools/headless_hold/player_book.sh <root> <play|nofrf|unchanged> Sweden,Germany,France,Italy,Poland,USA 100 777,424242` - the player's
country with no party seated (the shared century's idle book), `PolicyDecision.None` every year: `play` the book as the tree carries it, `nofrf` the fiscal
reaction retired in every book (the sweep hook's pair at (0, 1, 1)), `unchanged` K249's no-policy-change book. `Tools/headless_hold/hold.sh <root>` -
DebtPathHoldDiagnostic out of the engine. `Tools/headless_hold/frf_play.sh <root> on none 100 <seed>` - the dump's all-AI world.

## Part 1 - the rule an unchanged-policy line follows (K269 (a))

**What was measured before** (player_book, cf6bd007). The player's book indexes its lines by prices x driver alone (`SimulationManager.IndexSpendingLines`: the
real factors are the AI's only), so every idle player book's lines fall as a share of GDP: total lines from 41.5 / 41.8 / 46.5 / 40.3 / 41.8 / 19.9 % of GDP
(Sweden, Germany, France, Italy, Poland, the USA) to 12.2 / 15.1 / 16.8 / 16.4 / 12.7 / 6.6 % at year 100 (seed 777; 424242 within 1 point). With the fiscal
reaction the multiplier pins at its 0.5 floor in all twelve books by year 100 and debt reaches -255 / -112 / -79 / +96 / -209 / +5 % (777); without it
-924 / -787 / -855 / -381 / -863 / -350 %.

**The rule (R1), decided from the sources.** The sources' no-policy-change conventions hold programmes at their rules and the rest at its share of output:
- the Ageing Report 2024 (Institutional Paper 279, `ElectionsData/macro/raw/k276_pension_age/`): education "expenditure per capita growing in line with labour
  productivity" at a constant students-to-staff ratio; health care unit costs with real GDP per capita (the income elasticity converging to 1), the labour-intensity
  scenario with GDP per worker; "wages are assumed to evolve in line with prices and labour productivity";
- the Debt Sustainability Monitor 2024 (Institutional Paper 306, `ElectionsData/macro/raw/q32_debt/`): "the structural primary balance (SPB) before costs of ageing
  is assumed to remain constant at its value forecast for T+1", ageing costs added from the Ageing Report - every non-ageing line at a constant share of GDP;
- CBO, The Long-Term Budget Outlook 2026 to 2056 (publication 62044, `ElectionsData/usa/raw/k274_fiscal_reaction/`): Table 4's discretionary outlays stand at
  4.641 % of GDP every year from 2040 to 2056 (held at the pace of GDP beyond the window); Table 7's health care grows with potential GDP per person plus
  "additional cost growth".

So R1: **in every book, the player's included, a line with a driver (a caseload) carries prices x its driver x the real wage's growth; a line with no driver
carries prices x the economy's real potential growth** - exactly the AI's rule (RF-2 as ruled, §384), which K249 already reads as unchanged law. A player's change
sets a line's level and R1 carries it from there; a pinned line stays pinned. The player's tax statute is untouched (K271's creep, K275 / K287's cap).
DECLARED, not modelled: CBO's additional cost growth on health; the pension line's benefit ratio (the line is the statutory headcount at the going wage, the
income index's rule RF-2 rests on, in all six - France's and Italy's pensions in payment follow prices, the USA's COLA the CPI, Poland's NDC benefit falls with
life expectancy; CBO's Social Security without ageing falls 5.222 -> 4.780 % of GDP 2026-2056). Measured before in the no-policy-change book, which already runs
R1: Poland's pension line 10.4 -> 20.1 % of GDP by year 100. Filed as its own item if Part 2 names it.

**Acceptance (A), DECLARED before measurement** - after the change, player_book `play` against `unchanged`, six players, both seeds:
- **(A1) the rule holds in the player's book**: total spending lines (% of GDP) at years 10, 30, 50 and 100 within +/-3.0 points of the no-policy-change book's at
  the same year. [AUTHORED-DRAFT] band: the two books share R1 and differ in revenue (the statute's creep and the reaction against re-indexed thresholds and none),
  which moves the economy the drivers read.
- **(A2) the reaction no longer pinned**: the fiscal reaction's multiplier at neither bound (0.5, 1.5) in any year 1-100, and the debt ratio at year 100 within
  +/-50 points of the opening ratio. [AUTHORED-DRAFT] band.
- **(A3) the AI's books untouched by R1**: the dump's all-AI world (frf_play `on none`, both seeds) byte-identical before and after R1 alone (R1 changes no
  expression an AI book stores).

## Part 2 - the fiscal reaction in the player's book (K269 (a), K251)

**The decision rule.** The reaction retires from the player's book (`GetFiscalReactionMultiplier` returning 1 in every book) **if and only if**, with R1 and Part 4's
fix in and the reaction retired (player_book `nofrf`), every one of the twelve idle player books (six players, seeds 777 and 424242) over years 1-100:
- **(B1)** has its runaway guards absorb nothing (`SovereignCrisisState.GuardAbsorbedBillions` 0 at year 100);
- **(B2)** keeps its debt ratio within [-100 %, +300 %] at every year. [AUTHORED-DRAFT] band: a net creditor of a year's output, and a stock past which the debt
  crisis, not the budget, would be what stops it;
- **(B3)** keeps (A1)'s band.

Otherwise the reaction STAYS in the player's book, the code unchanged there, and every failing book is named with its cause: its revenue at unit multiplier
against its lines, year 1 to year 100.

**Prediction, stated before measurement:** (B) FAILS. Before, without the reaction, the idle player book's revenue rises from 40-45 % of GDP to 48-57 % by year
100 in Sweden, Germany, France and Poland (the income tax's creep the player's statute keeps, K271 and K275: Sweden 10.4 -> 18.5 % of GDP, France 9.7 -> 21.3,
Poland 4.5 -> 13.5), Italy's flat (42.0 -> 42.1) and the USA's 17.7 -> 22.9 (CPI-indexed thresholds, K288). R1 holds lines near the no-policy-change book's, which
reach 41.7 / 46.9 / 48.2 / 49.3 / 60.0 / 24.4 % at year 100 (777), so Sweden and France should run surpluses past (B2)'s floor and Italy and Poland deficits past its
ceiling. The reaction then stays, and its documented job becomes absorbing the creep and the pension line's drift rather than the driver-only lines' runaway.

## Part 3 - the debt chart's line and the three fiscal-framework laws (K269 (d))

**The rule.** The debt chart's threshold line reads the country's own fiscal framework, [SOURCED] in `ElectionsData/rules/fiscal_rules.md` (every page byte-exact
with its digest): Sweden its debt anchor, 35 % of GDP (skr. 2025/26:76, 3.2: "Skuldankaret är satt till 35 procent av BNP och gäller tills vidare"); Germany,
France, Italy and Poland the Treaty's reference value, 60 % (Protocol No 12, Art. 1 - Germany's brake and Italy's balance are structural-balance rules and France
has no national limit; Poland's constitutional 3/5, art. 216 ust. 5, is the same figure on the national definition); the USA none (no ratio rule: its debt
limit is a dollar ceiling, 31 U.S.C. 3101(b)), so no line. The line is labelled by its rule and its slip names the instrument. `FiscalRules` (the desk's rule
table) holds the figures; the anchor the reaction reads no longer draws.

**The laws.** The Constitutional Debt Brake Act, the Debt Anchor Act and the Debt Brake Suspension Act move `Country.ComfortableDebtToGdpPercent`, which is the
reaction's neutral point (seeded at the opening debt ratio, [AUTHORED-DRAFT] with the slope) and which nothing but the reaction reads.
- If Part 2 keeps the reaction in the player's book: the laws keep their mechanism - the reaction's neutral point, so the government the player is tightens earlier
  or later - acting in the player's book alone (an AI book's rule is the treaty's), DECLARED on each law with its provenance (the citation CONFIRMED-DIRECTION,
  the size [AUTHORED-DRAFT]); the anchor's own doc says what it is. Re-pointing them at the framework is filed for the day the reaction retires.
- If Part 2 retires the reaction everywhere: the anchor has no consumer; the laws then move the framework's reference the chart draws (DECLARED, display only)
  and a mechanism of their own is filed as an ask.

## Part 4 - K218's five registered bands (DebtPathHoldDiagnostic)

**Measured before** (hold.sh, cf6bd007, seed 777). Three registered bands already hold: France (h2) +39.7 points against the DSM's +29.8, band +/-10.7; Italy (h2)
+28.6 against +18.5, +/-15.8; Poland (h2) +48.9 against +44.9, +/-11.2. Two miss, both on the primary balance: Sweden (h2) -23.2 against -5.8, +/-5.6 (the
decomposition's -pb -19.3 against the DSM's -1.1); Germany (h2) +33.7 against +3.0, +/-7.6 (-pb +34.9 against +9.8). Parts 1-3 do not reach the hold: it reads
the no-policy-change book, which already runs R1 and no reaction.

**Attribution, read line by line in the no-policy-change book** (player_book `unchanged`, 777): the `IncomeSecurity` line - in the EU five the RESIDUAL of
general-government cash transfers `WorldFactory` solves to land the year-1 primary balance (Sweden 9.58, Germany 11.80, France 10.12, Italy 7.70, Poland 13.35 %
of GDP) - carries the unemployment RATE as its driver (`SpendingDrivers.Of`), so the whole residual moves one for one with the rate's ratio. Sweden's rate falls
8.0 -> 5.7 % by year 5 and the line 9.58 -> 6.47 % of GDP; Germany's oscillates (3.50, 3.95, 3.53, 4.01, 3.59, 4.18, 4.78 in years 0-6) and the line with it
(13.83 % at year 5), the primary balance swinging between -0.75 and -6.95 %; Poland's, a rate on a shrinking population carried at the going wage, rises
13.35 -> 19.84 % by year 100. The cycle is already the unemployment benefit term's (`GetUnemploymentBenefitCost`: the benefit rate x the unemployment rate x
nominal GDP) and the LaborMarket lines'; a residual has no caseload of its own. A model defect, in every book.

**The fix (F4):** `IncomeSecurity`'s driver becomes the population - a caseload of residents at the going wage, which holds the line's share of GDP where the real
wage follows output per head (the DSM's non-ageing convention). The USA's IncomeSecurity line (budget function 600) moves with it; its cycle is in the benefit
term as well. F4 is in every book, the AI's included (a measured defect demands it): **the dump moves, and its family is `fisc2`.**

**The decision rule, DECLARED before measurement** (hold.sh after the change, seed 777 - the hold's own seed):
- a registration is lifted where its band holds; a registered band that misses stays registered, its reason re-attributed by its own decomposition; no band is
  widened and no band is newly registered;
- F4 lands only if no band asserted today misses after it (Sweden, Germany, France, Italy and Poland (h1); the USA (h3) over ten and thirty years). If one does,
  F4 is withdrawn and reported with the measurement, and the registrations are read on the tree without it.

**Prediction:** Sweden (h2) narrows by about the residual's fall (3 points of GDP for most of the window) and may close; Germany (h2) narrows (the oscillation's
deficits gone; the pension line's ageing stays); France (h2), Italy (h2) and Poland (h2) stay inside or near their bands, Poland's least sure (its residual fell with
its unemployment rate in the window, so its primary balance worsens without it).

**What follows a miss.** The item reports the criterion missed, with the measurement. No band, coefficient or rule above is re-chosen to pass.

## Note 1 (2026-10-29, written AFTER the measurement, on the review's H1): (A1) MISSED

Measured out of the engine on the branch with R1 and F4 in (`player_book.sh` `play` against `unchanged`, six players, seeds 777 and 424242), the band
unchanged at +/-3.0 points of GDP:
- **Sweden** misses at year 100: +3.86 (777) and +3.88 (424242); years 10, 30 and 50 inside (+0.88 at year 50).
- **Poland** misses at year 50, +3.42 (777) and +3.41 (424242), and at year 100, +3.79 and +3.71.
- The other ten books are inside: France 2.2-2.4, Germany 2.2, the USA 0.3, Italy 0.00.

**Attribution** (the LINE and MACRO rows, seed 777, year 100): the played economy is smaller and its real wage higher than the no-policy-change book's.
Sweden's real GDP is 2474.6 against 2583.5 (-4.2 %) and its real wage 371.6 against 349.2. Poland's real GDP is 2902 against 3001 (-3.3 %) and its real
wage 566 against 542. This is the income tax's creep in the played book (K271, K275), whose participation term lowers labour supply. The caseload lines at the
going wage therefore take a larger share of a smaller output. The movers are the pension line (Sweden +1.21, Poland +1.63 points of GDP) and F4's income
security on the population (+1.10 and +1.28), with municipal grants (+0.36, +0.39) and Sweden's health (+0.30) after them. The same gap fails (B3) in
those books, and Sweden's (B) fails on (B3) alone.

**Disposition:** ruled by K310 under Elias's standing instruction. R1 lands with (A1) missed, the band unchanged, the miss recorded. Nothing above was
re-chosen.

## Amendment 1 (2026-10-29, written AFTER the measurement, on the review's M1): LongRunAnchorsDiagnostic (d3)'s USA marks

**Not preregistered.** The rule below was written after R1 was measured, and it is dated so. With R1 in, the player's idle USA book gains +6.03 / +6.94 /
+7.78 years of life expectancy at years 50 / 60 / 70 (`diag.sh LongRunAnchorsDiagnostic`). The sourced gains are +5.68 / +6.58 / +7.38, +/-0.3, so the
marks are +0.05 / +0.06 / +0.10 past the band. Before R1 they read +5.78 / +6.68 / +7.51, inside.

**Cause:** the health lines at the going wage lower the health family's treatable excess. This is K222's declared M1 drift, which R1 brings into the
player's book. Without the treatable excess the gains are +5.81 / +6.70 / +7.54, inside the band.

**The rule, ruled as K298:** exactly three marks are REGISTERED rather than failed - the USA at years 50, 60 and 70. Each is registered only while both
hold:
- the gain without the treatable excess is inside the band;
- the treatable part of the gain (the gain less the gain without it) is at most 0.5 year.

Any other (d3) mark outside its band fails: another country, another year, a larger treatable part, or a rest outside the band. No band was widened.

## Amendment 2 (2026-10-30, written AFTER the measurement, on the rebase onto main 54c23ca9): ParticipationCohortDiagnostic (c) Poland 2050

**Not preregistered.** The rule below was written after the measurement, and it is dated so. Out of the engine (`diag.sh ParticipationCohortDiagnostic`)
on the rebased branch, Poland's labour force aged 20-74 at 2050 reads +3.03 % against the Ageing Report's (band +/-3 %). Main alone, without fisc2, reads
+2.47 %. Poland 2040 (+2.49 %) and 2070 (-0.99 %) are inside, as is every other (c), (d1), (d2) and (d3) line.

**Attribution** (the diagnostic's own lines, Poland 2050, branch against main):
- The 15+ population is x0.9300 against x0.9271, adding +0.31 %. This is the mortality route: the health lines at the going wage lower treatable mortality.
- The state's participation rate over its anchor is 1.0242 against 1.0221, adding +0.21 %. Within it, the health term is +0.13 points (main -0.02), the
  education term +0.03 points, and the unemployment gap's term +0.07 points.

This is the mechanism K298 registered for the USA's life expectancy: K222's declared M1 drift, the health and education lines at the going wage read as
capacity, brought into the player's book by R1.

**The rule, ruled as K313:** exactly one (c) line is REGISTERED rather than failed - Poland 2050. It stays registered only while both hold:
- (i) the line with the health and education terms removed is inside +/-3 %;
- (ii) their part - the line less the line with them removed - is at most one point.

"The line with them removed" is what the diagnostic can compute: the model's labour force with the state's rate lowered by the two terms at the year and at
the base year. That is the participation route. The mortality route through the 15+ population is not separable from the run's own pyramid, so it stays
inside "the line without them" and is not bounded by (ii). Any other (c) miss fails, and Poland 2050 fails once either bound is broken. No band was widened.
