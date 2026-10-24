# fetch_log - ElectionsData/usa/raw/salience/k102/ (K102, Elias 2026-10-24: Gallup's "most important problem" for 2015, 2020 and 2022, to test the USA's salience conversion out of sample), 2026-10-24

Command: `curl -sS -o <file> "<url>"` (no decompression asked for: the bytes stored are the bytes received). Each file was fetched twice and the two copies were byte-identical. Digests in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-24T11:12:49Z | 200 | 1746 | gallup_mip_vEKjO_1_dataset.csv | https://datawrapper.dwcdn.net/vEKjO/1/dataset.csv |
| 2026-10-24T11:12:51Z | 200 | 120886 | wayback_gallup_mip_page_20220425021115.html | https://web.archive.org/web/20220425021115id_/https://news.gallup.com/poll/1675/most-important-problem.aspx |
| 2026-10-24T11:12:53Z | 200 | 141351 | wayback_gallup_mip_page_20201028085700.html | https://web.archive.org/web/20201028085700id_/https://news.gallup.com/poll/1675/most-important-problem.aspx |
| 2026-10-24T11:12:55Z | 200 | 69085 | wayback_gallup_mip_page_20150915083712.html | https://web.archive.org/web/20150915083712id_/http://www.gallup.com/poll/1675/most-important-problem.aspx |

Probed, not kept: the Internet Archive's CDX index for both page URLs (2015-2023), and the Datawrapper chart vEKjO's versions 3, 5, 10 and 20 (newest columns Sep 2022, Sep 2022, Oct 2022 and Apr 2023).

Notes
- **Same source family as `../gallup_mip_vEKjO_47_dataset.csv`**: Gallup's trends page for the question (open-ended, "What do you think is the most important problem facing this country today?"), either as Datawrapper chart vEKjO's published dataset (version 1 is its first, newest column August 2022) or as the page's own HTML table, replayed raw from the Internet Archive.
- **2020**: the page of 28 October 2020 tables Feb, Apr, May, Jun, Jul, Aug and Sep 2020. March 2020 is not on it. Its February column reads "Coronavirus/Diseases" 13 %, the page's own figure, read as printed.
- **2022**: the page of 25 April 2022 tables the months to March 2022 (its February 2022 column was fielded 1-17 February, before 24 February). Version 1 of the chart tables April to August 2022.
- **2015 is BILLED**: the trends page of the time (this replay of 15 September 2015, and the end-of-year replay probed) prints the monthly table as an image (`content.gallup.com/.../dpoami2fxuyrpptcgcnzjw.png`, alt "Trend: What do you think is the most important problem facing this country today? (open-ended)"). No text table is stored, and figures are never read off an image. The page is kept as the evidence.
