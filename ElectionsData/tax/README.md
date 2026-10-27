# The real tax instruments — the five billed countries, sourced from their own authorities

Fetched 2026-09-02 for F4's sourcing bill (`POLISIM_TAX_SPECLET.md` §3). Every file here is the
authority's own page or document, unedited; every figure below is QUOTED from the file beside it, and
the file's digest is what `sha256sum ElectionsData/tax/*/*` re-derives. Nothing is transcribed from
memory; where a fetch failed the row says so and stays BILLED.

⚠ **The spec-let's most important sentence held on contact:** Germany's tariff is a formula in the
statute, France's barème is applied per *part* (the quotient familial), and the USA's is four filing
tables. "Brackets as data" fits Poland and (per table) the USA; it does not fit Germany or France.

## Germany — §32a EStG, the 2026 tariff (`germany/estg_32a.html`, gesetze-im-internet.de)

Verbatim (Absatz 1, Satz 2, "ab dem Veranlagungszeitraum 2026", zu versteuernde Einkommen):
1. *bis 12 348 Euro (Grundfreibetrag): 0;*
2. *von 12 349 Euro bis 17 799 Euro: (914,51 · y + 1 400) · y;*
3. *von 17 800 Euro bis 69 878 Euro: (173,10 · z + 2 397) · z + 1 034,87;*
4. *von 69 879 Euro bis 277 825 Euro: 0,42 · x – 11 135,63;*
5. *von 277 826 Euro an: 0,45 · x – 19 470,38.*

where *y* is one ten-thousandth of the income above the Grundfreibetrag, *z* one ten-thousandth of the
income above 17 799 €, *x* the income rounded down to a full euro (Sätze 3–5). Absatz 5: married couples
assessed jointly pay twice the tariff on half the joint income (the *Splitting*).

## France — barème 2026 on 2025 income (`france/economie_gouv_fr_tranches.html`, `france/service_public_F1419.html`)

Both official pages carry the same table, per *part* of the quotient familial:

| tranche (revenu par part) | taux |
|---|---|
| jusqu'à 11 600 € | 0 % |
| de 11 601 € à 29 579 € | 11 % |
| de 29 580 € à 84 577 € | 30 % |
| de 84 578 € à 181 917 € | 41 % |
| au-delà de 181 917 € | 45 % |

The economie.gouv.fr page's own words: *"Multipliez le résultat obtenu à l'étape 2 par le nombre de vos
parts fiscales"* — the quotient is the instrument, not a footnote to it.

**CSG/CRDS — discharged 2026-09-02 (`france/urssaf_taux_cotisations_secteur_prive.html`, urssaf.fr, the
2026 table).** Verbatim, the salarié column: *CSG imposable* **2,40 % sur 98,25 % du salaire brut dans la
limite de 192 240 € en 2026**; *CSG non imposable* **6,80 %** on the same base; *CRDS* **0,50 %** on the
same base. So 9,20 % CSG + 0,50 % CRDS on 98,25 % of gross up to four PASS, of which only the 2,40 % is
deductible from the income-tax base — the split is the instrument.

## USA — Rev. Proc. 2025-32, tax year 2026 (`usa/rp-25-32.pdf`, irs.gov)

The rate schedules are § 1(j)(2) Tables 1–4 in the PDF (page with "TABLE 1 - Section 1(j)(2)(A)");
quoted here for the two the model would read first, the rest are in the file:

*Table 3 — Unmarried Individuals:* not over $12,400: 10 %; over $12,400 to $50,400: $1,240 plus 12 %;
over $50,400 to $105,700: $5,800 plus 22 %; over $105,700 to $201,775: $17,966 plus 24 %; over
$201,775 to $256,225: $41,024 plus 32 %; the 35 % and 37 % rows follow in the file.
*Table 1 — Married Filing Jointly:* not over $24,800: 10 %; to $100,800: $2,480 plus 12 %; to $211,400:
$11,600 plus 22 %; to $403,550: $35,932 plus 24 %; to $512,450: $82,048 plus 32 %; to $768,700:
$116,896 plus 35 %; over $768,700: $206,583.50 plus 37 %.
*Standard deduction (§ 3.14):* married filing jointly **$32,200**; the other filing statuses follow in
the same section.

**Table 3 whole, and the single filer's deduction — read from the same file 2026-09-13 (F4-2, the table the model reads: the
incomes are per person).** Verbatim: *Over $256,225 but not over $640,600: $58,448 plus 35% of the excess over $256,225; Over
$640,600: $192,979.25 plus 37% of the excess over $640,600.* § 3.14 (1), the same table: *Unmarried Individuals (other than
Surviving Spouses and Heads of Households) (§ 1(j)(2)(C)) $16,100*. The model folds the deduction in as the exempt band: its
brackets in gross income are $16,100 + each of Table 3's thresholds.

**The OASDI wage base — discharged 2026-09-02 (`usa/fr-2025-19763_cola_2026.pdf`, the Social Security
Administration's notice *Cost-of-Living Increase and Other Determinations for 2026*, 90 FR, 3 November
2025, fetched from govinfo.gov via the Federal Register API — ssa.gov itself still 403s a batch fetch).**
Verbatim: *"The OASDI contribution and benefit base will be **$184,500** for remuneration paid in 2026 and
self-employment income earned in tax years beginning in 2026"*; the same notice gives the 2.8 percent
benefit increase and the "old-law" base of $137,100.

## Poland — skala podatkowa (`poland/podatki_gov_pl_stawki.html`, podatki.gov.pl)

Verbatim, *"Skala podatkowa od 2022 roku"*: podstawa obliczenia podatku do **120 000 zł**: **12 %
minus kwota zmniejszająca podatek 3 600 zł**; ponad 120 000 zł: **10 800 zł + 32 % nadwyżki ponad
120 000 zł**.

**The levy and the contributions — discharged 2026-09-02 from the Sejm's ELI API (api.sejm.gov.pl, the
consolidated texts, *tekst ujednolicony*; the originals it also serves are the 1991 and 1998 acts as
first passed and carry neither).**

- `poland/sejm_eli_DU_1991_350_ustawa_pit_ujednolicony.pdf` (Dz.U. 1991 nr 80 poz. 350, consolidated on
  t.j. Dz.U. 2026 poz. 592, 779, 846): **art. 27 ust. 1** is the scale above, verbatim in the act — *do
  120 000: 12 % minus kwota zmniejszająca podatek 3600 zł; ponad 120 000: 10 800 zł + 32 % nadwyżki
  ponad 120 000 zł* — and **art. 30h ust. 1–2**: *"Osoby fizyczne są obowiązane do zapłaty daniny
  solidarnościowej w wysokości **4 %** podstawy obliczenia tej daniny. Podstawę obliczenia daniny
  solidarnościowej stanowi nadwyżka ponad **1 000 000 zł** sumy dochodów …"*.
- `poland/sejm_eli_DU_1998_887_ustawa_sus_ujednolicony.pdf` (Dz.U. 1998 nr 137 poz. 887, consolidated on
  t.j. Dz.U. 2026 poz. 199 … 734): **art. 22 ust. 1**: *19,52 % podstawy wymiaru — na ubezpieczenie
  emerytalne; 8,00 % — na ubezpieczenia rentowe; 2,45 % — na ubezpieczenie chorobowe; od 0,40 % do
  8,12 % — wypadkowe*; **art. 16**: emerytalne financed *"w równych częściach, ubezpieczeni i płatnicy
  składek"* (9,76 % each), rentowe *"1,5 % podstawy wymiaru ubezpieczeni i 6,5 % płatnicy"*, chorobowe
  *"w całości … sami ubezpieczeni"*. So the employee side is 9,76 + 1,50 + 2,45 = **13,71 %** of the base.
  `poland/zus_wysokosc_skladek.html` (zus.pl) shows the same four rates in force and the 1,67 % accident
  rate for small payers.

## Italy — TUIR art. 11, FETCHED 2026-10-27 (`italy/`, normattiva.it; fetched under K187, E-6)

**What changed since the two failed attempts:** normattiva's article URN with the `!vig=` suffix (the
point-in-time view) serves the article's text in the HTML a batch fetch receives; the bare URN and the ELI
CONSOLIDATED endpoint still return the 903 KB shell without it. Fetched with curl, HTTP 200 each, saved
byte for byte (`italy/fetch_log.txt`, `italy/SHA256SUMS.txt`; `.gitattributes` keeps them `-text`).
Normattiva is the Istituto Poligrafico e Zecca dello Stato's consolidated text, not the Gazzetta itself.

**In force 1-1-2026 to 31-12-2026** (`italy/normattiva_tuir_art11_vig_2026-10-27.html`, the page's own
"Testo in vigore dal: 1-1-2026 al: 31-12-2026"), comma 1 verbatim:
*"L'imposta lorda è determinata applicando al reddito complessivo, al netto degli oneri deducibili indicati
nell'articolo 10, le seguenti aliquote per scaglioni di reddito: a) fino a 28.000 euro, 23 per cento;
b) oltre 28.000 euro e fino a 50.000 euro, ((33 per cento)) ; c) oltre 50.000 euro, 43 per cento."*
(the double parentheses are normattiva's mark of the latest amendment).

**In force 1-1-2025 to 31-12-2025** (`italy/normattiva_tuir_art11_vig_2025-06-30.html`, "Testo in vigore
dal: 1-1-2025 al: 31-12-2025"): *"a) ((fino a 28.000 euro, 23 per cento;)) b) ((oltre 28.000 euro e fino a
50.000 euro, 35 per cento;)) c) ((oltre 50.000 euro, 43 per cento))"*, under the same opening words.

**The amending acts** (`italy/normattiva_tuir_art11_aggiornamenti_v25.html`, the article's list of
updates, entries 23 to 26 verbatim): *"Il DECRETO LEGISLATIVO 30 dicembre 2023, n. 216 (in G.U. 30/12/2023,
n.303) ha disposto (con l'art. 1, comma 1) la modifica dell'art. 11, comma 1."* (for 2024 only, per the
article's AGGIORNAMENTO (229)); *"Il DECRETO-LEGGE 9 agosto 2024, n. 113 ... la modifica dell'art. 11,
comma 4."*; *"La LEGGE 30 dicembre 2024, n. 207 (in SO n.43, relativo alla G.U. 31/12/2024, n.305) ha
disposto (con l'art. 1, comma 2, lettera a)) la modifica dell'art. 11, comma 1."* (the three brackets made
standing); *"La LEGGE 30 dicembre 2025, n. 199 (in SO n.42, relativo alla G.U. 30/12/2025, n.301) ha
disposto (con l'art. 1, comma 3) la modifica dell'art. 11, comma 1, lettera b)."* (35 → 33 per cento).

⚠ **The errand's premise is one law behind:** E-6 asked for the text "as amended by Legge 207/2024"; the
text in force for 2026 is the one Legge 199/2025 amended - the middle bracket is **33 %**, not 35 %. Both
years are on disk; which one a seed reads is the seed vintage's question (one seed vintage per country,
§621), not this folder's.

*Not used:* the Agenzia delle Entrate and the Gazzetta Ufficiale were not re-tried - the normattiva text
answered first. The earlier attempts' record: the Agenzia's portal and normattiva's article view without
`!vig=` are JavaScript shells (the art. 11 page at 903 KB contains no "per cento"); the Gazzetta's article
endpoint returns an 11 KB shell; its issue PDF of Legge 207/2024 (SO n. 43, 25.6 MB) is scanned.

## Sweden — the two layers (no file in this folder; sourced in `POLISIM_TAX_SPECLET.md` §2)

Kommunal skattesats, national average **32.38 %** (SCB, *Kommunalskatterna 2026*); statlig inkomstskatt **20 %** above the
brytpunkt, skiktgräns **643 000 kr** (Skatteverket, *Belopp och procent 2026*). The model (F4-2, 2026-09-13) reads them as
one line with two layers (DS-2d): the municipal rate on every krona, the state rate on the income above the skiktgräns; the
grundavdrag is not modelled and the skiktgräns is read on the whole income - stated, not hidden.
F4-3 (2026-09-15, `COMPLETED.md` §514): the line's own rate is the municipal layer's 32.38 (the blended 52 retired); its revenue
anchor is OECD Revenue Statistics `T_1110`, general government, 2022: **10.389453 %** of GDP - booked **14.656972** local and **−4.267519**
central (the state layer net of the credits it pays) - over 32.38.

## Digests (`sha256sum`, first 16 hex, at fetch)

`964f5bee61b994d8` economie.gouv.fr tranches · `741a32d757bc2502` service-public F1419 ·
`053bfcfc2ba961a3` §32a EStG · `6426b64c1d497d9a` podatki.gov.pl stawki · `e9ada115fb43a4af` rp-25-32.pdf ·
`2fd045566387c0c6` urssaf taux · `4d2754905fbbe1f1` fr-2025-19763 · `eca28a1b7394b2d0` PIT act (U) ·
`307adbff35f42174` SUS act (U) · `bc28df6bf65b55e5` zus.pl składki ·
`b2f8531830f2c0fb` normattiva TUIR art. 11 (2026) · `5f7dc488c1849941` (2025) · `84b7147e18349826` its updates list

## The income tax REALISED, per country (F4-5, fetched 2026-09-16)

What each income row of `TaxBaseTable` is derived from — the realised revenue over the rate the row is read
against. Taxes on the income and profits of individuals, % of GDP, 2022. OECD Revenue Statistics
(`OECD.CTP.TPS,DSD_REV_COMP_OECD@DF_RSOECD` 2.0, `T_1110`), keys and values:

| country | key | % of GDP |
|---|---|--:|
| Germany | `DEU.TAX_REV.S13.T_1110._T.PT_B1GQ.A` | 10.425679 |
| France | `FRA.TAX_REV.S13.T_1110._T.PT_B1GQ.A` | 9.693475 |
| Sweden | `SWE.TAX_REV.S13.T_1110._T.PT_B1GQ.A` | 10.389453 |
| the USA (FEDERAL, used) | `USA.TAX_REV.S1311.T_1110._T.PT_B1GQ.A` | 9.300111 |
| the USA (general government, NOT used) | `USA.TAX_REV.S13.T_1110._T.PT_B1GQ.A` | 11.384784 |
| Poland | the flow carries NO income rows for Poland | — |

Poland's figure is Eurostat `gov_10a_taxag`, D51A, general government, PC_GDP, 2022 = **4.5**, the same source
D-16 (a) used for the same reason. The USA is taken at the FEDERAL level because this project calibrates the
USA on the federal perimeter; the general-government figure is listed only to show the gap between them.

Each figure reproduces the row D-16 (a) derived against the old top rate — 45 × 0.2317 = 10.4265 (Germany),
45 × 0.2154 = 9.693 (France), 32 × 0.1406 = 4.4992 (Poland) — so the fetch confirms the table it re-derives.
