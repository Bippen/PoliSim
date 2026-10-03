# fetch_log - ElectionsData/poland/raw/td_list_2023/ (Elias's ruling D2, the TD list's seats by party), 2026-10-03

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -o kandydaci_sejm_csv.zip https://danewyborcze.kbw.gov.pl/dane/2023/sejmsenat/kandydaci_sejm_csv.zip` - HTTP 200, 251,223 bytes, stored as received; `kandydaci_sejm_utf8.csv` is the archive's one member, unzipped unchanged (the archive's own timestamp: 17 Oct 2023).

The same KBW data service `district_votes_2023.csv` was read from (its header names wyniki_gl_na_listy_po_okregach_sejm_csv.zip). Columns used: "Nazwa komitetu", "Przynależność do partii", "Czy przyznano mandat"; the file's 460 "Tak" rows sum by committee to the PKW's 2023 seats (PiS 194, KO 157, TD 65, NL 26, Konf 18).
