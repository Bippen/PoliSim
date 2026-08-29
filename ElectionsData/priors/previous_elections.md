# Previous elections (T−1) — the loyalty and prior-vote anchor [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; 2026-08-29, research agent; official sources per country).
`[PROVISIONAL]` until re-verified (R-K9).

**What these are for.** Two things, and the distinction matters: (1) the **prior vote** §8 damps
toward when predicting the next election; (2) with the T−2 results, the **volatility** W-A1 derives
per-party loyalty from. **Loyalty is never derived from the election being predicted** — that
would read the answer off the answer sheet — so the derivation uses T−1 against T−2 only.

## GERMANY — Bundestag 2021-09-26 (Zweitstimmen)

Source: Die Bundeswahlleiterin — https://www.bundeswahlleiterin.de/bundestagswahlen/2021/ergebnisse/bund-99.html
(current official version, re-established 2024-03-01 after the Berlin partial re-run of 2024-02-11);
originally-declared version: https://www.bundeswahlleiterin.de/info/presse/mitteilungen/bundestagswahl-2021/52_21_endgueltiges-ergebnis.html

| party | share % | votes |
|---|---|---|
| SPD | 25.7 | 11,901,558 |
| CDU | 19.0 | 8,774,920 |
| GRÜNE | 14.7 | 6,814,408 |
| FDP | 11.4 | 5,291,013 |
| AfD | 10.4 | 4,809,233 |
| CSU | 5.2 | 2,402,827 |
| DIE LINKE | 4.9 | 2,255,864 |
| SSW | 0.1 | 55,578 |

Turnout 76.4 %; valid Zweitstimmen 46,298,387.
⚠ **Two official versions exist.** The post-re-run figures above differ slightly from those
originally declared (CDU 18.9, GRÜNE 14.8, FDP 11.5, AfD 10.3, turnout 76.6). **Pick one basis and
record it** — this file uses the current official version throughout, and the absolute counts pair
with it. **BSW has no 2021 existence** (founded 2024 as a Linke split): its prior is zero, which is
the model's correct statement of newness, not a fallback.

## SWEDEN — Riksdag 2018-09-09

Source: Valmyndigheten — https://historik.val.se/val/val2018/slutresultat/R/rike/index.html
(slutligt valresultat after kontrollräkning).

| party | share % | votes |
|---|---|---|
| S | 28.26 | 1,830,386 |
| M | 19.84 | 1,284,698 |
| SD | 17.53 | 1,135,627 |
| C | 8.61 | 557,500 |
| V | 8.00 | 518,454 |
| KD | 6.32 | 409,478 |
| L | 5.49 | 355,546 |
| MP | 4.41 | 285,899 |
| FI | 0.46 | 29,665 |
| other registered parties | 1.07 | 69,472 |

Turnout 87.18 %; valid votes 6,476,725. **The rows sum to exactly 6,476,725** and every percentage
reproduces from votes ÷ valid votes — the cleanest arithmetic confirmation of the four countries.
**Sweden is the one clean party-for-party join:** all eight 2022 parties contested 2018 as the same
entities, no coalition lists, no splits, no renames.

## POLAND — Sejm 2019-10-13

Source: PKW — https://pkw.gov.pl/uploaded_files/1571084597_obwieszczenie_sejm.pdf
(*Obwieszczenie PKW z dnia 14 października 2019 r.*, the statutory announcement under art. 238).

| committee (as registered) | share % | votes |
|---|---|---|
| KW Prawo i Sprawiedliwość | 43.59 | 8,051,935 |
| KKW Koalicja Obywatelska PO .N iPL Zieloni | 27.40 | 5,060,355 |
| KW Sojusz Lewicy Demokratycznej (campaigned as "Lewica") | 12.56 | 2,319,946 |
| KW Polskie Stronnictwo Ludowe (campaigned as "PSL–Koalicja Polska") | 8.55 | 1,578,523 |
| KWW Konfederacja Wolność i Niepodległość | 6.81 | 1,256,953 |
| KWW Koalicja Bezpartyjni i Samorządowcy | 0.78 | 144,773 |
| KWW Mniejszość Niemiecka | 0.17 | 32,094 |

Turnout 61.74 % (valid ballot cards ÷ eligible — PKW's own basis); valid votes 18,470,710.
⚠ **Mapping bias, stated:** Trzecia Droga's 2019 counterpart is PSL–Koalicja Polska's 8.55 %, which
**understates** it — Polska 2050, TD's other half, did not exist in 2019. Using 8.55 as TD's prior
biases TD's error term downward by roughly the whole Polska 2050 component. NL's 12.56 carries
composition drift (SLD+Wiosna+Razem in 2019 → Nowa Lewica by 2023).

## ITALY — Camera 2018-03-04 (proportional part)

Source: Ministero dell'Interno, Eligendo Archivio —
https://elezionistorico.interno.gov.it/index.php?tpel=C&dtel=04/03/2018&tpa=I&tpe=A&lev0=0&levsut0=0&es0=S&ms=S
(area "ITALIA escl. Valle d'Aosta").

| party | share % | votes |
|---|---|---|
| MoVimento 5 Stelle | 32.68 | 10,732,066 |
| Partito Democratico | 18.76 | 6,161,896 |
| Lega | 17.35 | 5,698,687 |
| Forza Italia | 14.00 | 4,596,956 |
| Fratelli d'Italia | 4.35 | 1,429,550 |
| Liberi e Uguali | 3.39 | 1,114,799 |
| +Europa | 2.56 | 841,468 |

Turnout 72.94 %; valid list votes 32,841,025 (**derived by subtraction** from printed figures — the
page prints no valid-vote total; every party percentage reproduces against it to two decimals).
⚠ **Mapping problems, stated:** **Azione and Italia Viva have NO 2018 existence** (both are 2019
splits from the PD), so their prior is zero — and the PD's 18.76 correspondingly **overstates** the
2022 PD, since it contains the material two of its 2022 competitors were built from. **AVS's 3.39
(LeU) is a partial lineage only**: LeU held Sinistra Italiana but not the Greens (who sat inside
Italia Europa Insieme, 0.58 %), and LeU's Articolo 1 component later went toward the PD, not AVS.

## Cross-country note

**Turnout bases differ and are not interchangeable:** Germany and Sweden votes-cast ÷ eligible;
Poland *valid ballot cards* ÷ eligible; Italy votes-cast ÷ eligible on a reduced geography
(excludes Valle d'Aosta and the overseas constituency). Normalise before comparing.
**Only Sweden is a clean join.** In the other three the naive prior-vote join biases specific
parties in a direction that is named above — those biases are reported with every result that
uses them, never silently absorbed.

---

# T−2 elections — the volatility base (sourced 2026-08-29)

**Why a third election.** Loyalty (W-A1) is derived from how much a party's vote MOVED between
the two elections preceding the one being modelled. Which pair that is depends on the use:

| use | the pair | why |
|---|---|---|
| **PLAY** — predicting the next, unplayed election | the two MOST RECENT (e.g. Italy 2018→2022) | those are the two preceding it |
| **BACKTEST** — re-predicting 2022/2023/2025 | T−2→T−1 (e.g. Italy 2013→2018) | using the target's own change would read the answer off the answer sheet |

The same function serves both; only the inputs differ. **Any backtest figure derived from the
target election's own movement is circular and is never reported as validation.**

## SWEDEN 2014-09-14 — Valmyndigheten, https://historik.val.se/val/val2014/slutresultat/R/rike/index.html
S 31.01 · M 23.33 · SD 12.86 · MP 6.89 · C 6.11 · V 5.72 · FP 5.42 · KD 4.57 · FI 3.12 · other 0.97.
Turnout 85.81 %; valid votes 6,231,573 (party rows sum exactly).
**Mapping to 2018: 8 of 8 Riksdag parties CLEAN, one RENAMED** — Folkpartiet (FP) → Liberalerna (L),
same legal entity, renamed November 2015; join on entity, not on abbreviation.

## GERMANY 2017-09-24 — Die Bundeswahlleiterin, https://www.bundeswahlleiterin.de/bundestagswahlen/2017/ergebnisse/bund-99.html
CDU 26.8 · SPD 20.5 · AfD 12.6 · FDP 10.7 · DIE LINKE 9.2 · GRÜNE 8.9 · CSU 6.2. Zweitstimmen.
Turnout 76.2 %; valid Zweitstimmen 46,515,492.
**Mapping to 2021: all seven CLEAN.** ⚠ **SSW did NOT contest 2017** (its first federal candidacy
since 1961 was 2021) — so "no T−2 *candidacy*", not "no T−2 *organisation*"; it existed throughout
as a Schleswig-Holstein Landtag party. Shares are published to ONE decimal — recompute from counts
against 46,515,492 if two-decimal precision is needed to match the T−1 file.

## POLAND 2015-10-25 — PKW, https://parlament2015.pkw.gov.pl/349_Wyniki_Sejm.html
PiS 37.58 · PO 24.09 · Kukiz'15 8.81 · Nowoczesna 7.60 · Zjednoczona Lewica 7.55 · PSL 5.13 ·
KORWiN 4.76 · Razem 3.62 · Mniejszość Niemiecka 0.18. Turnout 50.92 %; valid votes 15,200,671.
⚠ **Zjednoczona Lewica took 0 seats** on 7.55 % — registered as a *coalition*, so it faced the 8 %
bar rather than 5 %: the first post-1989 Sejm with no left representation.
⚠ **Mapping to 2019 is mostly UNSAFE — only 2 of 9 clean** (PiS and Mniejszość Niemiecka). PO and
Nowoczesna merged into KO; Kukiz'15 went inside PSL's Koalicja Polska; Zjednoczona Lewica and Razem
went inside the SLD committee; KORWiN became one component of Konfederacja. PKW reports at
**committee** level and committees are re-formed every cycle by design.

## ITALY 2013-02-24/25 — Ministero dell'Interno, Eligendo Archivio (Camera, Area ITALIA)
M5S 25.56 · PD 25.43 · PdL 21.56 · Scelta Civica 8.30 · Lega Nord 4.09 · SEL 3.20 ·
Rivoluzione Civile 2.25 · FdI 1.96 · UDC 1.79. Turnout 75.20 %; valid votes 34,005,755.
⚠ **Mapping to 2018 is mostly UNSAFE — only 2 of 9 clean** (PD, M5S). PdL → Forza Italia is a
rename **plus a split** (the NCD faction left and ran separately in 2018); Lega Nord → Lega is a
rename; SEL dissolved into Sinistra Italiana which then ran inside LeU beside a PD splinter;
Scelta Civica and Rivoluzione Civile have **no 2018 successor** at all.

## ⚠ THE COVERAGE CONSTRAINT — the finding that governs how W-A1 may be used

Ranked by how far a name-joined volatility measure can be trusted across T−2 → T−1:

| country | entities safely joinable | share of the vote covered |
|---|---|---|
| **Sweden** | 9 of 9 | **~99 %** |
| **Germany** | 8 of 8 (SSW the one new entrant) | **~95 %** |
| Italy | 2 of 9 | ~53 % |
| Poland | 2 of 9 | ~38 % |

**In Poland and Italy a name-joined loyalty attributes organisational reshuffling to voter
defection.** The derivation is therefore reported with its coverage figure per country, and
Poland's and Italy's derived loyalties are marked LOW-CONFIDENCE wherever they appear. This is a
property of those party systems, not a defect in the formula — and it is exactly why the prototype
target (0.1) is **Sweden**, the country whose continuity is cleanest.
