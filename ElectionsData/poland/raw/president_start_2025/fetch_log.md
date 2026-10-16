# fetch_log - ElectionsData/poland/raw/president_start_2025/ (SP-7, the presidential start's legal calendar), 2026-10-16

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -o <file> <url>`; bytes stored as received, no re-encoding. Status is the final status after redirects. Digests in `SHA256SUMS.txt`. Lines marked [NOT KEPT] are fetches whose bodies were deleted; they are not sources.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-16 (before 18:35:51Z; not stamped) | 200 | 1018 | eli_meta_DU_2025_48.json | https://api.sejm.gov.pl/eli/acts/DU/2025/48 |
| 2026-10-16T18:35:51Z | 200 | 298913 | eli_DU_2025_48_marszalek_zarzadzenie_wyborow_prezydenta.pdf | https://api.sejm.gov.pl/eli/acts/DU/2025/48/text.pdf |
| 2026-10-16T20:26:35Z | 200 | 60459 | [NOT KEPT] platforma_org_rafal-trzaskowski-kandydatem-na-prezydenta-rp.html | https://platforma.org/aktualnosci/rafal-trzaskowski-kandydatem-na-prezydenta-rp - the post is dated 15 maja 2020: the 2020 nomination, not 2025's |
| 2026-10-16T20:26:38Z | 200 | 928171 | konfederacja_pl_to-juz-oficjalnie-slawomir-mentzen-kandydatem.html | https://konfederacja.pl/to-juz-oficjalnie-slawomir-mentzen-kandydatem-konfederacji-na-prezydenta/ |
| 2026-10-16T20:26:39Z | 200 | 355405 | polska2050_pl_szymon-holownia-bedzie-kandydowal-2025.html | https://polska2050.pl/informacja-prasowa/szymon-holownia-bedzie-kandydowal-w-wyborach-prezydenckich-w-2025-roku/ |
| 2026-10-16T20:26:41Z | 200 | 355075 | polska2050_pl_szymon-holownia-kandydatem-w-wyborach-na-prezydenta.html | https://polska2050.pl/szymon-holownia-kandydatem-w-wyborach-na-prezydenta/ |
| 2026-10-16T20:27:39Z | 200 | 66032 | [NOT KEPT] platforma_org_rada-krajowa-platformy-obywatelskiej-i-nowoczesnej.html | https://platforma.org/aktualnosci/rada-krajowa-platformy-obywatelskiej-i-nowoczesnej - dated 08 czerwca 2019, unrelated |
| 2026-10-16T20:27:40Z | 404 | 52237 | [NOT KEPT] platforma_org_historia-partii.html | https://platforma.org/historia-partii - a 404 page |
| 2026-10-16T20:28:24Z | 200 | 82462 | platforma_org_prawybory-sondaz-2024-11-18.html | https://platforma.org/aktualnosci/radoslaw-sikorski-i-rafal-trzaskowski-wygrywaja-w-drugiej-turze-wyborow-prezydenckich-z-kandydatem-pis |
| 2026-10-16T20:28:24Z | 200 | 60074 | platforma_org_konwencja-koalicji-obywatelskiej-w-gliwicach.html | https://platforma.org/aktualnosci/konwencja-koalicji-obywatelskiej-w-gliwicach |

Notes
- Found through the Sejm ELI API's search already held at `../president/eli_search_DU_2025_prezydent.json` (fetched 2026-10-01; its last item is "Dz.U. 2025 poz. 48 | Postanowienie Marszałka Sejmu Rzeczypospolitej Polskiej z dnia 15 stycznia 2025 r. w sprawie zarządzenia wyborów Prezydenta Rzeczypospolitej Polskiej").
- `eli_meta_DU_2025_48.json`: `announcementDate` and `promulgation` both 2025-01-15, `entryIntoForce` 2025-01-15, `textHTML: false` - so the PDF is the source read. Its legal bases as the API lists them: Kodeks wyborczy art. 289 § 1 and art. 290, Konstytucja art. 128 ust. 2.
- ISAP was not tried: it sits behind a bot wall for this machine (`../president/fetch_log.txt`, 2026-10-01, Incapsula 403), and the Sejm ELI API serves the same Dziennik Ustaw PDF.
- The PDF's text was read by inflating its Flate streams and decoding each font through its own ToUnicode CMap (a throwaway per-font reader in the session's scratchpad; the WinAnsi TrueType fonts carry a two-byte codespace but one-byte codes, so a simple font is read one byte per code). The reading is `../../presidential_calendar_2025.md`.
- The party pages (the review of SP-7's design, finding 2: the three nominations the first pass left NOT HELD) were found by a web search restricted to each party's own domain - platforma.org (the PO site carrying KO's news), konfederacja.pl, polska2050.pl - and fetched with the command above. Each page's date is read from the page itself (its post header and its `datePublished`), and a page is dated by the decision it records where it names one (§621). The reading is `../../presidential_calendar_2025.md` §5.
- NOT FETCHED: the PKW's opinion on the draft calendar, which art. 290 § 2 has the Marshal take before the order. Nothing in the design rests on its date (the start option leans on art. 104, the campaign's opening on the order's publication).
