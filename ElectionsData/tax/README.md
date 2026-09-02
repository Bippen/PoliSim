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
parts fiscales"* — the quotient is the instrument, not a footnote to it. ⚠ CSG/CRDS: not on these pages;
still BILLED.

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

⚠ The OASDI wage base (Social Security Administration) is still BILLED: ssa.gov returns 403 to a batch
fetch, and a figure recalled is a figure invented.

## Poland — skala podatkowa (`poland/podatki_gov_pl_stawki.html`, podatki.gov.pl)

Verbatim, *"Skala podatkowa od 2022 roku"*: podstawa obliczenia podatku do **120 000 zł**: **12 %
minus kwota zmniejszająca podatek 3 600 zł**; ponad 120 000 zł: **10 800 zł + 32 % nadwyżki ponad
120 000 zł**. ⚠ The *danina solidarnościowa* and ZUS contributions are not on this page and stay BILLED.

## Italy — STILL BILLED

The Agenzia delle Entrate's portal and normattiva's article view are JavaScript shells that hand a batch
fetch no article text (the TUIR art. 11 page fetched at 903 KB contained no "per cento"); the shell was
not kept. The ask is a primary PDF or a page that renders without a browser — a real errand.

## Digests (`sha256sum`, first 16 hex, at fetch)

`964f5bee61b994d8` economie.gouv.fr tranches · `741a32d757bc2502` service-public F1419 ·
`053bfcfc2ba961a3` §32a EStG · `6426b64c1d497d9a` podatki.gov.pl stawki · `e9ada115fb43a4af` rp-25-32.pdf
