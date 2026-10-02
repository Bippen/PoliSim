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
| Education | Science & Education 0.82 (and Research & Technology 0.93, the BMBF's other post) | Education (& Science) 1.07 | Education 1.40 | Education 1.10 |

## The mapping onto the game's six portfolios (DERIVED)

The game's six portfolios stand for ministries: **a portfolio that stands for several of the country's separate ministries weighs their
sum; one that stands for one ministry weighs its score**. Italy's MEF, one ministry of today that merged three posts the paper rates (Treasury, Finance and Budget), weighs their
SUM, as the paper does (Elias's ruling of 2026-10-01, item 8, §717; it weighed the highest, 1.64, before). The miss this makes against the
record is recorded and the method is not adjusted: Meloni's Finance goes to FdI, the record's minister is Giorgetti (Lega).

| game portfolio | Germany | Sweden | France | Italy | the rest (mean of the four) |
|---|---:|---:|---:|---:|---:|
| head of government (credited, not allocated) | 2.12 | 2.19 | 2.75 | 2.48 | 2.385 |
| FinanceTreasury | 1.58 | 1.68 | 2.98 (1.92 + Industry 1.06, from the decree of 20 May 2022 - §753) | 3.94 (1.64 + 1.32 + 0.98) | 2.545 |
| InteriorJustice | 2.29 (1.27 + 1.02) | 0.99 | 3.11 (1.63 + 1.48) | 3.01 (1.78 + 1.23) | 2.35 |
| HealthSocialAffairs | 2.01 (1.21 + 0.80) | 2.48 (1.22 + 1.26) | 2.12 (1.13 + 0.99) | 2.25 (1.06 + 1.19) | 2.215 |
| Defense | 1.12 | 0.99 | 1.38 | 1.19 | 1.17 |
| ForeignAffairs | 1.41 | 1.27 | 1.45 | 1.69 | 1.455 |
| Education | 1.75 (0.82 + 0.93, the BMBF) for a cabinet of the 2021 chamber; 1.50 (0.82 + 0.68, the BMBFSFJ) from the 2025 chamber - §753 | 1.07 | 1.40 | 1.10 | 1.33; 1.2675 from 2025-02-23 |

**Poland and the USA** take the mean of the four countries rated (DERIVED — the play-calibration list's 23rd entry, key `SAL-POL`, owed
Druckman & Roberts' Polish table; the USA's executive is a presidency, one party, so no share is ever split there).

## The allocation (the method [AUTHORED-DRAFT] — the play-calibration list's 24th entry — the weights sourced)

Gamson's law with salience (Druckman & Warwick's own use of the scores, "salience-weighted portfolio payoffs"):
1. each cabinet party's entitlement is its share of the cabinet's seats times the total weight (the head of government's weight plus the
   six portfolios');
2. the head of government's party is credited the head's weight;
3. the portfolios, heaviest first, each go to the party with the largest entitlement still outstanding (a tie to the larger party);
4. the head's party holds at least one portfolio (the rule before this ruling, kept; the head's party passes every lever gate but Finance's where a partner holds Finance - §716: that partner holds its levers in fact, run from its own positions).



**Merged ministries, Germany included** (Elias's ruling A3, 2026-10-02, §753): *coded as Druckman & Warwick code them*. The paper's own
rule (DW05, p. 26 of the authors' copy): where posts it rated separately are merged, *"it is possible to produce a salience value by summing
the ratings of the component posts"* - its example, Ireland's justice (1.24) and communications (0.91) merged in 1989, weighs 2.15; a post
combining a rated and an unrated area weighs the rated one plus 1; a post split in two takes half each; and it warns that summing *"may not
be perfectly accurate"* (one could downgrade the sum) - not done here, the paper's own rule taken as written. The paper codes each CABINET's
posts as they stood, so the weights are **DATED** (`PortfolioSalience.Weight(country, post, day)`; the day a cabinet is formed):
- **Germany's Education**: the BMBF (1994-2025) merged Science & Education (0.82) and Research & Technology (0.93) - **1.75** for a cabinet
  of the 2021 chamber; the 2025 chamber's government split it (6 May 2025: research to the BMFTR, education to the BMBFSFJ with family
  affairs - the cabinet page in `germany/raw/records/breg_bundeskabinett.html`), so a cabinet formed on the 2025 chamber - keyed on its
  ELECTION, 2025-02-23, one structure through the whole formation - weighs **1.50** (Science & Education 0.82 + Families & Youth 0.68);
- **France's Finance**: Bercy has carried industry since the Borne government - the *Décret du 20 mai 2022 relatif à la composition
  du Gouvernement*, art. 1: *"M. Bruno LE MAIRE, ministre de l'économie, des finances et de la souveraineté industrielle et numérique"*
  (`france/raw/executive/wb_legifrance_JORFTEXT000045819551.html`, an archived copy - Légifrance refused the live page, 403; *"... et de
  l'industrie"* in the JORF of 2024-12-14; the JORF of 2022-05-17 still reads *"et de la relance"*), keyed on 2022-05-20 -
  Economy & Finance 1.92 + Industry 1.06 = **2.98**;
- **the mean** Poland and the USA take is computed from the four at the same day: Finance **2.545**, Education 1.33 (1.2675 from 2025-02-23)
  - so Poland's and the USA's weights move on German and French reorganisation dates (a premise, stated).
Checked and left: Italy's Education is the post that holds education (Istruzione; Universities & Research, 0.69, a separate ministry not
among the six); Sweden's Education is one post; the portfolios standing for several separate ministries already sum them (§706).
**The outcomes** (`PortfolioSalienceDiagnostic`): the 2021 chamber's Finance goes to **the SPD** - a MISS against the record's Lindner (FDP),
recorded and not adjusted (Education stays with the FDP, the record's Stark-Watzinger); the 2025 chamber's to **the SPD**, the record's
Klingbeil (posts CDU Health/Education, SPD Interior/Finance/Defence, CSU Foreign), and the formation forms the record's CDU+CSU+SPD;
Poland's Finance stays with KO (the record's Domański) but TD and NL swap Foreign and Education (KO Finance/Interior/Defence, TD Health/
Foreign, NL Education); France's stand-in seats the head's ENS at Finance (RN before; no partner runs a stand-in either way, §716's
decision A); Sweden's and Italy's hold. The no-player trajectory does not move (`fp717`, byte for byte).

**The ruling's tests** (`PortfolioSalienceDiagnostic`): the 2025 chamber's CDU+CSU+SPD puts Finance with **the SPD** (Klingbeil, the
record); the 2021 chamber's SPD+Grüne+FDP puts it with **the FDP** (Lindner, the record) - **until §753**: since then 2021's goes to the SPD, a recorded miss (above) — and Education with the FDP too, as the record
has it (Stark-Watzinger).
