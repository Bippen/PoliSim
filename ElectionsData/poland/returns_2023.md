# Poland — Sejm 2023 returns + rules [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; overnight 2026-08-28→29, research agent; primary sources fetched
directly — the PKW obwieszczenie via the official ELI/Dz.U. service, per-district CSVs from the
KBW's own data service). `[PROVISIONAL]` until a second session re-verifies (R-K9). National
table carries ABSOLUTE VOTE COUNTS (the Part 5 constraint honoured). District table is
percent-of-valid per okręg with magnitudes — the constituency-level spine the overhaul doc's
Part 5 demands for any Polish allocation.

## POLAND — Sejm election, 2023-10-15
### Source register
- returns: https://eli.gov.pl/api/acts/DU/2023/2234/text.html (Obwieszczenie Państwowej Komisji Wyborczej z dnia 17 października 2023 r. o wynikach wyborów do Sejmu, Dziennik Ustaw 2023 poz. 2234, official ELI/Dz.U. service; accessed 2026-08-28; full text fetched — national vote totals, shares, seats, turnout taken from its Dział I rozdz. 2–3)
- returns (record page): https://isap.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20230002234 (ISAP, Sejm; accessed 2026-08-28)
- returns (per-district shares): https://danewyborcze.kbw.gov.pl/dane/2023/sejmsenat/wyniki_gl_na_listy_po_okregach_proc_sejm_csv.zip (Krajowe Biuro Wyborcze official election-data service danewyborcze.kbw.gov.pl, "Parlament 2023" index at https://danewyborcze.kbw.gov.pl/indexc6e4.html?title=Parlament_2023; accessed 2026-08-28; file `wyniki_gl_na_listy_po_okregach_proc_sejm_utf8.csv`, % of valid votes per list per okręg; cross-checked against the obwieszczenie's district turnout figures — okr. 1: 71.45%, okr. 3: 78.06% match)
- returns (district magnitudes/seats of OKW): https://danewyborcze.kbw.gov.pl/dane/2023/sejmsenat/okregi_sejm_csv.zip (KBW; accessed 2026-08-28; file `okregi_sejm_utf8.csv`)
- returns (interactive site, JS-only shell — data not scrapeable directly): https://sejmsenat2023.pkw.gov.pl/sejmsenat2023/pl/sejm/wynik/pl (PKW)
- rules: https://eli.gov.pl/api/acts/DU/2011/112/text.html (Ustawa z dnia 5 stycznia 2011 r. — Kodeks wyborczy, Dz.U. 2011 nr 21 poz. 112, ELI full text; accessed 2026-08-28; ISAP record https://isap.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20110210112)

### National result
| committee/party (full name, abbrev) | vote share % | seats |
|---|---|---|
| Komitet Wyborczy Prawo i Sprawiedliwość (PiS) | 35.38 | 194 |
| Koalicyjny Komitet Wyborczy Koalicja Obywatelska PO .N iPL Zieloni (KO) | 30.70 | 157 |
| Koalicyjny Komitet Wyborczy Trzecia Droga Polska 2050 Szymona Hołowni – Polskie Stronnictwo Ludowe (TD) | 14.40 | 65 |
| Komitet Wyborczy Nowa Lewica (NL / Lewica) | 8.61 | 26 |
| Komitet Wyborczy Konfederacja Wolność i Niepodległość (Konf) | 7.16 | 18 |
| Komitet Wyborczy Wyborców Mniejszość Niemiecka (MN) — exempt from threshold, admitted to seat division, won 0 seats | 0.12 | 0 |
| Komitet Wyborczy Bezpartyjni Samorządowcy (BS) — largest committee without seats | 1.86 | 0 |

Votes (valid): PiS 7,640,854; KO 6,629,402; TD 3,110,670; NL 1,859,018; Konf 1,547,364; BS 401,054; MN 25,778. Valid votes total 21,596,674 = 98.31% of votes cast; invalid 370,217 (1.69%).
Turnout: 74.38% (basis: 21,966,891 valid ballot cards / 29,532,595 eligible voters — Dz.U. 2023 poz. 2234, Dział I rozdz. 2 pkt 8–9)
Total seats: 460

### Regional table
Source table: `wyniki_gl_na_listy_po_okregach_proc_sejm_utf8.csv` (KBW, URL above) — % of valid votes per okręg; district seats (siedziba OKW) and magnitudes from `okregi_sejm_utf8.csv`. Top four committees nationally: PiS, KO, TD, NL. All 41 districts:

| Okręg (siedziba OKW, magnitude) | PiS % | KO % | TD % | NL % |
|---|---|---|---|---|
| 1 Legnica (12) | 34.80 | 33.78 | 10.75 | 9.51 |
| 2 Wałbrzych (8) | 33.34 | 37.17 | 12.13 | 7.98 |
| 3 Wrocław (14) | 26.66 | 36.94 | 13.74 | 11.35 |
| 4 Bydgoszcz (12) | 30.45 | 35.01 | 15.06 | 9.92 |
| 5 Toruń (13) | 34.06 | 29.52 | 15.68 | 11.25 |
| 6 Lublin (15) | 45.48 | 20.32 | 15.87 | 5.72 |
| 7 Chełm (12) | 50.75 | 17.40 | 13.04 | 5.62 |
| 8 Zielona Góra (12) | 27.76 | 37.73 | 15.07 | 9.27 |
| 9 Łódź (10) | 26.82 | 41.07 | 11.89 | 12.22 |
| 10 Piotrków Trybunalski (9) | 46.60 | 21.69 | 13.73 | 6.39 |
| 11 Sieradz (12) | 41.46 | 25.89 | 14.50 | 7.73 |
| 12 Kraków [II] (8) | 42.86 | 24.24 | 14.97 | 6.04 |
| 13 Kraków [I] (14) | 30.68 | 30.73 | 16.86 | 11.04 |
| 14 Nowy Sącz (10) | 53.73 | 16.10 | 11.58 | 3.18 |
| 15 Tarnów (9) | 48.67 | 17.02 | 18.64 | 4.00 |
| 16 Płock (10) | 44.11 | 22.40 | 17.07 | 6.52 |
| 17 Radom (9) | 48.68 | 20.96 | 13.98 | 5.34 |
| 18 Siedlce (12) | 48.62 | 18.71 | 15.51 | 4.85 |
| 19 Warszawa I (20) | 20.14 | 43.23 | 13.25 | 13.45 |
| 20 Warszawa II (12) | 31.74 | 35.23 | 15.06 | 7.06 |
| 21 Opole (12) | 31.26 | 33.59 | 12.74 | 7.24 |
| 22 Krosno (11) | 54.70 | 15.85 | 13.79 | 4.47 |
| 23 Rzeszów (15) | 51.60 | 17.70 | 12.42 | 4.87 |
| 24 Białystok (14) | 42.39 | 20.84 | 18.86 | 4.84 |
| 25 Gdańsk (12) | 25.20 | 41.70 | 14.70 | 9.41 |
| 26 Słupsk/Gdynia (14) | 29.24 | 37.91 | 13.59 | 8.33 |
| 27 Bielsko-Biała (9) | 36.71 | 28.67 | 14.55 | 7.77 |
| 28 Częstochowa (7) | 36.35 | 29.11 | 14.72 | 9.41 |
| 29 Katowice/Gliwice (9) | 30.16 | 36.06 | 13.34 | 9.21 |
| 30 Bielsko-Biała/Rybnik (9) | 38.06 | 29.98 | 12.45 | 6.84 |
| 31 Katowice (12) | 30.88 | 36.79 | 13.27 | 8.46 |
| 32 Katowice/Sosnowiec (9) | 29.74 | 30.30 | 9.85 | 21.60 |
| 33 Kielce (16) | 47.07 | 20.93 | 13.80 | 6.83 |
| 34 Elbląg (8) | 35.20 | 31.87 | 15.40 | 8.11 |
| 35 Olsztyn (10) | 32.33 | 33.07 | 16.11 | 8.09 |
| 36 Kalisz (12) | 35.85 | 28.58 | 16.16 | 8.52 |
| 37 Konin (9) | 38.69 | 23.99 | 16.63 | 9.48 |
| 38 Piła (9) | 29.11 | 34.87 | 17.66 | 7.84 |
| 39 Poznań (10) | 19.57 | 44.09 | 16.54 | 12.31 |
| 40 Koszalin (8) | 31.36 | 38.69 | 12.35 | 8.72 |
| 41 Szczecin (12) | 28.79 | 40.13 | 12.62 | 9.39 |

(Bracketed I/II and second city names for duplicate OKW seats are conventional labels only; official identifier is the okręg number. PKW also publishes the same data at gmina/powiat/województwo/obwód level as CSV/XLSX from the same index, and per-district pages at sejmsenat2023.pkw.gov.pl/sejmsenat2023/pl/sejm/wynik/okr/N — JS-rendered.)

### Electoral rules
- Open-list PR: voter votes for one candidate list by marking "x" beside ONE candidate on that list, thereby indicating that candidate's priority to a seat (Kodeks wyborczy art. 227 §1); seats won by a list go to its candidates in order of individual votes received (art. 233 §1). Source: https://eli.gov.pl/api/acts/DU/2011/112/text.html
- d'Hondt per district, no national tier: in each okręg the district electoral commission divides each eligible list's valid votes successively by 1, 2, 3, 4… and awards the district's seats to the largest quotients (art. 232 §1); all 460 seats are allocated in the 41 districts — the Code provides no national compensatory tier. Source: https://eli.gov.pl/api/acts/DU/2011/112/text.html
- Thresholds: 5% of valid votes nationwide for party committees (art. 196 §1); 8% for coalition committees (art. 196 §2); committees of registered national-minority organisations may be exempted from the 5% condition on declaration to PKW by the 5th day before the election (art. 197 §1) — applied to MN in 2023 per the obwieszczenie (Dz.U. 2023 poz. 2234, Dział I rozdz. 3 pkt 2, "przy uwzględnieniu art. 197 § 1"). Source: https://eli.gov.pl/api/acts/DU/2011/112/text.html
- Districts: multi-member okręgi of at least 7 deputies each (art. 201 §2), boundaries respecting powiat lines (art. 201 §3); in 2023 there were 41 districts with magnitudes from 7 (okręg 28 Częstochowa) to 20 (okręg 19 Warszawa I). Sources: https://eli.gov.pl/api/acts/DU/2011/112/text.html (art. 201; magnitudes fixed in załącznik nr 1 to the Code) and https://danewyborcze.kbw.gov.pl/dane/2023/sejmsenat/okregi_sejm_csv.zip (41 rows, "Liczba mandatów" column, min 7 / max 20)
- Senate: elected simultaneously under a separate system — single-member districts (art. 260 §1), drawn on a uniform representation norm of national population divided by 100, i.e. 100 districts (art. 261 §1), the candidate with the most valid votes winning the seat (art. 273 §1). Source: https://eli.gov.pl/api/acts/DU/2011/112/text.html; official Senate returns: Dz.U. 2023 poz. 2235 (https://isap.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU20230002235, not fetched)

### Caveats
- The Electoral Code text fetched is the ORIGINAL Dz.U. 2011 nr 21 poz. 112 version via the ELI API; the 2023 election ran under the consolidated text (Dz.U. 2022 poz. 1277 i 2418; 2023 poz. 497, as cited in the obwieszczenie's preamble). The article numbers used here (196, 197, 201, 227, 232, 233, 260, 261, 273) are the same ones the PKW obwieszczenie itself cites (231 §2, 197 §1), so the numbering for these provisions is unchanged; wording amendments since 2011 cannot be excluded for uncited articles.
- Per-district percentage table comes from KBW's danewyborcze.kbw.gov.pl CSV dump (official National Electoral Office data service), not from the eli.gov.pl obwieszczenie text; the obwieszczenie carries per-district absolute votes and turnout, and two spot-checked turnout figures match the CSV exactly. The interactive PKW pages (sejmsenat2023.pkw.gov.pl) render via JavaScript and could not be scraped directly tonight.
- Okręg 19 (Warszawa I) also contains all abroad and ship votes ("zagranica; statki" in its official boundary description), which is why its registered-voter count exceeds its resident count.
- MN's exemption meant it took part in seat division but won 0 seats (Dz.U. 2023 poz. 2234, Dział I rozdz. 3 pkt 4(6)). PJJ (Polska Jest Jedna, 1.63%) and all other committees fell below threshold; BS (1.86%) is confirmed as the largest committee without seats from the obwieszczenie's national vote list.
- District "names": PKW identifies Sejm districts by number plus siedziba OKW; several share a seat city (two Kraków, two Warszawa, three Katowice, two Bielsko-Biała). "Warszawa I/II", "Kraków I/II" are conventional, not official, labels.
- Senate-side turnout and results were not fetched (out of scope beyond the one-sentence system description); the widely reported Senate turnout of 74.31% is [UNCONFIRMED] here (secondary: bankier.pl news report surfaced in search).

*(Filed verbatim from the research agent's return, 2026-08-28 night. For the backtest: the
district table is `[SHARES-ONLY]` at 0.01 % — per-district ABSOLUTE counts exist in the
obwieszczenie and the KBW absolute-votes CSV; billed in `../DATA_BILL.md`.)*
