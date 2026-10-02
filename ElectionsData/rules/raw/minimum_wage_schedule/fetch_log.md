# minimum_wage_schedule: fetch log, 2026-10-02 (06:22-06:32 UTC)

Tool: curl 8.21.0 `-sL` with a browser User-Agent (Chrome 128 on Windows), saved with `-o` into this folder. Files are byte-exact as served. The exception is the four `fr_wb*` Wayback Machine `id_` captures. Those were fetched with `--compressed`, so curl undid the HTTP transport gzip, and the file is the original Légifrance HTML as archived.

Columns: fetch time (UTC), HTTP status, saved file, bytes on disk at fetch, source URL. Every request through the fetch helper is listed, including failures. A file name that appears twice was overwritten by the later fetch. Only the final file is kept.

| Fetched (UTC) | HTTP | File | Bytes | URL |
|---|---|---|---|---|
| 2026-10-02T06:23:02Z | 200 | pl_eli_DU_2022_1952.json | 1039 | https://api.sejm.gov.pl/eli/acts/DU/2022/1952 |
| 2026-10-02T06:23:03Z | 200 | pl_eli_DU_2023_1893.json | 1003 | https://api.sejm.gov.pl/eli/acts/DU/2023/1893 |
| 2026-10-02T06:23:09Z | 200 | pl_eli_DU_2022_1952_text.html | 4784 | https://api.sejm.gov.pl/eli/acts/DU/2022/1952/text.html |
| 2026-10-02T06:23:10Z | 200 | pl_eli_DU_2023_1893_text.html | 4904 | https://api.sejm.gov.pl/eli/acts/DU/2023/1893/text.html |
| 2026-10-02T06:23:50Z | 200 | de_milog_full.html | 68030 | https://www.gesetze-im-internet.de/milog/BJNR134810014.html |
| 2026-10-02T06:23:51Z | 404 | (de_milov3_index.html, deleted) | 236 | https://www.gesetze-im-internet.de/milov3/index.html |
| 2026-10-02T06:23:52Z | 200 | de_bmas_mindestlohn.html | 535939 | https://www.bmas.de/DE/Arbeit/Arbeitsrecht/Mindestlohn/mindestlohn.html |
| 2026-10-02T06:24:04Z | 404 | (overwritten by the next line) | 515867 | https://www.bmas.de/DE/Arbeit/Arbeitsrecht/Mindestlohn/einfuehrung-und-anpassung-mindestlohn.html |
| 2026-10-02T06:24:16Z | 200 | de_bmas_einfuehrung_anpassung_mindestlohn.html | 526146 | https://www.bmas.de/DE/Arbeit/Arbeitsrecht/Mindestlohn/Einfuehrung-und-Anpassungen-Mindestlohn/einfuehrung-und-anpassung-mindestlohn.html |
| 2026-10-02T06:24:29Z | 200 | de_bmas_mindestlohnerhoehungsgesetz.html | 551702 | https://www.bmas.de/DE/Service/Gesetze-und-Gesetzesvorhaben/mindestlohnerhoehungsgesetz.html |
| 2026-10-02T06:27:08Z | 200 | fr_wb20221231_legifrance_decret_2022-1608.html | 103724 | https://web.archive.org/web/20221231145916id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000046780043 |
| 2026-10-02T06:27:15Z | 429 | (rate-limit page, overwritten) | 620 | https://web.archive.org/web/20230511123329id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000047495817 |
| 2026-10-02T06:27:21Z | 200 | fr_wb20231229_legifrance_decret_2023-1216.html | 107653 | https://web.archive.org/web/20231229095806id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000048604676 |
| 2026-10-02T06:27:28Z | 429 | (rate-limit page, overwritten) | 620 | https://web.archive.org/web/20241126021133id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000050392683 |
| 2026-10-02T06:28:27Z | 429 | (rate-limit page, overwritten) | 620 | https://web.archive.org/web/20230511123329id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000047495817 |
| 2026-10-02T06:28:49Z | 429 | (rate-limit page, overwritten) | 620 | https://web.archive.org/web/20230511123329id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000047495817 |
| 2026-10-02T06:28:57Z | 200 | fr_servicepublic_actualite_A17008.html | 59063 | https://www.service-public.gouv.fr/particuliers/actualites/A17008 |
| 2026-10-02T06:29:11Z | 200 | (us_uscode_house_29usc206_prelim.html, deleted) | 14615 | https://uscode.house.gov/view.xhtml?req=granuleid:USC-prelim-title29-section206&num=0&edition=prelim |
| 2026-10-02T06:29:12Z | 403 | (us_dol_whd_minimum_wage.html, deleted) | 400 | https://www.dol.gov/agencies/whd/minimum-wage |
| 2026-10-02T06:29:13Z | 200 | us_cornell_29usc206.html | 201959 | https://www.law.cornell.edu/uscode/text/29/206 |
| 2026-10-02T06:29:31Z | 200 | fr_wb20230511_legifrance_arrete_2023-04-26.html | 103256 | https://web.archive.org/web/20230511123329id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000047495817 |
| 2026-10-02T06:29:49Z | 429 | (rate-limit page, overwritten) | 620 | https://web.archive.org/web/20241126021133id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000050392683 |
| 2026-10-02T06:29:59Z | 200 | (fr_servicepublic_actualite_A16528.html, deleted) | 59063 | https://www.service-public.gouv.fr/particuliers/actualites/A16528 |
| 2026-10-02T06:30:11Z | 429 | (rate-limit page, overwritten) | 620 | https://web.archive.org/web/20241126021133id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000050392683 |
| 2026-10-02T06:30:32Z | 200 | fr_servicepublic_F2300_smic_20261002.html | 125990 | https://www.service-public.gouv.fr/particuliers/vosdroits/F2300 |
| 2026-10-02T06:30:53Z | 429 | (rate-limit page, overwritten) | 620 | https://web.archive.org/web/20241126021133id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000050392683 |
| 2026-10-02T06:31:14Z | 200 | (fr_servicepublic_actualite_A17785_novembre_2024.html, deleted) | 64507 | https://www.service-public.gouv.fr/particuliers/actualites/A17785 |
| 2026-10-02T06:31:55Z | 200 | fr_wb20241126_legifrance_decret_2024-951.html | 113157 | https://web.archive.org/web/20241126021133id_/https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000050392683 |

## Notes on deleted and failed fetches

- `de_milov3_index.html`: 404. The MiLoV3 short name is not on gesetze-im-internet.de. MiLoV3 is not needed: its last step (EUR 10.45 from 2022-07-01) was overtaken on 2022-10-01, before the schedule window opens.
- `de_bmas_einfuehrung...` first try: 404, because a wrong path was guessed. The correct link was then taken from the BMAS Mindestlohn page and fetched.
- `us_uscode_house_29usc206_prelim.html`: HTTP 200, but the body was the "Under Maintenance" page (14.6 KB), as on 2026-10-01. Deleted. LII (law.cornell.edu) and the existing GPO govinfo 2024 file are used instead.
- `us_dol_whd_minimum_wage.html`: 403 (Akamai), as on 2026-10-01. Deleted.
- `fr_servicepublic_actualite_A16528.html`: the May 2023 SMIC news item. service-public.gouv.fr now serves the December 2025 A17008 article at this id (same title, same 59063 bytes). It is not the 2023 text, so it was deleted.
- `fr_servicepublic_actualite_A17785_novembre_2024.html`: the "Ce qui change en novembre 2024" id now serves "Ce qui change en novembre 2025". Deleted.
- Wayback 429 lines: rate-limit pages (620 B). Each one was overwritten when the same capture was later fetched with HTTP 200.

## Requests not saved here (lookups only)

- `https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000054126589` (direct): 403 Cloudflare "Just a moment..." challenge, as on 2026-10-01. This is why Légifrance pages were read from Wayback `id_` captures.
- `https://echanges.dila.gouv.fr/OPENDATA/JORF/` (index): the daily JORF archives only go back to 2025-07-13. Older texts are only in `Freemium_jorf_global_20250713-140000.tar.gz` (1.6 GB), which was not downloaded.
- `https://www.bgbl.de/xaver/bgbl/start.xav?startbk=Bundesanzeiger_BGBl&jumpTo=bgbl122s0969.pdf`: HTTP 200, but it is a JavaScript viewer shell with no text. BGBl. I 2022 S. 969 was not read.
- `https://api.sejm.gov.pl/eli/acts/search?publisher=DU&year=2022|2023&title=minimalnego wynagrodzenia`: ELI searches that confirmed the act ids DU/2022/1952 and DU/2023/1893.
- `https://web.archive.org/cdx/search/cdx?url=legifrance.gouv.fr/jorf/id/JORFTEXT000…`: CDX lookups for the capture timestamps used above. The ids came from a web search restricted to legifrance.gouv.fr.
- `https://www.service-public.gouv.fr/actualites/lettresp/archives/L1175`: a newsletter of 21 Nov 2024. It only links to A17785 (see above). Not used.
