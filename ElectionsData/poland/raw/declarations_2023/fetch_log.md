# fetch_log - ElectionsData/poland/raw/declarations_2023/ (Elias's ruling E2, the parties' 2023 declarations), 2026-10-04

Every page here is a source `ElectionsData/poland/coalition_declarations_2023.md` cites in its register. Each is stored byte for byte as it
was served on 2026-10-04, and its SHA-256 is in `SHA256SUMS.txt` beside it (the register carries the same digests).

**The whole sweep stays out of tree:** `PoliSim-captures/sources/poland_declarations_2023/<declarer>/`. It holds every page the sweep saved,
the corroborations and the dropped findings' pages included. Each folder carries the checksum file and URL list its sweep wrote, and not
every folder has both:
- PiS has no checksum file; its other files' digests are in the record's register footer.
- CROSS, TD and PiS have no URL list.
- Konf's `SOURCES.tsv` and NL's `urls.tsv` give the URL and the day accessed.
- Only KO's `SOURCES.tsv` also gives the publisher and the page's date as served.

Where a folder has no URL row for a page, the page's saved bytes carry its canonical link (rel=canonical / og:url), or its `link` field
for pis.org.pl's REST records, and the register gives the URL of every page it lists. The one exception is uncited and out of tree:
`TD/pl2050_2023-04-27_umowa-skan.pdf`, Polska 2050's image-only scan of the 27 April agreement ([TD-P2] is the cited copy), fetched, as the
sweep's transcript records, from https://polska2050.pl/assets/uploads/2023/04/4df0e7ac-10fe-717b-fc12-b7d6a6b5c91f.pdf. The pages here were
copied from the sweep unchanged, their digests re-checked on the copy. Since §776's review they include the pages §12's doubts lean on.

**How the pages were read** (as the register's rows say):
- **Pages as served:** the publishers' own article pages, saved whole.
- **pis.org.pl:** each page is paired with the site's own WordPress REST record of the same post (`pis_wpjson_post_*.json`), which gives the
  publication and modification times.
- **X:** Mentzen's post of 2023-07-06 and Bosak's of 2023-10-16, through X's syndication endpoint (`x_*_syndication.json`), which gives
  `created_at` and `isEdited`.
- **psl.pl:** it did not answer, so PSL's pages are Internet Archive `id_` captures (the archive's original bytes). The 27 April 2023
  agreement PDF's SHA-1 matches the archive's own digest for it.

Folders by declarer: `PiS`, `Konf`, `TD`, `NL`, `KO`. `CROSS` holds pages that bear on more than one party.

**Added 2026-10-05 (Elias's ruling F1, "keep X from power" is a red line):** two of NL's own pages, held out of tree since the sweep of
2026-10-04 and brought in for the line [NL-P31] dates and [NL-P32] restates - `NL/lewica_org_20220920_czarzasty_deklaracja_koalicji.html`
([NL-P31], NL → PiS from 2022-09-20) and `NL/lewica_org_20230123_apelujemy_o_wspolprace.html` ([NL-P32], the National Board's resolution of
21 January 2023). Byte for byte the sweep's copies; their digests are in `SHA256SUMS.txt` and the record's register.

**Added 2026-10-05 (`COMPLETED.md` §780, the review of F1 and F2):** five more pages held out of tree since the sweep, brought in because the
record's §4, §11 and §12 now cite them - `NL/lewica_org_20230826_wiec_czestochowa.html` ([NL-P33], NL's own page of 2023-08-26; modified
2026-01-29), `NL/lewica_org_20220131_czarzasty_musimy_wspolrzadzic.html` ([NL-P34], NL's own page of 2022-01-31),
`NL/tvn24_st7322187_konwencja_lodz.html` ([NL-I13], TVN24, 2023-09-02), `KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html` ([KO-I13],
TVN24, 2023-08-09) and `Konf/x_rozmowa-rmf_1671214148456251392_syndication.json` ([KONF-I26], RMF's own post of 2023-06-20, through X's
syndication endpoint). Byte for byte the sweep's copies; their digests are in `SHA256SUMS.txt` and the record's register.

**KO's own record, swept 2026-10-05 (`COMPLETED.md` §780; F2's "a date from the party's own record always wins", the review's finding that it
had not been searched):** platforma.org (its news, live and through the Internet Archive's CDX index), koalicjaobywatelska.pl and its 2023
campaign pages, and the X posts of Donald Tusk, of KO's own account @Obywatelska_KO (formerly @Platforma_org) and of the spokesman Jan Grabiec -
post ids from the Internet Archive's index of their status URLs, each post's text and `created_at` from X's syndication endpoint - from January
2022 to polling day. Out of tree under `PoliSim-captures/sources/poland_declarations_2023/KO/own/`, listed with digests in its `SOURCES.tsv`.
2021 was not swept. Brought into the tree for KO → PiS, which they now date or weigh (§5): `KO/x_donaldtusk_1491474675771355145_syndication.json`
([KO-P1], 2022-02-09) and `KO/x_obywatelska_ko_1494201933447544834_syndication.json`, `KO/x_obywatelska_ko_1526989992060502016_syndication.json`,
`KO/x_obywatelska_ko_1704443105661886953_syndication.json` ([KO-P2] to [KO-P4]), and `KO/x_obywatelska_ko_1487416047955324930_syndication.json`
([KO-P5], 2022-01-29, earlier and less plain) - byte for byte the sweep's copies, renamed; their digests are in
`SHA256SUMS.txt` and the record's register.

Konfederacja's own record has not been swept as KO's was; its own pages here are the §776 sweep's ([KONF-P1], [KONF-P2], [KONF-P3]).
