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
