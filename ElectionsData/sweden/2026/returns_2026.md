# Sweden — Riksdag 2026 returns + rules [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; K-1 sourcing pass 2026-09-23, research agent; Valmyndigheten's official
JSON backend (final count), the formal decision PDF with its nine appendices, and the final-count
xlsx workbook, all fetched 2026-09-23 and saved byte-exact under `raw/`). `[PROVISIONAL]` until a
second session re-verifies (R-K9). Election day 2026-09-13; **result fixed (fastställt) 2026-09-19 by
Valmyndigheten's decision Dnr VAL-735-2026** ("Fördelning av mandat i riksdagen och fastställande av
vilka kandidater som valts till ledamöter och ersättare", decided by Valmyndighetens nämnd).
National counts: cast 6,834,413 / valid 6,767,429 / eligible 8,051,355. **Exact per-party counts
below — the Part 5 constraint (counts, never shares) is fully honoured.** 2022 stays the backtest's
reference (`../returns_2022.md`); this file is the 2026 chamber.

**Every figure in this file was read from a file in `raw/` (no figure is recalled). Three independent
official sources agree on every count: the JSON backend, the decision PDF (Bilaga 1, 3, 4, 5, 6), and
the final-count workbook — see "Verification" below.**

## National result — EXACT COUNTS (RD_S.json = decision Bilaga 1)

| party | votes (integer) | share % (as RD_S.json prints) | exact share % (DERIVED: votes / valid) | seats | of which fixed | of which adjustment | seats 2022 (RD_S.json antalMandatForegaendeVal) |
|---|---|---|---|---|---|---|---|
| Arbetarepartiet-Socialdemokraterna (S) | 1895989 | 28.02 | 28.016386 | 99 | 94 | 5 | 107 |
| Moderaterna (M) | 1343448 | 19.85 | 19.851675 | 70 | 68 | 2 | 68 |
| Sverigedemokraterna (SD) | 1183248 | 17.48 | 17.484454 | 62 | 60 | 2 | 73 |
| Vänsterpartiet (V) | 568781 | 8.4 | 8.404684 | 30 | 27 | 3 | 24 |
| Centerpartiet (C) | 475780 | 7.03 | 7.030439 | 25 | 17 | 8 | 24 |
| Kristdemokraterna (KD) | 417490 | 6.17 | 6.169108 | 22 | 20 | 2 | 19 |
| Miljöpartiet de gröna (MP) | 414307 | 6.12 | 6.122074 | 22 | 13 | 9 | 18 |
| Liberalerna (tidigare Folkpartiet) (L) | 361187 | 5.34 | 5.337138 | 19 | 11 | 8 | 16 |
| Övriga anmälda partier (all other registered parties) | 107199 | 1.58 | 1.584043 | 0 | 0 | 0 | 0 |
| **Total** | **6767429** | 100 | | **349** | **310** | **39** | 349 |

Valid votes total (Summa giltiga röster / rosterPaverkaMandat.antalRoster): 6767429.
Invalid votes (Ogiltiga röster): 66984 (0.98 % of cast), of which: votes for parties not registered to
take part (ej anmälda partier) 1287 (0.02 %); blank (blanka) 59557 (0.87 %); other invalid (övriga
ogiltiga) 6140 (0.09 %). Cast (Summa avgivna röster): 6834413 = 6767429 + 66984. Eligible
(Röstberättigade): 8051355. Turnout (Valdeltagande): **84.89 %** as printed (exact 6834413 / 8051355 =
0.8488525). Voting districts counted: 6626 of 6626.
Personal votes (Bilaga 1, all parties): 1637668 = 24.20 % of valid votes; per party (count, % of the
party's votes): S 353591 (18.65), M 258700 (19.26), SD 414098 (35.00), V 164557 (28.93), C 93326
(19.62), KD 141840 (33.97), MP 92946 (22.43), L 69600 (19.27), Övriga 49010 (45.72).

Counts source: the RD_S.json backend (rakningstillfalle "slutlig", senasteUppdateringstid
"19 september 2026 14:53:03"), fields antalRoster/andelRoster per party; identical in the decision's
Bilaga 1 ("Hela landet: 349 mandat") and in the final-count workbook's "Antal_valkrets" sheet summed
over the 29 valkretsar. The "exact share" column is DERIVED (count ÷ 6767429 × 100, six decimals).

## SWEDEN — Riksdag election, 2026-09-13
### Source register
- returns (national): https://resultat.val.se/data/resultat/val2026/RD_S.json (Valmyndigheten — official data backend of the result presentation https://resultat.val.se/val2026/RD?r=S, accessed 2026-09-23, basis: "rakningstillfalle": "slutlig" [final count], last updated 19 September 2026 14:53:03; saved as `raw/resultat_val2026_RD_S.json`)
- returns (per constituency): https://resultat.val.se/data/resultat/val2026/RD_{NN}_S.json for NN = 01..29 (same backend, all "slutlig", accessed 2026-09-23; code 30 returns 404; saved as `raw/resultat_val2026_RD_{NN}_S.json`) → `valkrets_votes_2026.csv`
- returns (formal decision): https://www.val.se/download/18.7faaad3f1a0b0c300e4282/1789822515986/beslutsprotokoll-resultat-riksdagen-2026.pdf (Valmyndigheten, Beslut 2026-09-19, Dnr VAL-735-2026, with Bilaga 1–9: 1 Röster och mandat för partierna; 2 Valda ledamöter och ersättare; 3 Fördelning av fasta mandat per valkrets; 4 Fördelning av samtliga mandat (totalfördelning); 5 Återföring och tilldelning av återförda fasta mandat; 6 Fördelning av utjämningsmandat; 7 Kandidater som klarat spärren för inval på personliga röstetal; 8 Ordning av namn för inval av ledamöter; 9 Dubbelvalsavveckling; downloaded 2026-09-23, text-extracted with pdftotext -raw; saved as `raw/beslutsprotokoll-resultat-riksdagen-2026.pdf`) → the seat split and `valkrets_seats_2026.csv`
- returns (the same protocol as linked from the JSON's "lankTillProtokoll"): https://resultat.val.se/protokoll/protokoll_Val_2026_00_RD.pdf (downloaded 2026-09-23; different bytes from the val.se copy, identical extracted text — both pdftotext -layout and -raw outputs compare equal; saved as `raw/protokoll_Val_2026_00_RD.pdf`)
- returns (final count, district/kommun/valkrets/län): https://www.val.se/download/18.7faaad3f1a0b0c300e458a/1790069872234/roster-per-distrikt-slutligt-antal-roster-inklusive-totalt-valdeltagande-riksdagsvalet-2026.xlsx (Valmyndigheten, "Röster i val till riksdagen 2026 … enligt länsstyrelsernas slutliga sammanräkning", sheets Antal_valkrets and Valdeltagande_valkrets used as the third cross-check; saved as `raw/…xlsx`)
- returns (summary pages): https://www.val.se/valresultat-och-statistik/riksdags--region--och-kommunval/valresultat-2026 ("Publicerad: 21 september 2026"; "Slutligt resultat i riksdagsvalet 2026"), https://www.val.se/english/election-results/elections-to-the-riksdag-and-regional-and-municipal-councils/election-results-2026 ("Published: 19 September 2026"), https://www.val.se/valresultat-och-statistik (entry page), https://www.val.se/valresultat-och-statistik/statistik-och-data/radata-val-2026 ("Rådata och statistik val 2026", "Publicerad: 22 september 2026" — the index the xlsx links came from); all accessed 2026-09-23, saved as `raw/val_*.html`
- rules (fixed-seat apportionment): https://www.val.se/download/18.4005a7d19dee20a8ea531/1789570272432/fasta-valkretsmandat-val-2026.xlsx (Valmyndigheten 2026-05-05, "Antal fasta valkretsmandat, val till riksdag, region- och kommunfullmäktige 2026"), https://www.val.se/download/18.3eba56b819e04244a687ae/1789570324431/fordelning-av-mandat-2022-och-2026.xlsx (Valmyndigheten 2026-05-11, fixed seats 2022 vs 2026), https://www.val.se/download/18.4005a7d19dee20a8ea544/1778074856144/valkretsmandat-riksdag-1988-2026.xlsx (Valmyndigheten 2026-05-05, fixed seats 1988–2026); accessed 2026-09-23, saved under `raw/`
- rules (method): the decision PDF's own appendix texts (quoted under "Electoral rules" below)

### National result
| party (full name, abbrev) | vote share % | seats |
|---|---|---|
| Arbetarepartiet-Socialdemokraterna (S) | 28.02 | 99 |
| Moderaterna (M) | 19.85 | 70 |
| Sverigedemokraterna (SD) | 17.48 | 62 |
| Vänsterpartiet (V) | 8.40 | 30 |
| Centerpartiet (C) | 7.03 | 25 |
| Kristdemokraterna (KD) | 6.17 | 22 |
| Miljöpartiet de gröna (MP) | 6.12 | 22 |
| Liberalerna (tidigare Folkpartiet) (L) | 5.34 | 19 |
| Örebropartiet (ÖrP) — largest non-parliament party | 0.59 (39,644 votes) | 0 |

Turnout: 84.89% (basis: 6,834,413 votes cast of 8,051,355 eligible voters = 0.8488525, per RD_S.json "valdeltagande" and decision Bilaga 1 "Valdeltagande 84,89%"; cast votes include invalid/blank ballots). val.se's summary pages round it to "84,9 procent (eller 6 834 413 personer)".
Total seats: 349 (310 fixed constituency seats + 39 adjustment seats; decision Bilaga 4 and 6).

### 4 % threshold outcome
All eight parties passed the national 4 % threshold (lowest: L 5.34 %, 361,187 votes) — the same eight
parties as 2022. No other party passed it nationally (largest: Örebropartiet 0.59 %) and none reached 12 %
in any valkrets (largest non-parliament share in any valkrets: Örebropartiet 4.46 % in Örebro län,
9,051 of 203,013 — DERIVED from the per-valkrets JSONs), so no seat went to a party outside the eight.
Runners-up among non-parliament parties (RD_S.json): Alternativ för Sverige 26,729 (0.39 %), Kristna
Värdepartiet 9,017 (0.13 %), Ambition Sverige 7,679 (0.11 %), Piratpartiet 7,464 (0.11 %),
Medborgerlig Samling 4,118 (0.06 %).

### Regional table
Source: the 29 per-valkrets JSONs (= decision Bilaga 1 per-valkrets pages = workbook sheet Antal_valkrets).
Shares are DERIVED (count ÷ valid × 100, half-up to 0.01) — and all 232 share cells equal the shares the
decision's Bilaga 1 prints for each valkrets (0 differences). Seats: fixed = Bilaga 3's valkrets header;
adj = adjustment seats placed there by Bilaga 6; total = Bilaga 1's "N mandat". Per-party seats per valkrets:
`valkrets_seats_2026.csv`. Counts: `valkrets_votes_2026.csv`. All 29 constituencies, all eight parties.

| valkrets | valid | S % | M % | SD % | V % | C % | KD % | MP % | L % | fixed | adj | total seats |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Blekinge län | 104309 | 28.66 | 18.30 | 24.41 | 5.86 | 5.44 | 7.29 | 3.82 | 4.86 | 5 | 0 | 5 |
| Dalarnas län | 187850 | 29.52 | 18.35 | 22.29 | 5.76 | 6.49 | 7.08 | 4.74 | 4.03 | 9 | 2 | 11 |
| Gotlands län | 41526 | 31.11 | 18.30 | 13.49 | 6.74 | 10.73 | 5.24 | 7.88 | 5.28 | 2 | 0 | 2 |
| Gävleborgs län | 184537 | 31.82 | 17.73 | 21.53 | 6.76 | 6.06 | 6.18 | 4.22 | 4.03 | 9 | 1 | 10 |
| Göteborgs kommun | 380417 | 27.14 | 18.18 | 11.83 | 15.12 | 6.51 | 4.85 | 9.05 | 5.94 | 18 | 0 | 18 |
| Hallands län | 230235 | 25.58 | 23.33 | 19.57 | 5.25 | 7.54 | 7.05 | 4.40 | 5.95 | 10 | 3 | 13 |
| Jämtlands län | 86814 | 31.50 | 17.45 | 17.59 | 6.02 | 10.07 | 5.46 | 6.64 | 3.54 | 4 | 0 | 4 |
| Jönköpings län | 236678 | 26.58 | 19.75 | 20.58 | 5.96 | 7.00 | 10.24 | 3.82 | 4.59 | 11 | 2 | 13 |
| Kalmar län | 161796 | 29.03 | 18.74 | 21.36 | 6.14 | 6.41 | 8.39 | 4.13 | 4.26 | 7 | 1 | 8 |
| Kronobergs län | 128547 | 27.67 | 19.96 | 20.34 | 7.62 | 6.23 | 7.98 | 4.45 | 4.38 | 6 | 0 | 6 |
| Malmö kommun | 212965 | 28.98 | 16.47 | 13.20 | 17.17 | 5.86 | 3.25 | 9.28 | 4.61 | 10 | 1 | 11 |
| Norrbottens län | 160359 | 37.52 | 16.37 | 18.11 | 6.86 | 5.14 | 6.45 | 4.55 | 3.35 | 8 | 0 | 8 |
| Skåne läns norra och östra | 206399 | 23.24 | 20.85 | 27.28 | 5.95 | 5.31 | 6.86 | 3.65 | 5.50 | 10 | 2 | 12 |
| Skåne läns södra | 271168 | 22.77 | 23.25 | 19.78 | 6.03 | 7.97 | 5.34 | 7.24 | 6.51 | 12 | 1 | 13 |
| Skåne läns västra | 200979 | 26.05 | 20.13 | 24.06 | 7.82 | 5.32 | 5.24 | 4.38 | 5.72 | 9 | 3 | 12 |
| Stockholms kommun | 652543 | 26.34 | 19.10 | 8.35 | 13.04 | 9.79 | 3.90 | 11.14 | 7.10 | 29 | 5 | 34 |
| Stockholms län | 887078 | 25.83 | 23.80 | 14.92 | 8.10 | 7.81 | 5.71 | 5.71 | 6.63 | 41 | 2 | 43 |
| Södermanlands län | 190962 | 30.54 | 19.57 | 20.18 | 7.33 | 5.58 | 5.75 | 4.85 | 4.53 | 9 | 2 | 11 |
| Uppsala län | 265992 | 27.07 | 18.36 | 15.61 | 9.69 | 7.73 | 6.64 | 8.35 | 4.98 | 12 | 1 | 13 |
| Värmlands län | 184781 | 31.06 | 20.10 | 19.71 | 5.81 | 6.05 | 6.86 | 4.36 | 4.44 | 9 | 1 | 10 |
| Västerbottens län | 181055 | 36.94 | 15.39 | 12.75 | 9.34 | 7.19 | 5.91 | 7.49 | 3.50 | 8 | 1 | 9 |
| Västernorrlands län | 158985 | 34.38 | 16.24 | 19.17 | 6.34 | 6.91 | 7.02 | 4.60 | 3.69 | 7 | 1 | 8 |
| Västmanlands län | 176628 | 30.47 | 19.73 | 20.38 | 7.98 | 5.30 | 5.60 | 3.74 | 5.03 | 8 | 1 | 9 |
| Västra Götalands läns norra | 176511 | 28.57 | 19.06 | 21.59 | 7.10 | 5.79 | 6.94 | 4.51 | 4.92 | 8 | 2 | 10 |
| Västra Götalands läns södra | 146081 | 27.31 | 19.55 | 20.74 | 7.10 | 6.70 | 7.87 | 4.31 | 4.66 | 7 | 0 | 7 |
| Västra Götalands läns västra | 258972 | 25.30 | 21.50 | 18.20 | 6.34 | 7.37 | 7.22 | 6.30 | 6.32 | 11 | 2 | 13 |
| Västra Götalands läns östra | 179042 | 28.07 | 19.95 | 21.42 | 5.80 | 6.51 | 8.02 | 4.07 | 4.43 | 8 | 1 | 9 |
| Örebro län | 203013 | 30.35 | 17.63 | 17.76 | 7.82 | 5.72 | 6.20 | 4.88 | 4.43 | 9 | 3 | 12 |
| Östergötlands län | 311207 | 28.36 | 20.55 | 18.48 | 7.06 | 6.82 | 6.60 | 5.63 | 4.92 | 14 | 1 | 15 |

Fixed seats per valkrets changed from 2022 to 2026 in four valkretsar (fordelning-av-mandat-2022-och-2026.xlsx,
"Diff fasta mandat"): Stockholms län 40 → 41, Göteborgs kommun 17 → 18, Kalmar län 8 → 7,
Västernorrlands län 8 → 7 (total unchanged at 310). Anything that hard-codes 2022's fixed apportionment
must repoint.

### Electoral rules (quoted from the fetched 2026 decision and the apportionment file)
- Threshold (Bilaga 1): "Mandaten fördelas mellan partier som deltar i valet och som fått minst 4 procent av rösterna i hela landet eller minst 12 procent av rösterna i en valkrets."
- Tiers (Bilaga 6): "310 mandat är fasta valkretsmandat. Återstående 39 är utjämningsmandat." (Bilaga 4: "Antal fasta mandat 310 / Antal utjämningsmandat 39 / Totalt antal mandat 349".)
- Divisor method (Bilaga 3): "De fasta mandaten har fördelats enligt den jämkade uddatalsmetoden. Vid beräkningarna har partiernas röstetal delats (se Bilaga 1) först med 1,2 och sedan med 3, med 5, med 7 osv. i tur och ordning allt eftersom partierna har tagit sina mandat." — e.g. Bilaga 4's first quotient, S 1 579 990,83 = 1,895,989 / 1.2.
- National entitlement (Bilaga 4): "I totalfördelningen har samtliga mandat (både de fasta valkretsmandaten och utjämningsmandaten) fördelats mellan partierna. Den jämkade uddatalsmetoden har tillämpats (se Bilaga 3) på hela landet som en valkrets."
- Returned seats (Bilaga 5): "Om ett parti vid fördelningen av de fasta mandaten har fått fler mandat än vad som motsvarar en proportionell representation i hela landet (totalfördelning), ska överskjutande mandat återföras." — **2026: none returned** (every party's fixed seats ≤ its entitlement: S 94 ≤ 99, M 68 ≤ 70, SD 60 ≤ 62, V 27 ≤ 30, C 17 ≤ 25, KD 20 ≤ 22, MP 13 ≤ 22, L 11 ≤ 19).
- Adjustment seats (Bilaga 6): "Varje parti ska tilldelas så många utjämningsmandat som behövs för att partiet ska få en representation som svarar mot dess andel av samtliga giltiga röster i landet." … "Ett parti tilldelas utjämningsmandat i den eller de valkretsar där partiet efter fördelning av fasta mandat har störst jämförelsetal."
- Fixed-seat apportionment (fasta-valkretsmandat-val-2026.xlsx, Info): "Varje valkrets får ett mandat för varje gång som antalet röstberättigade i kretsen är jämnt delbart med en trehundrationdel av antalet röstberättigade i hela landet (Omgång 1 i uträkningen). De mandat som inte fördelas på detta sätt tillförs valkretsarna i tur och ordning efter den rest som uppstått vid fördelningen (Omgång 2)." Basis: eligible voters on 2 March 2026 (the file: 1 March 2026 was a Sunday).
- Personal votes (Bilaga 7): "Spärren för inval på personligt röstetal är 5 % av partiets röster." (Bilaga 8: candidates are ordered "i första hand … efter storleken på personliga röstetal och i andra hand jämförelsetal som beräknats enligt heltalsmetoden".)
- Legal basis (decision p. 1): "3 kap. regeringsformen (1974:152) och 14 kap. vallagen (2005:837)."

### Verification (run 2026-09-23, all passed)
- Seats: 349 = 310 fixed + 39 adjustment. Per party, fixed (Bilaga 3 count) + adjustment (Bilaga 6) = total (Bilaga 1 = RD_S.json) in every one of the 29 valkretsar × 8 parties; the valkrets seat sums equal the national seats per party; Bilaga 1's "Varav utj." column = Bilaga 6 cell by cell; Bilaga 3's 29 fixed-seat headers = fasta-valkretsmandat-val-2026.xlsx (and its sum is 310).
- Votes: for each party the sum over the 29 valkretsar equals the national count exactly (difference 0 for all eight); valid (6767429), cast (6834413), eligible (8051355) and invalid (66984) sum exactly too. **No votes are held outside the valkretsar:** national minus the valkrets sum is 0 for every party and every total.
- "Övriga anmälda partier": the per-valkrets JSON buckets sum to 98,148 against the national 107,199 — the 9,051 gap is Örebropartiet in Örebro län, which that valkrets file shows as its own row (visa = 0) instead of inside the bucket; the decision's Örebro page folds it back (1,533 + 9,051 = 10,584, as printed). With that fold the bucket sums exactly. Örebropartiet's own count summed over the 29 files = 39,644 = its national figure.
- Per valkrets: the eight party counts + Övriga = valid; valid + invalid = cast; ej anmälda + blanka + övriga ogiltiga = invalid — all 29.
- Three sources: JSON vs decision Bilaga 1 (every party count, valid, invalid and its three parts, cast, eligible, seats, adjustment seats — national and all 29 valkretsar): 0 differences. JSON vs workbook (Antal_valkrets: party counts, valid as the sum of all 124 party columns; Valdeltagande_valkrets: cast, eligible): 0 differences.
- Continuity with 2022: each 2026 file's 2022 comparison fields (antalRosterForegaendeVal, antalRostberattigadeForegaendeVal, antalMandatForegaendeVal) equal `../returns_2022.md` and `../valkrets_votes_2022.csv` exactly — same 29 valkretsar, same names.
- Raw files: all 40 re-hashed with `sha256sum -c raw/SHA256SUMS.results.txt` — 40 OK.

### Caveats
- **Personal-vote share discrepancy on val.se's own pages:** the English summary page says "The share of personal votes was 22.2 per cent"; the Swedish summary page says "andel väljare som personröstade 24,2 procent", and the decision's Bilaga 1 prints 1 637 668 / 24,20 % (1,637,668 ÷ 6,767,429 = 24.20 %). The English page's 22.2 is taken as its error; the decision governs.
- **Dnr printed two ways:** page 1 of the decision reads "VAL- 735-2026", page 2 "VAL-735". Recorded here as VAL-735-2026.
- **Two copies of the protocol:** the val.se download and the resultat.val.se "lankTillProtokoll" PDF differ in bytes (379,117 vs 373,611) but their extracted text is identical; both are kept.
- **The national JSON's `valkretsar` array is a stub** (29 entries with zero counts and empty codes); every per-valkrets figure comes from the 29 per-valkrets files, never from that array.
- **Party name spelling:** the 2026 backend and decision write "Liberalerna (tidigare Folkpartiet)"; `../returns_2022.md` records "Liberalerna, tidigare Folkpartiet". Consumers should key on the abbreviation (partiforkortning: S M SD C V KD L MP), which is the same in both years' files.
- **Per-file update times:** the 29 valkrets files were last updated between 17 September 2026 12:06:24 and 19 September 2026 11:27:07 (the länsstyrelser's final counts); the national file 19 September 2026 14:53:03 — the day the result was fixed.
- Early voting (Swedish summary page, not used in any table): "andel väljare som förtidsröstade i Sverige 54,7 procent (eller 3 736 703 personer)".
- The formation-relevant facts (who governs) are not in these documents; the decision covers seats and elected members only.

### Raw files (SHA-256; `raw/SHA256SUMS.results.txt` is the `sha256sum -c` form)
| file (raw/) | bytes | SHA-256 | fetched from (2026-09-23) |
|---|---|---|---|
| `beslutsprotokoll-resultat-riksdagen-2026.pdf` | 379117 | `a7fde8050b27036b964a9fe2044fdb4f05fb1fd0b63c312bc4f86323e04ff62e` | https://www.val.se/download/18.7faaad3f1a0b0c300e4282/1789822515986/beslutsprotokoll-resultat-riksdagen-2026.pdf |
| `fasta-valkretsmandat-val-2026.xlsx` | 36880 | `8f58ebcfb7d13248bd5c7c499d89dd800ef4d3c9791de4d64b62f3c62df64027` | https://www.val.se/download/18.4005a7d19dee20a8ea531/1789570272432/fasta-valkretsmandat-val-2026.xlsx |
| `fordelning-av-mandat-2022-och-2026.xlsx` | 35065 | `559a379d64ef09cb7459f8248cfb86934bf641a74a05ebe63d66202afb86828f` | https://www.val.se/download/18.3eba56b819e04244a687ae/1789570324431/fordelning-av-mandat-2022-och-2026.xlsx |
| `protokoll_Val_2026_00_RD.pdf` | 373611 | `4214de8c223ae4107b5fbe63c0982d7f75590f38ad5e5c068976f493ddc7ca44` | https://resultat.val.se/protokoll/protokoll_Val_2026_00_RD.pdf |
| `resultat_val2026_RD_01_S.json` | 378487 | `1afa59f834e187f0dc23a7f9bd679ff1d8415c74c958bd9f0f8ff6892318f83d` | https://resultat.val.se/data/resultat/val2026/RD_01_S.json |
| `resultat_val2026_RD_02_S.json` | 775522 | `8d88a7fef0e37a8278d43b7b8cac043e16ea0b7ad2d38672b410c4efa367a183` | https://resultat.val.se/data/resultat/val2026/RD_02_S.json |
| `resultat_val2026_RD_03_S.json` | 294994 | `875454ce3be52c9738b641c7b7346b4f207933b1a5a589989268c36f955dc674` | https://resultat.val.se/data/resultat/val2026/RD_03_S.json |
| `resultat_val2026_RD_04_S.json` | 248741 | `a4829b432deeb05c5fdfaed90b612d393af6c9dcb7d68f28653a2f8a7cb8f56b` | https://resultat.val.se/data/resultat/val2026/RD_04_S.json |
| `resultat_val2026_RD_05_S.json` | 343205 | `c11c155b41a9d2f20bdefaf30cd208b8989cce92f4b6840635668526088f0290` | https://resultat.val.se/data/resultat/val2026/RD_05_S.json |
| `resultat_val2026_RD_06_S.json` | 345697 | `02808777a102d6f2ce607cac5e9a7f6e2ad0652729b329c9dac90fe99d4c5b89` | https://resultat.val.se/data/resultat/val2026/RD_06_S.json |
| `resultat_val2026_RD_07_S.json` | 222202 | `b3a323e67e7b1d20c62d059dbf4f0c3101737e7094160685925332253024d985` | https://resultat.val.se/data/resultat/val2026/RD_07_S.json |
| `resultat_val2026_RD_08_S.json` | 292872 | `b7aeae8e2f132340ddef7f4a20a2470ba16cc1d4476ba3a382b6bf26df554bf2` | https://resultat.val.se/data/resultat/val2026/RD_08_S.json |
| `resultat_val2026_RD_09_S.json` | 271535 | `82610c37d70731a80a640a42c710ea114943d3284c3db986cf332d02b613aba8` | https://resultat.val.se/data/resultat/val2026/RD_09_S.json |
| `resultat_val2026_RD_10_S.json` | 162397 | `156d50c658a6daad1b70cc9447421d49c35510130649f192d84a6a7b903d67c1` | https://resultat.val.se/data/resultat/val2026/RD_10_S.json |
| `resultat_val2026_RD_11_S.json` | 161323 | `6802c751275f24a56e1c32c036f2e5ad2f2fa065fa0feec0812a27ff28a57f19` | https://resultat.val.se/data/resultat/val2026/RD_11_S.json |
| `resultat_val2026_RD_12_S.json` | 240991 | `9d73c6fb971c1a1440b1fa910c842e67ccbc96bb9c702b28ffe3ba35d6debd57` | https://resultat.val.se/data/resultat/val2026/RD_12_S.json |
| `resultat_val2026_RD_13_S.json` | 327910 | `fc91055b9edfc2792a5266b8d8d173497153d3e057041c69b73a440738a39159` | https://resultat.val.se/data/resultat/val2026/RD_13_S.json |
| `resultat_val2026_RD_14_S.json` | 307007 | `677b7c745e205c02d79401d5178cada061007111a20bf99f5c0f641f6d539d54` | https://resultat.val.se/data/resultat/val2026/RD_14_S.json |
| `resultat_val2026_RD_15_S.json` | 237937 | `aeda293f8962b2512d93c48de2dd33e1712b31a11e7664b60bbc799560a6beae` | https://resultat.val.se/data/resultat/val2026/RD_15_S.json |
| `resultat_val2026_RD_16_S.json` | 2269420 | `27a1281a6367a2e1ff120d3e8d870425254e138921f6b9db11cef89f64a172e3` | https://resultat.val.se/data/resultat/val2026/RD_16_S.json |
| `resultat_val2026_RD_17_S.json` | 327543 | `83a378f71de617f6f2b81d1fa3bf60b95370c95cc5c82455e33f12387fc2514c` | https://resultat.val.se/data/resultat/val2026/RD_17_S.json |
| `resultat_val2026_RD_18_S.json` | 318358 | `4264e07853a5ab2839ccd650d10134c8b606b03f952ce80d772547a09065ad40` | https://resultat.val.se/data/resultat/val2026/RD_18_S.json |
| `resultat_val2026_RD_19_S.json` | 223990 | `026135f17a283786516f7fa71f912d4fb3dfcfad7012d5ca5db3708885054993` | https://resultat.val.se/data/resultat/val2026/RD_19_S.json |
| `resultat_val2026_RD_20_S.json` | 331794 | `afcf00355f8e6ea515f1e17e29505a775dbf79eba1d5d6e71d62db67cd912a06` | https://resultat.val.se/data/resultat/val2026/RD_20_S.json |
| `resultat_val2026_RD_21_S.json` | 343225 | `2e1f2a7d29359481f80dfbdf55157a3cab9bf7fac822108e37ef4372b326b665` | https://resultat.val.se/data/resultat/val2026/RD_21_S.json |
| `resultat_val2026_RD_22_S.json` | 313569 | `b123fe39f770e8a6b07e3d4fa81fec2b1abb271e0767369d44169ca0097bb461` | https://resultat.val.se/data/resultat/val2026/RD_22_S.json |
| `resultat_val2026_RD_23_S.json` | 249946 | `4354eeed29686e57802465c1d823c298d7c6e7939269f06c9203d312069db126` | https://resultat.val.se/data/resultat/val2026/RD_23_S.json |
| `resultat_val2026_RD_24_S.json` | 322133 | `1bc9032b772259d1da4366fb9ec900f6180bc2587d2bd876b2208bfd02527d63` | https://resultat.val.se/data/resultat/val2026/RD_24_S.json |
| `resultat_val2026_RD_25_S.json` | 281452 | `33d4b9a5ad51131c1a24cebd05126f799c538d8ee2b0155724ef39598937c0a0` | https://resultat.val.se/data/resultat/val2026/RD_25_S.json |
| `resultat_val2026_RD_26_S.json` | 202724 | `c4c2c87c09ab94041a4f93388b72b89298d196661c084d185b9deb8edeb19156` | https://resultat.val.se/data/resultat/val2026/RD_26_S.json |
| `resultat_val2026_RD_27_S.json` | 184818 | `dff18e59d9075866678e00a6959113d86bfae7b22e8dd5c553802b5f1054b2c2` | https://resultat.val.se/data/resultat/val2026/RD_27_S.json |
| `resultat_val2026_RD_28_S.json` | 321210 | `d399f24adfe7651b562ede947f18b7a95edb22e68b2eb2c6daa2a08bc8a8be60` | https://resultat.val.se/data/resultat/val2026/RD_28_S.json |
| `resultat_val2026_RD_29_S.json` | 252235 | `9bd66f04fa7f7c6f3363f5a965c49744bffe22c8b7b3702c550907ce614f5e22` | https://resultat.val.se/data/resultat/val2026/RD_29_S.json |
| `resultat_val2026_RD_S.json` | 789535 | `1998afdc723a83b325c729faf68063c8731e33496fc274bf9fbb80499e0afb08` | https://resultat.val.se/data/resultat/val2026/RD_S.json |
| `roster-per-distrikt-slutligt-antal-roster-inklusive-totalt-valdeltagande-riksdagsvalet-2026.xlsx` | 12447414 | `6e170f9ae4622659f0fff39a2d5b086dfd9163fd3fe9b914f798ff72a5500231` | https://www.val.se/download/18.7faaad3f1a0b0c300e458a/1790069872234/roster-per-distrikt-slutligt-antal-roster-inklusive-totalt-valdeltagande-riksdagsvalet-2026.xlsx |
| `val_english_election-results-2026.html` | 143275 | `b7fa0fa319c09c4ef5414e98b8e234b5bd3306b516cabcac26f12593d2cfb4d6` | https://www.val.se/english/election-results/elections-to-the-riksdag-and-regional-and-municipal-councils/election-results-2026 |
| `val_radata-val-2026.html` | 256989 | `a231fc22212beca455f46a28ef8301a59f8a1ead63c4e86166b93f69916497ca` | https://www.val.se/valresultat-och-statistik/statistik-och-data/radata-val-2026 |
| `val_valresultat-2026.html` | 220383 | `26d7073273c6ef68d84d4995c26542f3be7919dac7ca93a3a504a9dc0448714c` | https://www.val.se/valresultat-och-statistik/riksdags--region--och-kommunval/valresultat-2026 |
| `val_valresultat-och-statistik.html` | 216435 | `6a363cfd6065fbdf1b6585d2e0e971ce26c485deb60aeb08a4f41d13cc1f68db` | https://www.val.se/valresultat-och-statistik |
| `valkretsmandat-riksdag-1988-2026.xlsx` | 21065 | `74d02c44f2c30722c995eee196cdb55d7dbf2371c0e4d3bec64cb44786aa5576` | https://www.val.se/download/18.4005a7d19dee20a8ea544/1778074856144/valkretsmandat-riksdag-1988-2026.xlsx |

*(Filed 2026-09-23 by the K-1 sourcing agent. Nothing outside `ElectionsData/sweden/2026/` was touched;
the `raw/declarations/` folder belongs to a separate sourcing pass and is not covered by these hashes.)*
