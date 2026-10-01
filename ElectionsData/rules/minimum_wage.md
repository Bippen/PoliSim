# [SOURCED] Statutory minimum wage — six countries

`[PROVISIONAL]` — first-session sourcing (2026-10-01), not yet re-verified by a second session (README, class SOURCED).
Fetched 2026-10-01 17:40–18:11 UTC. Every page relied on is saved byte-exact under `raw/`; hashes are in
`SHA256SUMS.txt`, and every request (including failed ones) is in `fetch_log.txt`. Quotes are copied from the
saved files. Glosses are mine and are not official translations. Figures are written in the source's own
decimal notation inside quotes and in InvariantCulture (`12.31`) outside them.

| Country | Statutory national minimum? | Current level (as the law states it) | In force from | Instrument |
|---|---|---|---|---|
| Sweden | **No**: wages are set by collective agreements | — | — | none (Regeringskansliet FPM 2020/21:FPM41; Eurostat, 1 Jul 2026) |
| Germany | Yes | **EUR 13.90 gross per hour** (EUR 14.60 from 2027-01-01) | 2026-01-01 | MiLoG § 1, set by the Fünfte Mindestlohnanpassungsverordnung (MiLoV5) of 5 Nov 2025, § 1 |
| France | Yes (SMIC) | **EUR 12.31 gross per hour** (metropolitan France; 1 867.02/month at 35 h/week) | 2026-06-01 | Arrêté du 22 mai 2026 (NOR TRST2612929A), art. 1, under Code du travail L. 3231-5 |
| Italy | **No**: minimum pay comes from national collective agreements (CCNL) | — | — | none (Eurostat, 1 Jul 2026; legge 26 settembre 2025, n. 144 delegates by reference to CCNL) |
| Poland | Yes | **PLN 4 806 per month; PLN 31.40 per hour** (PLN 4 950 / 32.30 from 2027-01-01) | 2026-01-01 | Rozporządzenie Rady Ministrów z 11 września 2025 r. (Dz.U. 2025 poz. 1242), §§ 1–2 |
| United States | Yes (federal floor) | **USD 7.25 per hour** | 24 Jul 2009 (computed from the statute's wording) | FLSA, 29 U.S.C. § 206(a)(1)(C); higher state and local minimums are preserved by § 218(a) |

---

## Sweden: no statutory minimum wage

- **Rule:** Sweden has no statutory minimum wage and does not declare collective agreements universally applicable. Wage formation is left to the social partners (collective agreements).
- **Threshold:** none.
- **Instrument (primary statement of the absence):** Regeringskansliet, *Faktapromemoria 2020/21:FPM41 "Direktiv om tillräckliga minimilöner i Europeiska unionen"*, Arbetsmarknadsdepartementet, 2020-12-02, section 1.3 "Gällande svenska regler och förslagets effekt på dessa".
  - Quote (sv): "I Sverige finns inte lagstadgade minimilöner eller allmängiltigförklaring av kollektivavtal. Lönebildning är en fråga för arbetsmarknadens parter."
  - Gloss: "In Sweden there are no statutory minimum wages and no universal-applicability declaration of collective agreements. Wage formation is a matter for the labour-market parties."
  - URL: https://data.riksdagen.se/dokument/H806FPM41.html — `raw/se_riksdagen_fpm_2020-21-FPM41_H806FPM41.html` — sha256 `8d38b28f38bf48006e54bda2bb6fc86e458bbf2ad4349a35d0ba070c7684b562`
- **Current status (official EU statistics, data as of 1 July 2026):** Eurostat, *Statistics Explained: Minimum wage statistics* (data extracted July 2026; page last edited 31 July 2026).
  - Quote (en): "As of 1 July 2026, there was no national minimum wage in Denmark, Italy, Austria, Finland and Sweden." … "In Denmark, Italy, Austria, Finland and Sweden, as well as in Iceland, Norway and Switzerland, minimum wage are laid down by collective agreements for a range of specific sectors."
  - URL: https://ec.europa.eu/eurostat/statistics-explained/index.php?title=Minimum_wage_statistics — `raw/eu_eurostat_minimum_wage_statistics.html` — sha256 `f8c3da328314ffa7d72ce880f9f69cd449defd7dd7516106a7bab9d96d00a0b8`
- **EU law does not oblige Sweden to introduce one:** Directive (EU) 2022/2041 on adequate minimum wages in the EU, OJ L 275/33, Article 1(4)(a).
  - Quote (en): "Nothing in this Directive shall be construed as imposing an obligation on any Member State: (a) where wage formation is ensured exclusively via collective agreements, to introduce a statutory minimum wage; or (b) to declare any collective agreement universally applicable."
  - URL: http://publications.europa.eu/resource/celex/32022L2041 (the Publications Office Cellar copy of the EUR-Lex document; see fetch_log) — `raw/eu_dir2022-2041_cellar.xhtml` — sha256 `98753837a060e4d25f20612d90284d2cfe0ae0ef64ed18f3ef6142042dbe454f`
  - The Court of Justice's judgment of 11 November 2025 in C-19/23 (Denmark v Parliament and Council) annulled only parts of Article 5 (Article 5(2), a phrase in Article 5(1) and a phrase in Article 5(3)) and "Dismisses the action as to the remainder", so Article 1(4) stands. `raw/eu_cjeu_C-19-23_judgment_cellar.xhtml` — sha256 `bcb3183e9f81a033bf76b1f0cd1053d0b9f0856154bc6c1454b6fd6ef2ba3075`

## Germany: statutory minimum wage (MiLoG)

- **Rule:** every employee is entitled to at least the statutory minimum wage. The Federal Government changes the level by Rechtsverordnung on a proposal from the Mindestlohnkommission.
- **Thresholds (gross, per hour of work):**
  - EUR 12.82 from 2025-01-01 (MiLoV4 § 1 Nr. 2)
  - **EUR 13.90 from 2026-01-01** (MiLoV5 § 1 Nr. 1)
  - EUR 14.60 from 2027-01-01 (MiLoV5 § 1 Nr. 2)
  - The law states no monthly figure; the level is hourly only.
- **Instrument 1, the statute:** Mindestlohngesetz (MiLoG) of 11 Aug 2014 (BGBl. I S. 1348), § 1.
  - Quote (de): "(1) Jede Arbeitnehmerin und jeder Arbeitnehmer hat Anspruch auf Zahlung eines Arbeitsentgelts mindestens in Höhe des Mindestlohns durch den Arbeitgeber. (2) Die Höhe des Mindestlohns beträgt ab dem 1. Oktober 2022 brutto 12 Euro je Zeitstunde. Die Höhe des Mindestlohns kann auf Vorschlag einer ständigen Kommission der Tarifpartner (Mindestlohnkommission) durch Rechtsverordnung der Bundesregierung geändert werden."
  - Gloss: every employee is entitled to at least the minimum wage. It was EUR 12 gross per hour from 1 Oct 2022, and the government may change it by ordinance on the proposal of the standing Minimum Wage Commission of the social partners. (The page's footnote lists the adjusting ordinances, including "§ 1 V v. 5.11.2025 I Nr. 268".)
  - URL: https://www.gesetze-im-internet.de/milog/__1.html — `raw/de_milog_p1.html` — sha256 `3c3bf22d6452b018f636910b4921ff5b135b0f92c068f1e231af2b9a3b6bafc8`
- **Instrument 2, the current level:** Fünfte Mindestlohnanpassungsverordnung (MiLoV5) of 5 Nov 2025, BGBl. 2025 I Nr. 268 (Vollzitat also cites Nr. 312). § 1, in force 2026-01-01 (§ 2).
  - Quote (de): "Der Mindestlohn beträgt 1. ab 1. Januar 2026 13,90 Euro brutto je Zeitstunde, 2. ab 1. Januar 2027 14,60 Euro brutto je Zeitstunde."
  - Gloss: the minimum wage is EUR 13.90 gross per hour from 1 January 2026 and EUR 14.60 from 1 January 2027.
  - URL: https://www.gesetze-im-internet.de/milov5/BJNR10C0A0025.html — `raw/de_milov5_full.html` — sha256 `5a224b690ccb5442bb95d3c84871926d1b248e53be0bc83fee7dfda697ffa3bc`
- **Instrument 3, the 2025 level:** Vierte Mindestlohnanpassungsverordnung (MiLoV4) of 24 Nov 2023, BGBl. 2023 I Nr. 321, § 1.
  - Quote (de): "Der Mindestlohn beträgt 1. ab 1. Januar 2024 12,41 Euro brutto je Zeitstunde, 2. ab 1. Januar 2025 12,82 Euro brutto je Zeitstunde."
  - URL: https://www.gesetze-im-internet.de/milov4/BJNR1410A0023.html — `raw/de_milov4_full.html` — sha256 `93d102b7d9723431c7b9238f645b233dea6a3e108ae8c84bf392d30d06ff5a80`

## France: SMIC (salaire minimum interprofessionnel de croissance)

- **Rule:** a statutory national hourly minimum (the SMIC). It is revalued by decree every 1 January. Within the year it rises automatically, by arrêté, whenever the reference consumer price index (lowest-income-quintile households, excluding tobacco) has risen by at least 2 % since the last setting (Code du travail L. 3231-5, cited by the arrêté).
- **Thresholds (gross):**
  - EUR 12.02 per hour from 2026-01-01, i.e. EUR 1 823.03 per month at the legal 35 h/week (décret n° 2025-1228, art. 1 and notice)
  - **EUR 12.31 per hour from 2026-06-01, i.e. EUR 1 867.02 per month** (arrêté du 22 mai 2026, art. 1 and notice). This is the current level.
  - These are the metropolitan France and DOM figures. Mayotte has its own level: EUR 9.56 per hour from 2026-06-01.
- **Instrument 1, the current level:** *Arrêté du 22 mai 2026 relatif au relèvement du salaire minimum de croissance*, NOR TRST2612929A, JORF n° 0121 du 24 mai 2026, art. 1, in force 2026-06-01. It is taken under Code du travail L. 3231-5, L. 3231-12 and L. 3423-1.
  - Quote (fr, art. 1): "A compter du 1er juin 2026, pour les catégories de travailleurs mentionnés à l'article L. 2211-1 du code du travail, le montant du salaire minimum de croissance est relevé dans les conditions ci-après : 1° En métropole, en Guadeloupe, en Guyane, en Martinique, à La Réunion, à Saint-Barthélemy, à Saint-Martin et à Saint-Pierre-et-Miquelon, son montant est porté à 12,31 € l'heure ; 2° A Mayotte, son montant est fixé à 9,56 € l'heure."
  - Quote (fr, notice): "le montant du SMIC brut horaire à 12,31 €, soit 1 867,02 € mensuels sur la base de la durée légale du travail de 35 heures hebdomadaires"
  - Gloss: from 1 June 2026 the SMIC is raised to EUR 12.31 per hour (gross) in metropolitan France and the DOM, i.e. EUR 1 867.02 per month on the 35-hour legal week. In Mayotte it is EUR 9.56.
  - Source: the official Journal officiel open data of DILA (Direction de l'information légale et administrative). The archive https://echanges.dila.gouv.fr/OPENDATA/JORF/JORF_20260524-002330.tar.gz is saved as `raw/fr_dila_JORF_20260524-002330.tar.gz` (sha256 `c13d5879d22b2165d0a62a612054d37ff21affd3f09a28d90e54dadbe296b872`). The two files extracted from it byte-exact are `raw/fr_jorf_JORFTEXT000054126589_arrete_2026-05-22_smic_version.xml` (sha256 `8991be3532f6947b135052adddb665891b2a0a1f60fa0f890035bc7a8e71db53`) and `raw/fr_jorf_JORFARTI000054126607_arrete_2026-05-22_art1.xml` (sha256 `c54267f9505ad3972939844959872458c9fc958432e6bf52e7f0064efe5c1b0e`). The Légifrance permalink is https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000054126589 (blocked by a Cloudflare challenge, see NOT REACHED).
- **Instrument 2, the 1 January 2026 level:** *Décret n° 2025-1228 du 17 décembre 2025 portant relèvement du salaire minimum de croissance*, JORF n° 0296 du 18 décembre 2025, art. 1.
  - Quote (fr, art. 1): "A compter du 1er janvier 2026, […] 1° En métropole, en Guadeloupe, en Guyane, en Martinique, à La Réunion, à Saint-Barthélemy, à Saint-Martin et à Saint-Pierre-et-Miquelon, son montant est porté à 12,02 euros l'heure ; 2° A Mayotte, son montant est fixé à 9,33 euros l'heure."
  - Quote (fr, notice): "le montant du SMIC brut horaire à 12,02 euros (augmentation de 1,18 %), soit 1 823,03 euros mensuels sur la base de la durée légale du travail de 35 heures hebdomadaires"
  - Source: DILA archive `raw/fr_dila_JORF_20251218-003435.tar.gz` (sha256 `f5991e782bb4d5197abf678ea4ff52977076ca408bed7fbe3bd2b78a6916d327`). Extracted files: `raw/fr_jorf_JORFTEXT000053042520_decret_2025-1228_version.xml` (sha256 `83e1673320d19ae8710ebe2274ea8d4fd0d387cca13f482b1c854e4e17e32079`) and `raw/fr_jorf_JORFARTI000053042543_decret_2025-1228_art1.xml` (sha256 `dd55deabf6a55263163d372f890008ced09c28635a9ad87bbf34166d9593fc5c`).
- **Cross-check (official government portal):** service-public.fr fiche F2300 "Smic", "Vérifié le 01 juin 2026 - Service Public / Direction de l'information légale et administrative (Premier ministre)". Its table reads "Smic horaire 12,31 € … Smic mensuel 1 867,02 €". Its legal references list "Code du travail : articles L3231-2 à L3231-3" and "Arrêté du 22 mai 2026 relatif au relèvement du salaire minimum de croissance".
  - URL: https://www.service-public.fr/particuliers/vosdroits/F2300 — `raw/fr_servicepublic_F2300_smic.html` — sha256 `d94cda7d209b7e0b0876bf2313debfe98c12c40bf0f9efc4e6400437e3859d8b`
- Eurostat (above) has France at EUR 1 867 per month on 1 July 2026, which matches.

## Italy: no statutory minimum wage

- **Rule:** Italy has no statutory national minimum wage. Minimum pay is set by national collective agreements (CCNL), anchored in the constitutional right to a "sufficient" wage (Cost. art. 36). Legge 144/2025 delegates the Government to make the minimum overall pay of the *most-applied* CCNL the minimum for each category. It sets no numeric statutory floor.
- **Threshold:** none.
- **Primary statement of the absence (official EU statistics, data as of 1 July 2026):** Eurostat *Minimum wage statistics*, as quoted under Sweden: "As of 1 July 2026, there was no national minimum wage in Denmark, Italy, Austria, Finland and Sweden." and "… minimum wage are laid down by collective agreements for a range of specific sectors." `raw/eu_eurostat_minimum_wage_statistics.html` — sha256 `f8c3da328314ffa7d72ce880f9f69cd449defd7dd7516106a7bab9d96d00a0b8`
- **Instrument, the wage-setting route:** *Legge 26 settembre 2025, n. 144, "Deleghe al Governo in materia di retribuzione dei lavoratori e di contrattazione collettiva nonchè di procedure di controllo e informazione"* (GU n. 230 del 03-10-2025; in force 18/10/2025), art. 1.
  - Quote (it, art. 1 c. 2 lett. a): "definire, per ciascuna categoria di lavoratori, i contratti collettivi nazionali di lavoro maggiormente applicati in riferimento al numero delle imprese e dei dipendenti, al fine di prevedere che il trattamento economico complessivo minimo dei contratti collettivi nazionali di lavoro maggiormente applicati costituisca, ai sensi dell'articolo 36 della Costituzione, la condizione economica minima da riconoscere ai lavoratori appartenenti alla medesima categoria"
  - Gloss: (the delegated decrees shall) identify, for each category of workers, the most-applied national collective agreements (by number of firms and employees), so that the minimum overall pay in those agreements becomes, under Constitution art. 36, the minimum economic condition owed to workers in that category.
  - Art. 1 c. 1 gives the deadline: decrees "entro sei mesi dalla data di entrata in vigore della presente legge" (within six months of entry into force). **Whether the decrees were adopted was not checked** (see NOT REACHED).
  - URL: https://www.normattiva.it/uri-res/N2Ls?urn:nir:stato:legge:2025-09-26;144~art1 — `raw/it_normattiva_legge_2025-144_art1.html` — sha256 `8358ef29e0039d21c44fe76f0d0b9f60802660468fbd010ad549644165c94b95`
- **Constitutional anchor:** Costituzione art. 36.
  - Quote (it): "Il lavoratore ha diritto ad una retribuzione proporzionata alla quantità e qualità del suo lavoro e in ogni caso sufficiente ad assicurare a sé e alla famiglia un'esistenza libera e dignitosa."
  - Gloss: the worker has the right to pay proportionate to the quantity and quality of the work and in any case sufficient to ensure a free and dignified existence for the worker and family.
  - URL: https://www.normattiva.it/uri-res/N2Ls?urn:nir:stato:costituzione:1947-12-27~art36 — `raw/it_normattiva_costituzione_art36.html` — sha256 `50d5151171f64b48feef3470e85a76fde31d242fea36ca46f9f32640fd4565c3`

## Poland: statutory minimum wage (monthly) and minimum hourly rate

- **Rule:** the minimum monthly wage and the minimum hourly rate are negotiated each year in the Rada Dialogu Społecznego (Social Dialogue Council). If the Council does not agree, the Rada Ministrów (Council of Ministers) sets both by rozporządzenie (regulation) by 15 September for the following year. In 2024, 2025 and 2026 they were set by regulation.
- **Thresholds (gross):**
  - 2025: PLN 4 666 per month; PLN 30.50 per hour (Dz.U. 2024 poz. 1362)
  - **2026 (current): PLN 4 806 per month; PLN 31.40 per hour** (Dz.U. 2025 poz. 1242)
  - 2027 (already promulgated): PLN 4 950 per month; PLN 32.30 per hour (Dz.U. 2026 poz. 1213)
- **Instrument 1, the statute:** *Ustawa z dnia 10 października 2002 r. o minimalnym wynagrodzeniu za pracę*. The consolidated text is Obwieszczenie Marszałka Sejmu z 27 listopada 2024 r., Dz.U. 2024 poz. 1773, art. 2 ust. 5. ELI status of the base act DU/2002/1679: "akt posiada tekst jednolity". The ELI search found no newer act titled "o minimalnym wynagrodzeniu" in 2025–2026.
  - Quote (pl, art. 2 ust. 5): "Jeżeli Rada Dialogu Społecznego nie uzgodni w terminie, o którym mowa w ust. 3, wysokości minimalnego wynagrodzenia w roku następnym oraz nie ustali wysokości minimalnej stawki godzinowej w roku następnym, Rada Ministrów ustala, w drodze rozporządzenia, w terminie do dnia 15 września każdego roku, wysokość minimalnego wynagrodzenia w roku następnym, a także wysokość minimalnej stawki godzinowej w roku następnym wraz z terminem zmiany tych wysokości."
  - Gloss: if the Social Dialogue Council does not agree the next year's minimum wage and minimum hourly rate in time, the Council of Ministers sets them by regulation by 15 September each year.
  - URL: https://api.sejm.gov.pl/eli/acts/DU/2024/1773/text.html — `raw/pl_eli_DU_2024_1773_text.html` — sha256 `7f9a30d7c86c526769ece303f5910c7e4c9ad77e73bfd376b00665170e9bfdc3`. Metadata: `raw/pl_eli_DU_2024_1773.json` (`f28e670d…`) and `raw/pl_eli_DU_2002_1679.json` (`10304550…`).
- **Instrument 2, the current level:** *Rozporządzenie Rady Ministrów z dnia 11 września 2025 r. w sprawie wysokości minimalnego wynagrodzenia za pracę oraz wysokości minimalnej stawki godzinowej w 2026 r.*, Dz.U. 2025 poz. 1242 (promulgated 15 Sep 2025), §§ 1–2, in force 2026-01-01 (§ 3).
  - Quote (pl): "§ 1. Od dnia 1 stycznia 2026 r. ustala się minimalne wynagrodzenie za pracę w wysokości 4806 zł. § 2. Od dnia 1 stycznia 2026 r. ustala się minimalną stawkę godzinową w wysokości 31,40 zł."
  - Gloss: from 1 January 2026 the minimum wage is PLN 4 806 (per month) and the minimum hourly rate is PLN 31.40.
  - URL: https://api.sejm.gov.pl/eli/acts/DU/2025/1242/text.pdf — `raw/pl_eli_DU_2025_1242_text.pdf` — sha256 `bf547c5f5d601c33296c2ffbf646f6582dbd896d113640bca73f56743f5fb8b9` (ELI metadata `raw/pl_eli_DU_2025_1242.json`, `5358fbee…`). The text was read from the PDF with a crude Flate+ToUnicode decoder (no PDF renderer on this machine). The decoded words and figures read cleanly.
- **Instrument 3, the 2025 level:** *Rozporządzenie Rady Ministrów z dnia 12 września 2024 r. …w 2025 r.*, Dz.U. 2024 poz. 1362, §§ 1–2.
  - Quote (pl): "§ 1. Od dnia 1 stycznia 2025 r. ustala się minimalne wynagrodzenie za pracę w wysokości 4666 zł. § 2. Od dnia 1 stycznia 2025 r. ustala się minimalną stawkę godzinową w wysokości 30,50 zł."
  - URL: https://api.sejm.gov.pl/eli/acts/DU/2024/1362/text.html — `raw/pl_eli_DU_2024_1362_text.html` — sha256 `2f908cc81ebfbd2a8b2ddec5cc53ef1c55620c146494741e1a2e1d244e1aecd1`
- **Instrument 4, the 2027 level (promulgated, not yet in force):** *Rozporządzenie Rady Ministrów z dnia 14 września 2026 r. …w 2027 r.*, Dz.U. 2026 poz. 1213, §§ 1–2.
  - Quote (pl): "§ 1. Od dnia 1 stycznia 2027 r. ustala się minimalne wynagrodzenie za pracę w wysokości 4950 zł. § 2. Od dnia 1 stycznia 2027 r. ustala się minimalną stawkę godzinową w wysokości 32,30 zł."
  - URL: https://api.sejm.gov.pl/eli/acts/DU/2026/1213/text.pdf — `raw/pl_eli_DU_2026_1213_text.pdf` — sha256 `552c00bd370cde16dc9675baf06885e8c40bc62b54f017a956bbb2f0d46ca43e`

## United States: federal minimum wage (FLSA) plus higher state and local minimums

- **Rule:** the Fair Labor Standards Act sets a federal hourly minimum for covered employees. States and municipalities may set higher minimums, and the FLSA expressly does not excuse non-compliance with them.
- **Threshold:** **USD 7.25 per hour**, effective "24 months after [the] 60th day" after May 25, 2007. By my arithmetic that is 24 July 2009; the statute gives no calendar date. The federal level has not changed since.
- **Instrument:** 29 U.S.C. § 206(a)(1) (FLSA § 6, as amended by the Fair Minimum Wage Act of 2007). United States Code, 2024 Edition, as published by GPO on govinfo.
  - Quote (en): "(1) except as otherwise provided in this section, not less than— (A) $5.85 an hour, beginning on the 60th day after May 25, 2007; (B) $6.55 an hour, beginning 12 months after that 60th day; and (C) $7.25 an hour, beginning 24 months after that 60th day;"
  - URL: https://www.govinfo.gov/content/pkg/USCODE-2024-title29/html/USCODE-2024-title29-chap8-sec206.htm — `raw/us_29usc206_govinfo2024.htm` — sha256 `9dbfe952c7ce43d70abb16c2966f352554187f0e6c60b5de1db01346e898afa6`
- **State minimums:** 29 U.S.C. § 218(a).
  - Quote (en): "No provision of this chapter or of any order thereunder shall excuse noncompliance with any Federal or State law or municipal ordinance establishing a minimum wage higher than the minimum wage established under this chapter …"
  - URL: https://www.govinfo.gov/content/pkg/USCODE-2024-title29/html/USCODE-2024-title29-chap8-sec218.htm — `raw/us_29usc218_govinfo2024.htm` — sha256 `50742ec4433ec53aeff95f05d2c0aa864dfb1e1aa1dbc37b38a2014621644220`
  - The state-by-state levels were **not** sourced (see NOT REACHED).

---

## NOT REACHED

- **Légifrance** (`www.legifrance.gouv.fr`): Cloudflare "Just a moment..." challenge (HTTP 403). I could not fetch the Code du travail articles L. 3231-2 to L. 3231-12, which set up the SMIC and its indexation, as articles. They are cited here only as the arrêté and décret themselves cite them (visas and "Application" notice). The SMIC instruments were taken from DILA's official JORF open-data XML instead.
- **travail-emploi.gouv.fr**: F5 TSPD JavaScript bot wall; not used.
- **US Department of Labor state minimum-wage table** (`www.dol.gov/agencies/whd/minimum-wage/state`): HTTP 403 (Akamai). No state levels are given here. Only the legal fact that higher state and local minimums prevail (29 U.S.C. § 218(a)) is sourced.
- **uscode.house.gov** (the Office of the Law Revision Counsel "prelim" edition): "Site is currently under maintenance" on 2026-10-01. The GPO govinfo 2024 edition was used, and § 206 is unchanged in it.
- **Italy, legge 144/2025 implementation**: I did not check whether the legislative decrees under art. 1 were adopted. Normattiva's "atti attuativi" were not queried. Even if they were adopted, the law's criterion is the CCNL minimum, not a statutory numeric rate.
- **Sweden, a current (2025–2026) Swedish government page** restating the absence (e.g. Medlingsinstitutet or regeringen.se) was not fetched. The 2020 FPM gives the Swedish-law statement and Eurostat (1 July 2026) gives the current status.
