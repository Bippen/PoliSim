# fetch_log - ElectionsData/poland/raw/electoral_code/ (item B5, presidential nomination), 2026-10-02

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -o <file> <url>`; bytes stored as received, no re-encoding. Status is the final status after redirects. Digests in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-02T06:22:37Z | 200 | 34058 | eli_meta_DU_2011_112.json | https://api.sejm.gov.pl/eli/acts/DU/2011/112 |
| 2026-10-02T06:22:49Z | 200 | 1011 | eli_meta_DU_2026_1261.json | https://api.sejm.gov.pl/eli/acts/DU/2026/1261 |
| 2026-10-02T06:22:50Z | 200 | 1128 | eli_meta_DU_2026_178.json | https://api.sejm.gov.pl/eli/acts/DU/2026/178 |
| 2026-10-02T06:22:59Z | 200 | 1515651 | eli_DU_2026_1261_kodeks_wyborczy_tekst_jednolity.pdf | https://api.sejm.gov.pl/eli/acts/DU/2026/1261/text.pdf |
| 2026-10-02T06:23:02Z | 200 | 2420401 | eli_DU_2011_112_kodeks_wyborczy.html | https://api.sejm.gov.pl/eli/acts/DU/2011/112/text.html |

Notes
- `eli_meta_DU_2011_112.json` lists eight consolidated texts under "Inf. o tekście jednolitym"; the newest is **DU/2026/1261** (Obwieszczenie Marszałka Sejmu of 1 Sep 2026, Dz.U. 2026 poz. 1261, promulgated 28 Sep 2026, legal state as of 25 Aug 2026). It has no HTML text (`textHTML: false`), so the PDF is the source read.
- `eli_DU_2011_112_kodeks_wyborczy.html` is the act **as originally published in 2011** (the ELI HTML of the base act, not a consolidated text). Kept only as a cross-check; it is byte-identical in size to `../records/eli_DU_2011_112_kodeks_wyborczy.html` (fetched 2026-09-24).
- `eli_meta_DU_2026_178.json`: the amending act of 23 Jan 2026 (Dz.U. 2026 poz. 178, the "portal poparcia" online-support amendment), entry into force 18 Feb 2027 (art. 3 from 4 Mar 2026).
- The Constitution (Art. 127 ust. 3) is read from `../president/eli_DU_1997_483_konstytucja_U_D19970483Lj.pdf`, held since 2026-10-01 (SHA-256 9758dc31bcbcae253d0ac5499d1734954979d123397d61fc9773e287412d64b2, listed in `../president/SHA256SUMS.txt`); not re-fetched.
- PDF text was read by inflating the Flate streams and decoding each font through its own ToUnicode CMap (per-font extractor, scratchpad `pdftext3.pl`); a single shared map had produced "L" for "," and "M" for the hyphen.
