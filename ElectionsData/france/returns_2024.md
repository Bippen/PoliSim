# France — Legislative 2024 returns + rules [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; overnight 2026-08-28→29, research agent; the Ministry's archived
official portal + its own data.gouv régions CSV + Légifrance statute text). `[PROVISIONAL]`
until re-verified (R-K9). Classification note carried prominently: the national table uses the
MINISTRY'S NUANCE GRID — bloc composition differs from press tallies, stated in the caveats;
any model mapping to game blocs must state its own regrouping explicitly.

## FRANCE — Legislative elections, 2024-06-30 / 2024-07-07
### Source register
- returns: https://www.archives-resultats-elections.interieur.gouv.fr/resultats/legislatives2024/ensemble_geographique/index.php (Ministère de l'Intérieur, archived official results portal — the former https://www.resultats-elections.interieur.gouv.fr/legislatives2024/ 301-redirects here; accessed 2026-08-28; basis: "Résultats proclamés par les commissions de recensement", France entière, both rounds; Ministry **nuance** classification used throughout)
- returns (regional): https://static.data.gouv.fr/resources/elections-legislatives-des-30-juin-et-7-juillet-2024-resultats-definitifs-du-1er-tour/20240710-171318/resultats-definitifs-par-regions.csv — file `resultats-definitifs-par-regions.csv` of dataset "Élections législatives des 30 juin et 7 juillet 2024 - Résultats définitifs du 1er tour", publisher Ministère de l'Intérieur, https://www.data.gouv.fr/datasets/elections-legislatives-des-30-juin-et-7-juillet-2024-resultats-definitifs-du-1er-tour (accessed 2026-08-28; nuance votes ÷ exprimés aggregated per région from the raw counts)
- rules: https://www.legifrance.gouv.fr/codes/section_lc/LEGITEXT000006070239/LEGISCTA000006148464/ (L123–L126) · https://www.legifrance.gouv.fr/codes/article_lc/LEGIARTI000006353299 (L126) · https://www.legifrance.gouv.fr/codes/article_lc/LEGIARTI000006353380 (L162) · https://www.legifrance.gouv.fr/codes/article_lc/LEGIARTI000020103138 (LO119) — all Légifrance, accessed 2026-08-28

### National result
Classification: the Ministry's own nuance grid (portal above). Shares = % of suffrages exprimés, 1st round, France entière. Seats = final composition ("Sièges par nuance", both rounds combined), sum = 577.

| bloc/party (name, abbrev) | 1st-round vote share % | final seats |
|---|---|---|
| Union de la gauche (UG — Ministry label for the NFP joint candidacies) | 28.06 | 178 |
| — NFP parties running outside the UG banner: Parti socialiste (SOC) | 0.09 | 2 |
| — La France insoumise (FI) | 0.04 | 0 |
| — Parti communiste français (COM) | 0.01 | 0 |
| — Les Écologistes (VEC) | 0.01 | 0 |
| — Parti radical de gauche (RDG) | 0.04 | 0 |
| Ensemble ! Majorité présidentielle (ENS) | 20.04 | 150 |
| — Horizons (HOR) | 0.72 | 6 |
| — UDI | 0.51 | 3 |
| — Renaissance (REN) / Modem (MDM) as separate nuances | 0.00 | 0 |
| Rassemblement National (RN) | 29.26 | 125 |
| — Union de l'extrême droite (UXD — RN–Ciotti alliance) | 3.96 | 17 |
| Les Républicains (LR) | 6.57 | 39 |
| Divers droite (DVD) | 3.60 | 27 |
| Divers gauche (DVG) | 1.53 | 12 |
| Régionaliste (REG) | 0.97 | 9 |
| Divers centre (DVC) | 1.22 | 6 |
| Extrême gauche (EXG) | 1.14 | 0 |
| Reconquête ! (REC) | 0.75 | 0 |
| Écologistes (ECO) | 0.57 | 1 |
| Divers (DIV) | 0.45 | 1 |
| Droite souverainiste (DSV) | 0.28 | 0 |
| Extrême droite (EXD) | 0.19 | 1 |

Turnout: 1st round 66.71%, 2nd round 66.63% (votants/inscrits, France entière; R1 inscrits 49,332,709, votants 32,908,657; R2 inscrits 43,328,508, votants 28,867,759 — R2 basis covers only the seats contested in round 2)
Total seats: 577

### Regional table
Source: Ministry's own région-level aggregation (`resultats-definitifs-par-regions.csv`, data.gouv.fr, above). Shares = nuance votes / exprimés, 1st round, computed from the file's raw counts. Top three nuances per région (Ministry classification; UXD not merged into RN — see caveats):

| Région | 1st | 2nd | 3rd |
|---|---|---|---|
| Île-de-France | UG 38.59 | ENS 23.45 | RN 16.41 |
| Hauts-de-France | RN 41.84 | UG 22.59 | ENS 13.83 |
| Provence-Alpes-Côte d'Azur | RN 39.04 | UG 24.00 | ENS 18.36 |
| Auvergne-Rhône-Alpes | UG 29.00 | RN 26.24 | ENS 18.33 |
| Occitanie | RN 32.71 | UG 30.90 | ENS 19.54 |
| Nouvelle-Aquitaine | RN 31.76 | UG 28.74 | ENS 20.24 |
| Bretagne | ENS 29.72 | UG 29.63 | RN 27.66 |
| Grand Est | RN 37.69 | UG 21.26 | ENS 18.68 |
| Pays de la Loire | UG 28.95 | ENS 25.46 | RN 22.49 |
| Normandie | RN 33.76 | UG 26.30 | ENS 15.97 |
| Centre-Val de Loire | RN 32.35 | UG 22.64 | ENS 22.37 |
| Bourgogne-Franche-Comté | RN 33.87 | UG 24.22 | ENS 20.19 |
| Corse | REG 34.58 | RN 29.93 | DVD 14.04 |

(Portal navigation itself is national → département → circonscription; the région granularity comes from the Ministry's data.gouv file, not the portal pages.)

### Electoral rules
- 577 single-member seats: "Le nombre des députés est de cinq cent soixante-dix-sept." — Art. LO119, https://www.legifrance.gouv.fr/codes/article_lc/LEGIARTI000020103138
- Two-round majority vote in single-member circonscriptions, no proportional element: "Les députés sont élus au scrutin uninominal majoritaire à deux tours" (Art. L123); "Le vote a lieu par circonscription" (Art. L124) — https://www.legifrance.gouv.fr/codes/section_lc/LEGITEXT000006070239/LEGISCTA000006148464/
- 1st-round outright win: absolute majority of exprimés AND ≥ 25% of registered voters — "Nul n'est élu au premier tour de scrutin s'il n'a réuni : 1° La majorité absolue des suffrages exprimés ; 2° Un nombre de suffrages égal au quart du nombre des électeurs inscrits." — Art. L126, https://www.legifrance.gouv.fr/codes/article_lc/LEGIARTI000006353299
- Qualification for round 2: ≥ 12.5% of REGISTERED voters — "nul ne peut être candidat au deuxième tour s'il ne s'est présenté au premier tour et s'il n'a obtenu un nombre de suffrages au moins égal à 12,5 % du nombre des électeurs inscrits"; if only one (or none) qualifies, the next-best-placed candidate(s) may stand — Art. L162, https://www.legifrance.gouv.fr/codes/article_lc/LEGIARTI000006353380
- Plurality wins round 2: "Au deuxième tour la majorité relative suffit." — Art. L126 (same URL as above)

### Caveats
- Bloc classification is the Ministry's nuance grid, which is NOT identical to press/party bloc tallies. "UG" is the Ministry's label for joint left (NFP) candidacies; NFP-endorsed candidates who ran under SOC/FI/DVG/REG etc. nuances are counted there, so press seat counts for NFP (~180–182) exceed UG's 178. "Ensemble" press tallies usually add HOR and UDI (150+6+3); "RN + allies" usually means RN + UXD (125+17 = 142 seats; 1st-round 29.26 + 3.96 = 33.22%), sometimes also the single EXD seat. Assemblée nationale parliamentary-group counts differ again from all of the above.
- Regional table: UXD is kept separate per the Ministry grid; merging it into RN would change some rankings' margins (e.g. Occitanie RN+UXD = 38.63%) but not the top-3 membership in the eight required régions. The région CSV covers régions only — overseas collectivities (Wallis-et-Futuna, Polynésie, Nouvelle-Calédonie, St-Pierre-et-Miquelon, St-Martin/St-Barth) and Français de l'étranger are outside it, which is why its national sum (32.09M votants) is below the France-entière total (32.91M).
- Seats in the "1st-round vote share" table rows are final (both rounds); 76 seats were filled in round 1 (sum of the portal's tour-1 seat column), 501 in round 2 — both from the same portal page.
- No figure above is from a secondary source; nothing is [UNCONFIRMED].

*(Filed verbatim from the research agent's return, 2026-08-28 night. Note for any future
seat-model: France's two-round SMD system is not backtestable from national shares at all —
it needs per-circonscription rounds; the night's backtest treats France accordingly — see the
morning report.)*
