# Sweden — Riksdag 2022 returns + rules [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; overnight 2026-08-28→29, research agent; Valmyndigheten's official
JSON backend, the comparative-statistics xlsx, the formal decision PDF with the applied
divisors). `[PROVISIONAL]` until re-verified (R-K9). This is the backtest's PRIME ANCHOR
(overhaul doc Part 5: Sweden 2022 national modified Sainte-Laguë reproduces the chamber
exactly). National counts: cast 6,547,801 / valid 6,477,970; **exact per-party counts below —
the Part 5 constraint (counts, never shares) is fully honoured for the anchor country.**

## National result — EXACT COUNTS (follow-up, same sources)

| party | votes (integer) | share % (as RD_S.json prints, sv decimal comma) |
|---|---|---|
| Arbetarepartiet-Socialdemokraterna (S) | 1964474 | 30,33 |
| Sverigedemokraterna (SD) | 1330325 | 20,54 |
| Moderaterna (M) | 1237428 | 19,1 |
| Vänsterpartiet (V) | 437050 | 6,75 |
| Centerpartiet (C) | 434945 | 6,71 |
| Kristdemokraterna (KD) | 345712 | 5,34 |
| Miljöpartiet de gröna (MP) | 329242 | 5,08 |
| Liberalerna, tidigare Folkpartiet (L) | 298542 | 4,61 |

Valid votes total (Giltiga röster): 6477970. Counts source: the RD_S.json backend
(rakningstillfalle "slutlig"), fields antalRoster/andelRoster per party — identical in the
"Riket" sheet of the comparative-statistics xlsx (which stores shares as exact fractions,
e.g. 0.3032545689467534).

## SWEDEN — Riksdag election, 2022-09-11
### Source register
- returns: https://resultat.val.se/data/resultat/val2022/RD_S.json (Valmyndigheten — official data backend of the result presentation https://resultat.val.se/val2022/RD?r=S, accessed 2026-08-28, basis: "rakningstillfalle": "slutlig" [final count], last updated 19 October 2022 13:51)
- returns (per constituency): https://www.val.se/download/18.162047b519a91d05331197bd/1667207094207/slutligt-valresultat-riksdagen-jamforande-statistik-2018-2022.xlsx (Valmyndigheten, "Slutligt valresultat riksdagen — jämförande statistik 2018–2022", sheets "Riket" and "Valkrets", downloaded and parsed 2026-08-28, basis: official final result)
- returns (summary/turnout): https://www.val.se/english/election-results/elections-to-the-riksdag-and-regional-and-municipal-councils/election-results-2022 (Valmyndigheten, accessed 2026-08-28, basis: official final result)
- returns (formal decision): https://www.val.se/download/18.162047b519a91d05331146ae/1663951746642/Riksdag%20val%202022%20Beslut%20med%20bilagor%20-%20med%20r%C3%A4ttelse%20enligt%20FL.pdf (Valmyndigheten decision with annexes, downloaded and text-extracted 2026-08-28)
- rules: https://www.val.se/det-svenska-valsystemet/rostrakning-och-mandatfordelning/sa-fordelas-mandaten and https://www.val.se/det-svenska-valsystemet/rostrakning-och-mandatfordelning/sa-utses-ledamoter (Valmyndigheten, accessed 2026-08-28); decision PDF Bilaga 3–4 (above) for the applied method

### National result
| party (full name, abbrev) | vote share % | seats |
|---|---|---|
| Arbetarepartiet-Socialdemokraterna (S) | 30.33 | 107 |
| Sverigedemokraterna (SD) | 20.54 | 73 |
| Moderaterna (M) | 19.10 | 68 |
| Vänsterpartiet (V) | 6.75 | 24 |
| Centerpartiet (C) | 6.71 | 24 |
| Kristdemokraterna (KD) | 5.34 | 19 |
| Miljöpartiet de gröna (MP) | 5.08 | 18 |
| Liberalerna, tidigare Folkpartiet (L) | 4.61 | 16 |
| Partiet Nyans (PNy) — largest non-parliament party | 0.44 (28,352 votes) | 0 |

Turnout: 84.21% (basis: 6,547,801 votes cast of 7,775,390 eligible voters = 0.8421187, per the official xlsx "Valdeltagande"/"Röstberättigade" rows; cast votes include invalid/blank ballots)
Total seats: 349

### Regional table
Source: sheet "Valkrets" of Valmyndigheten's official file slutligt-valresultat-riksdagen-jamforande-statistik-2018-2022.xlsx (URL above); shares are the file's raw "Andel 2022" fractions × 100, rounded to 0.01. All 29 constituencies. Columns: national top four (S, SD, M, V) plus C (near-tie with V nationally).

| valkrets | S % | SD % | M % | V % | C % |
|---|---|---|---|---|---|
| Blekinge län | 31.14 | 28.53 | 17.86 | 4.44 | 4.84 |
| Dalarnas län | 31.66 | 25.69 | 16.43 | 5.33 | 6.50 |
| Gotlands län | 34.64 | 15.69 | 16.81 | 6.37 | 11.72 |
| Gävleborgs län | 34.73 | 24.09 | 16.24 | 5.91 | 6.25 |
| Göteborgs kommun | 27.65 | 14.66 | 18.48 | 12.85 | 5.86 |
| Hallands län | 28.27 | 22.58 | 22.47 | 4.04 | 7.03 |
| Jämtlands län | 36.07 | 20.11 | 14.79 | 5.59 | 9.14 |
| Jönköpings län | 29.05 | 23.28 | 18.73 | 3.96 | 7.45 |
| Kalmar län | 31.74 | 24.50 | 17.78 | 4.64 | 6.53 |
| Kronobergs län | 30.97 | 23.61 | 19.51 | 5.03 | 6.05 |
| Malmö kommun | 29.57 | 16.37 | 17.87 | 12.49 | 5.49 |
| Norrbottens län | 41.64 | 20.30 | 13.57 | 6.98 | 5.29 |
| Skåne läns norra och östra | 25.20 | 32.19 | 19.51 | 3.94 | 4.95 |
| Skåne läns södra | 25.35 | 23.35 | 22.06 | 4.96 | 6.62 |
| Skåne läns västra | 27.34 | 28.75 | 19.82 | 4.61 | 4.97 |
| Stockholms kommun | 28.07 | 10.67 | 19.07 | 11.73 | 8.48 |
| Stockholms län | 27.12 | 17.55 | 24.01 | 6.28 | 7.39 |
| Södermanlands län | 32.93 | 23.01 | 19.21 | 5.20 | 5.94 |
| Uppsala län | 29.13 | 18.18 | 18.26 | 7.85 | 7.25 |
| Värmlands län | 34.59 | 22.80 | 17.05 | 5.01 | 6.34 |
| Västerbottens län | 40.73 | 14.46 | 14.15 | 8.50 | 7.79 |
| Västernorrlands län | 39.42 | 20.68 | 13.97 | 5.75 | 7.45 |
| Västmanlands län | 32.00 | 23.67 | 19.13 | 6.13 | 5.40 |
| Västra Götalands läns norra | 31.28 | 25.43 | 17.53 | 5.16 | 5.72 |
| Västra Götalands läns södra | 29.14 | 23.59 | 18.92 | 5.34 | 7.09 |
| Västra Götalands läns västra | 28.03 | 21.20 | 20.46 | 5.68 | 6.41 |
| Västra Götalands läns östra | 31.40 | 24.12 | 18.58 | 4.45 | 6.61 |
| Örebro län | 33.25 | 22.09 | 16.74 | 6.11 | 6.26 |
| Östergötlands län | 30.55 | 21.20 | 19.83 | 5.62 | 6.48 |

### Electoral rules
- System: proportional (list-PR) election to a 349-seat Riksdag, counted in 29 valkretsar — https://www.val.se/det-svenska-valsystemet/rostrakning-och-mandatfordelning/sa-fordelas-mandaten and decision PDF Bilaga 4 ("Antal fasta mandat 310, Antal utjämningsmandat 39, Totalt antal mandat 349").
- Tiers: "310 fasta valkretsmandat" (fixed constituency seats) + "39 utjämningsmandat" (adjustment seats) = 349 — https://www.val.se/det-svenska-valsystemet/rostrakning-och-mandatfordelning/sa-fordelas-mandaten; confirmed in the 2022 decision PDF (URL in source register).
- Threshold: a party shares in seats only with "4 procent i hela landet eller 12 procent i en valkrets" (4% nationally, or 12% in a given constituency for that constituency's fixed seats) — https://www.val.se/det-svenska-valsystemet/rostrakning-och-mandatfordelning/sa-fordelas-mandaten.
- Divisor method: jämkade uddatalsmetoden (modified Sainte-Laguë) — a party's votes are divided by 1.2 for its first seat, then 3, 5, 7, 9, …; each seat goes to the party with the highest comparison number — https://www.val.se/det-svenska-valsystemet/rostrakning-och-mandatfordelning/sa-fordelas-mandaten; applied divisors 1.2/3/5/7… visible in the 2022 decision PDF Bilaga 3–4 (e.g. S first quotient 1,637,061.67 = 1,964,474/1.2).
- Adjustment (utjämningsmandat) mechanism: a national "totalfördelning" is computed treating "hela landet som en valkrets" with the same method; parties that received fewer fixed constituency seats than their national entitlement get the 39 adjustment seats, placed one at a time in the constituency where the party's comparison number is largest (parties with no fixed seat can also receive them) — decision PDF Bilaga 4 and https://val.se/valresultat/om-rostrakning-och-valresultat/mandatfordelning.html (redirects to the sa-fordelas-mandaten page above).
- Personal votes: candidates are elected first on personal votes; the threshold for the Riksdag is "5 procent av partiets röster i valkretsen"; where seats cannot be filled by personal votes, "utses de utifrån namnordningen på valsedlarna" (ballot list order) — https://www.val.se/det-svenska-valsystemet/rostrakning-och-mandatfordelning/sa-utses-ledamoter. Personal-vote share cast in 2022: 22.49% — val.se English results page (source register).

### Caveats
- "Övriga anmälda partier" (all non-parliament parties combined): 100,252 votes; the official JSON gives their share as 1.55% (= 100,252/6,477,970 valid votes = 1.5476%), while val.se's Swedish and English summary pages display "1.54%" — a rounding/truncation discrepancy on val.se's own pages; the underlying counts agree exactly.
- Partiet Nyans figures (28,352; 0.44%) are from the official slutlig JSON (resultat.val.se); cross-checked against https://www.gu.se/sites/default/files/2022-09/2022_15_Valresultat_2022.pdf and https://www.fokus.se/politik/hela-listan-sa-manga-roster-fick-smapartierna-i-valet-2022/ (identical). Runners-up among non-parliament parties: Alternativ för Sverige 16,646 (0.26%), Medborgerlig Samling 12,882 (0.20%), Piratpartiet 9,135 (0.14%) — same official JSON. No figure is [UNCONFIRMED].
- "Top four" in the regional table = national top four (S, SD, M, V); C added as a fifth column (6.71 vs V's 6.75 nationally). In some constituencies the locally fourth party is another one (e.g. KD in Jönköpings län over V's 3.96%) — the table is "national top four per constituency", not "each constituency's own top four".
- Regional shares are computed (fraction × 100, half-up to 0.01) from the raw "Andel 2022" values in the xlsx (the file stores exact fractions); the underlying vote counts are the official final counts.
- Turnout definition: val.se's "valdeltagande" = all ballots cast (incl. blank/invalid) ÷ eligible voters; 84.21% matches the exact xlsx fraction 0.8421187. SCB independently states 84.2% (https://www.scb.se/hitta-statistik/statistik-efter-amne/demokrati/allmanna-val/allmanna-val-valresultat/pong/statistiknyhet/allmanna-val-valresultat-2022/).
- The resultat.val.se JSON endpoint is the machine-readable backend of the official SPA presentation; per-constituency JSONs exist at …/data/resultat/val2022/RD_{valkretskod}_S.json but were not needed (the xlsx covers all 29).

*(Filed verbatim from the research agent's return, 2026-08-28 night. Exact per-party national
counts follow-up is in flight — the backtest's precision gate.)*
