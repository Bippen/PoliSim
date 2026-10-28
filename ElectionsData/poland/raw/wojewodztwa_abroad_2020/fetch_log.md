# fetch_log - ElectionsData/poland/raw/wojewodztwa_abroad_2020/ (PL-C review M5: the 2020 presidential first round's voters abroad and on ships), 2026-10-28

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -o <file> <url>`; bytes stored as received (renamed with the folder's `kbw_2020_1_` prefix, as `../president/` names its KBW zips). Digest in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-28T21:15Z | 200 | 30506 | kbw_2020_1_wyniki_gl_na_kand_po_powiatach_csv.zip | https://danewyborcze.kbw.gov.pl/dane/2020/prezydent/1/wyniki_gl_na_kand_po_powiatach_csv.zip |

What it holds: the first round of 28 Jun 2020 per powiat (member `wyniki_gl_na_kand_po_powiatach_utf8.csv`, `;`-separated). Two of its rows are not powiaty - "zagranica" (the voters abroad, TERYT 149900) and "statki" (the voters on Polish sea-going ships, 149800) - and both carry "Województwo" = "mazowieckie": the KBW's 2020 voivodeship file (`../president/kbw_2020_1_wyniki_gl_na_kand_po_wojewodztwach_csv.zip`) counts them inside mazowieckie, which is why its 16 rows sum to the national notice. `Tools/wojewodztwa_prep.pl` subtracts these two rows from mazowieckie's 2020 electorate and valid votes, so both vintages of `PolishWojewodztwa` hold the voters of the 16 wojewodztwa alone (2025's files already carry the abroad and ship units as their own rows). Not fetched: the gmina file of the same round (fetched to the scratchpad and discarded - the powiat file carries the two rows).
