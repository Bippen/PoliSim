# [SOURCED] Statutory minimum wage schedules, 2023 to the latest enacted step

`[PROVISIONAL]`: first-session sourcing (2026-10-02). It has not been re-verified by a second session.
The extract is `minimum_wage_rates.csv`, one row per step. Pages fetched in this pass are in `raw/minimum_wage_schedule/`, with `fetch_log.md` and `SHA256SUMS.txt` in that folder. Pages that the 2026-10-01 pass already held are reused from `raw/`, with their hashes in `SHA256SUMS.txt` and their requests in `fetch_log.txt`. All fourteen reused files were re-hashed today and match the 2026-10-01 sums.

The designer's ruling sets the scope: Sweden and Italy are Off, and Germany, France, Poland and the USA carry their statutory rates.

The window runs from 1 January 2023 through the latest rate already enacted. The earliest game start is 2023-02-19 (Poland). Each country's first row is the rate in force on 2023-01-01, even when it took effect earlier (Germany 2022-10-01, USA 2009-07-24). Figures are written InvariantCulture (`12.41`), and the source text uses decimal commas.

## Sweden and Italy: Off

- **Sweden:** there is no statutory minimum; pay floors come from collective agreements. Source: Regeringskansliet FPM 2020/21:FPM41, section 1.3: "I Sverige finns inte lagstadgade minimilöner eller allmängiltigförklaring av kollektivavtal." This is `raw/se_riksdagen_fpm_2020-21-FPM41_H806FPM41.html`, as cited in `minimum_wage.md`. Eurostat confirms there was no national minimum wage as of 1 July 2026 (`raw/eu_eurostat_minimum_wage_statistics.html`).
- **Italy:** there is no statutory minimum; pay floors come from collective agreements (CCNL). Eurostat states the same as for Sweden. Legge 144/2025 art. 1 points the minimum at the most-applied CCNL and sets no numeric floor. Sources: `raw/eu_eurostat_minimum_wage_statistics.html` and `raw/it_normattiva_legge_2025-144_art1.html`.

## Germany: EUR per hour, gross

| From | Rate | Instrument | Read in |
|---|---|---|---|
| 2022-10-01 (in force 2023-01-01) | 12.00 | MiLoG § 1 Abs. 2: "ab dem 1. Oktober 2022 brutto 12 Euro je Zeitstunde" | gesetze-im-internet.de `milog/__1.html` → `raw/de_milog_p1.html` |
| 2024-01-01 | 12.41 | MiLoV4 § 1 Nr. 1 (24 Nov 2023, BGBl. 2023 I Nr. 321) | `milov4/BJNR1410A0023.html` → `raw/de_milov4_full.html` |
| 2025-01-01 | 12.82 | MiLoV4 § 1 Nr. 2 | same |
| 2026-01-01 | 13.90 | MiLoV5 § 1 Nr. 1 (5 Nov 2025, BGBl. 2025 I Nr. 268; Nr. 312) | `milov5/BJNR10C0A0025.html` → `raw/de_milov5_full.html` |
| 2027-01-01 | 14.60 | MiLoV5 § 1 Nr. 2 (enacted future step) | same |

- **Cross-check (official):** BMAS, "Einführung und Anpassungen des Mindestlohns" (`raw/minimum_wage_schedule/de_bmas_einfuehrung_anpassung_mindestlohn.html`). Its table "Bisher festgesetzte Mindestlöhne" lists 01.10.2022 12 Euro, 01.01.2024 12,41, 01.01.2025 12,82, 01.01.2026 13,90 and 01.01.2027 14,60. This matches every row.
- **The 12.00 instrument:** BMAS names it as the "Gesetz zur Erhöhung des Schutzes durch den gesetzlichen Mindestlohn und zu Änderungen im Bereich der geringfügigen Beschäftigung vom 30. Juni 2022 (BGBl. I S. 969)". The BMAS law page dates its promulgation ("Gesetz ist verkündet") to 30.06.2022.
  - **Conflict note:** I believe the Ausfertigung date is 28 June 2022, but I could not open BGBl. I 2022 S. 969 (bgbl.de serves a JavaScript viewer), so I could not check it. The CSV therefore cites the page reference only, not a date.
  - The gesetze-im-internet.de footnote to MiLoG § 1 lists only the adjustment ordinances (MiLoV3, MiLoV4, MiLoV5), not the 2022 amending law.
- Two smaller points:
  - MiLoV3's steps (up to EUR 10.45 from 2022-07-01) were overtaken before the window opens, so they are not included.
  - No step after 2027-01-01 has been enacted. The Mindestlohnkommission's next decision is not yet due.

## France: SMIC horaire brut, EUR per hour (metropolitan France and the DOM except Mayotte)

| From | Rate | Instrument | Read in |
|---|---|---|---|
| 2023-01-01 | 11.27 | Décret n° 2022-1608 du 22 décembre 2022, art. 1 (JORF n°0297, 23 Dec 2022) | Légifrance JORFTEXT000046780043, Wayback capture 2022-12-31 |
| 2023-05-01 | 11.52 | Arrêté du 26 avril 2023, art. 2. This is the automatic +2.22 % under L. 3231-5 (JORF n°0099, 27 Apr 2023) | JORFTEXT000047495817, Wayback 2023-05-11 |
| 2024-01-01 | 11.65 | Décret n° 2023-1216 du 20 décembre 2023, art. 1 (JORF n°0295, 21 Dec 2023) | JORFTEXT000048604676, Wayback 2023-12-29 |
| 2024-11-01 | 11.88 | Décret n° 2024-951 du 23 octobre 2024, art. 1 (JORF n°0253, 24 Oct 2024) | JORFTEXT000050392683, Wayback 2024-11-26 |
| 2026-01-01 | 12.02 | Décret n° 2025-1228 du 17 décembre 2025, art. 1 | DILA JORF open-data XML (already in `raw/`) |
| 2026-06-01 | 12.31 | Arrêté du 22 mai 2026, art. 1 (automatic, under L. 3231-5) | DILA JORF open-data XML (already in `raw/`) |

- **No step on 2025-01-01.**
  - The décret 2024-951 notice explains why: "Ce relèvement anticipé de 2 % résulte de l'application de la formule du calcul de la revalorisation annuelle du SMIC, telle qu'elle est réalisée en fin d'année". In other words, the November 2024 rise was the early-applied year-end revaluation.
  - service-public.gouv.fr A17008 (DILA, published 15 Dec 2025, `raw/minimum_wage_schedule/fr_servicepublic_actualite_A17008.html`) confirms it: "La dernière augmentation (anticipée) du salaire minimum date de novembre 2024 … Il n'y avait pas eu de nouvelle augmentation au 1er janvier 2025", and "Smic horaire brut : 12,02 € (contre 11,88 €)".
  - So EUR 11.88 held from 2024-11-01 to 2025-12-31.
- **Légifrance is still behind Cloudflare** (HTTP 403 today). The four 2022–2024 texts were therefore read from Internet Archive `id_` captures. These are the unmodified HTML Légifrance served on the capture dates. Each page carries the title, NOR, ELI, the JORF number, the notice and the article text.
  - I marked these rows SOURCED because the text is the official page as archived. If the project wants live-only reads, these four rows are the ones to re-check. The DILA Freemium archive (1.6 GB) holds the same texts.
- **The current level is unchanged:** service-public F2300, fetched today and "Vérifié le 01 juin 2026", still shows 12,31 €. File: `raw/minimum_wage_schedule/fr_servicepublic_F2300_smic_20261002.html`.
- **Mayotte has its own lower SMIC.** It is not in the CSV. The instruments above give: 8.51 (2023-01), 8.70 (2023-05), 8.80 (2024-01), 8.98 (2024-11), 9.33 (2026-01), 9.56 (2026-06).
- **Monthly equivalents:** the instruments' notices give 35 h/week monthly equivalents: 1 709.28, 1 747.20, 1 766.92, 1 801.80, 1 823.03 and 1 867.02. These are derived figures; the legal rate is hourly.

## Poland: PLN per month gross and PLN per hour gross

| From | Monthly | Hourly | Instrument (Rozporządzenie Rady Ministrów) |
|---|---|---|---|
| 2023-01-01 | 3490 | 22.80 | of 13 Sep 2022, Dz.U. 2022 poz. 1952, §§ 1–2 |
| 2023-07-01 | 3600 | 23.50 | same, §§ 3–4 |
| 2024-01-01 | 4242 | 27.70 | of 14 Sep 2023, Dz.U. 2023 poz. 1893, §§ 1–2 |
| 2024-07-01 | 4300 | 28.10 | same, §§ 3–4 |
| 2025-01-01 | 4666 | 30.50 | of 12 Sep 2024, Dz.U. 2024 poz. 1362, §§ 1–2 |
| 2026-01-01 | 4806 | 31.40 | of 11 Sep 2025, Dz.U. 2025 poz. 1242, §§ 1–2 |
| 2027-01-01 | 4950 | 32.30 | of 14 Sep 2026, Dz.U. 2026 poz. 1213, §§ 1–2 (promulgated 15 Sep 2026, future step) |

- **Sources:** all seven steps were read on the Sejm ELI API (api.sejm.gov.pl), which is the official publication data of Dziennik Ustaw.
  - 2023 and 2024 are new HTML texts in `raw/minimum_wage_schedule/`, for example "§ 3. Od dnia 1 lipca 2023 r. ustala się minimalne wynagrodzenie za pracę w wysokości 3600 zł."
  - 2025 is the existing HTML in `raw/`.
  - 2026 and 2027 are the existing PDFs, decoded again today with the Flate+ToUnicode reader. The figures read cleanly: 4806 / 31,40 and 4950 / 32,30.
- **Legal basis:** every regulation is issued under art. 2 ust. 5 of the ustawa z 10 października 2002 r. o minimalnym wynagrodzeniu za pracę, as the ELI metadata for each act shows.
- **Two different instruments:** the monthly minimum applies to employment contracts (umowa o pracę). The hourly rate (minimalna stawka godzinowa) applies to umowy zlecenia and services contracts. The hourly rate is not the monthly minimum divided by hours.
  - The consolidated act (`raw/pl_eli_DU_2024_1773_text.html`, art. 1 pkt 1a) defines it as "minimalna wysokość wynagrodzenia za każdą godzinę wykonania zlecenia lub świadczenia usług".
  - Art. 2 ust. 3a says it is "corocznie waloryzowana" (indexed each year) by the change in the monthly minimum.

## USA: federal minimum, USD per hour

| From | Rate | Instrument | Read in |
|---|---|---|---|
| 2009-07-24 (computed) | 7.25 | 29 U.S.C. 206(a)(1)(C) (FLSA § 6, Fair Minimum Wage Act of 2007, Pub. L. 110-28 § 8102) | govinfo USCODE-2024 → `raw/us_29usc206_govinfo2024.htm`, cross-checked today on LII → `raw/minimum_wage_schedule/us_cornell_29usc206.html` |

- The statute reads "$7.25 an hour, beginning 24 months after that 60th day" after May 25, 2007. The 60th day after May 25, 2007 is 2007-07-24, so the rate began on 2009-07-24 (computed; the statute gives no calendar date).
- The latest amendment that LII's source credits list for § 206 is Pub. L. 114-187 (2016, PROMESA). There has been no federal step since 2009, so the window holds one row.
- State and local minimums can be higher and prevail (29 U.S.C. 218(a), see `minimum_wage.md`). They are not sourced here.

## Not reached / not done

- **legifrance.gouv.fr:** Cloudflare 403, so the pages were read via Wayback captures (see France above).
- **travail-emploi.gouv.fr:** this pass did not try it again; on 2026-10-01 it served a TSPD bot wall.
- **BGBl. I 2022 S. 969** (the 2022 German amending law): bgbl.de served only a JavaScript viewer. The law's date is taken from BMAS (see the conflict note under Germany).
- **dol.gov:** 403 (Akamai), so the DOL federal page and history chart were not read.
- **uscode.house.gov:** still "Under Maintenance". GPO govinfo (2024 edition) and LII were used instead.
- **service-public news items from 2023 and 2024:** their ids now serve newer articles (A16528 serves A17008's text; A17785 is now the November 2025 page). They were not kept.
