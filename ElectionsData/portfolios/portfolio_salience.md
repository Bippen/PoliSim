# Portfolio salience — what a cabinet post is worth in the Gamson share [SOURCED, the mapping DERIVED]

Class: SOURCED scores, DERIVED mapping (Elias's ruling of 2026-10-01, `COMPLETED.md` §706). The game reads these weights from
`Assets/Scripts/Elections/PortfolioSalience.cs`; this file is their record.

## The ruling

> *Lift the Treasury lock. Finance is a portfolio like any other and can go to a partner; the record has the SPD at Finance under Merz in
> 2025, and the FDP held it under Scholz in 2021. It counts for more than one post in the Gamson share: use a published portfolio-salience
> weight if one can be fetched (Druckman & Warwick's measure), otherwise [AUTHORED-DRAFT] on the calibration list. Whoever holds Finance
> holds its levers, as §634 rules for every post. Tests: the 2021 and 2025 chambers put Finance with the party that actually held it.*

## The source

James N. Druckman and Paul V. Warwick, "The missing piece: Measuring portfolio salience in Western European parliamentary democracies",
*European Journal of Political Research* 44 (2005): 17–42 [DW05]. An expert survey (163 respondents, 14 countries): each post scored
against an average portfolio of 1.00 ("if you believe the Finance portfolio is about 50% more important than an average portfolio, it
should be given a score of 1.50"). The appendix gives each country's mean per post. Saved whole: the authors' own copy,
`raw/druckman_warwick_ejpr_2005.pdf` (fetched 2026-10-01 from https://faculty.wcas.northwestern.edu/jnd260/pub/Druckman%20Warwick%20EJPR%202005.pdf,
137,729 bytes, SHA-256 `6e3ccc840a192fdd7c7e913c733629d50cb1f44cbffc7c75be6c6d4e225ec69b`).

The Eastern European companion (Druckman & Roberts, EJPR 47, 2008) would give Poland's; the authors' copy is served truncated
(65,536 bytes, the server's own Content-Length), its per-post appendix cut: only Poland's summary survives (average 0.96, maximum 2.21).

## The scores used (DW05's appendix, the country's mean per post)

| post | Germany | Sweden | France (V) | Italy |
|---|---:|---:|---:|---:|
| head of government | Chancellor **2.12** | PM **2.19** | PM **2.75** | PM **2.48** |
| Finance | Finance **1.58** | Finance **1.68** | Economy & Finance **1.92** | Treasury **1.64** (Finance 1.32, Budget 0.98) |
| Interior | Interior 1.27 | — (not rated) | Interior 1.63 | Interior 1.78 |
| Justice | Justice 1.02 | Justice 0.99 | Justice 1.48 | Justice 1.23 |
| Labour / social | Labour (& Social Affairs) 1.21 | Health & Social Affairs/Welfare 1.22; Labour/Employment 1.26 | Employment 1.13 | Labour & Social Security/Welfare 1.06 |
| Health | Health 0.80 | (in the above) | Public Health 0.99 | Health 1.19 |
| Defence | 1.12 | 0.99 | 1.38 | 1.19 |
| Foreign Affairs | 1.41 | 1.27 | 1.45 | 1.69 |
| Education | Science & Education 0.82 | Education (& Science) 1.07 | Education 1.40 | Education 1.10 |

## The mapping onto the game's six portfolios (DERIVED)

The game's six portfolios stand for ministries: **a portfolio that stands for several of the country's separate ministries weighs their
sum; one that stands for one ministry weighs its score**. Where one ministry of today merged several rated posts (Italy's MEF: Treasury,
Finance and Budget), it weighs the highest of them — a merged ministry is one post, not three.

| game portfolio | Germany | Sweden | France | Italy | the rest (mean of the four) |
|---|---:|---:|---:|---:|---:|
| head of government (credited, not allocated) | 2.12 | 2.19 | 2.75 | 2.48 | 2.385 |
| FinanceTreasury | 1.58 | 1.68 | 1.92 | 1.64 | 1.705 |
| InteriorJustice | 2.29 (1.27 + 1.02) | 0.99 | 3.11 (1.63 + 1.48) | 3.01 (1.78 + 1.23) | 2.35 |
| HealthSocialAffairs | 2.01 (1.21 + 0.80) | 2.48 (1.22 + 1.26) | 2.12 (1.13 + 0.99) | 2.25 (1.06 + 1.19) | 2.215 |
| Defense | 1.12 | 0.99 | 1.38 | 1.19 | 1.17 |
| ForeignAffairs | 1.41 | 1.27 | 1.45 | 1.69 | 1.455 |
| Education | 0.82 | 1.07 | 1.40 | 1.10 | 1.0975 |

**Poland and the USA** take the mean of the four countries rated (DERIVED — the play-calibration list's 23rd entry, key `SAL-POL`, owed
Druckman & Roberts' Polish table; the USA's executive is a presidency, one party, so no share is ever split there).

## The allocation (the method [AUTHORED-DRAFT] — the play-calibration list's 24th entry — the weights sourced)

Gamson's law with salience (Druckman & Warwick's own use of the scores, "salience-weighted portfolio payoffs"):
1. each cabinet party's entitlement is its share of the cabinet's seats times the total weight (the head of government's weight plus the
   six portfolios');
2. the head of government's party is credited the head's weight;
3. the portfolios, heaviest first, each go to the party with the largest entitlement still outstanding (a tie to the larger party);
4. the head's party holds at least one portfolio (the rule before this ruling, kept; the head's party passes every lever gate anyway - the post is its minister's).

**The ruling's tests** (`PortfolioSalienceDiagnostic`): the 2025 chamber's CDU+CSU+SPD puts Finance with **the SPD** (Klingbeil, the
record); the 2021 chamber's SPD+Grüne+FDP puts it with **the FDP** (Lindner, the record) — and Education with the FDP too, as the record
has it (Stark-Watzinger).
