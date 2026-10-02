# fetch_log - ElectionsData/rules/raw/sweden_fiscal_framework/ (item B6, Sweden's debt anchor and balance target), 2026-10-02

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -o <file> <url>`; bytes stored as received, no re-encoding. Status is the final status after redirects. Digests in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-02T06:26:10Z | 200 | 382 | [NOT KEPT] dokumentlista_skr_2025-26_76.json | https://data.riksdagen.se/dokumentlista/?doktyp=skr&rm=2025%2F26&nr=76&utformat=json |
| 2026-10-02T06:26:30Z | 200 | 154772 | dokumentstatus_HD0376.json | https://data.riksdagen.se/dokumentstatus/HD0376.json |
| 2026-10-02T06:26:45Z | 200 | 143568 | prop_2025-26_76_HD0376.html | https://data.riksdagen.se/dokument/HD0376.html |
| 2026-10-02T06:26:45Z | 200 | 168938 | prop_2025-26_76_HD0376.txt | https://data.riksdagen.se/dokument/HD0376.text |
| 2026-10-02T06:26:45Z | 200 | 103752 | dokumentstatus_HD01FiU14.json | https://data.riksdagen.se/dokumentstatus/HD01FiU14.json |
| 2026-10-02T06:27:27Z | 200 | 289932 | skr_2025-26_76_Skrivelse_76_202526.pdf | https://data.riksdagen.se/fil/9FF16874-235A-4F74-9698-A92C2AF7AB78 |
| 2026-10-02T06:27:27Z | 200 | 2345589 | dokumentstatus_HD01FiU1.json | https://data.riksdagen.se/dokumentstatus/HD01FiU1.json |
| 2026-10-02T06:27:48Z | 200 | 21307 | utskottsforslag_HD01FiU1.xml | https://data.riksdagen.se/utskottsforslag/HD01FiU1 |

Notes
- The list query with `doktyp=skr` returned 0 hits (the Riksdag files a skrivelse under `doktyp=prop`, `subtyp=skr`); not kept, not a source. The document id **HD0376** was found through a full-text search on "skuldankare"; its `typrubrik` is "Regeringens skrivelse 2025/26:76".
- `prop_2025-26_76_HD0376.*` are the Riksdag's HTML and text renderings of **skr. 2025/26:76** (the file names carry the API's `doktyp`, not the document's kind). `skr_2025-26_76_Skrivelse_76_202526.pdf` is the printed skrivelse (`Skrivelse_76_202526.pdf`), the reference copy; its text layer was read to confirm every quoted sentence (its simple fonts carry no ToUnicode map, so the extractor's cp1250 fallback renders "å" as "ĺ"; the quotes in the extract use the HTML's UTF-8 text, which agrees word for word).
- `dokumentstatus_HD01FiU1.json` / `utskottsforslag_HD01FiU1.xml`: bet. 2025/26:FiU1 (Statens budget 2026 - Rambeslutet), the decision that fixed the balance target (point 2 a), decided 2025-11-26 (rskr. 2025/26:64 per the skrivelse).
- `dokumentstatus_HD01FiU14.json`: bet. 2025/26:FiU14, the committee report on skr. 2025/26:76 ("Riksdagen lägger skrivelse 2025/26:76 till handlingarna"), decided 2026-02-25.
- regeringen.se was not fetched; the Riksdag copy is the document as tabled.
