# fetch_log - ElectionsData/usa/raw/salience/ (PS-6 US-30: the campaign staged on the states - "Gallup's salience on disk, or a wave dated before the campaign"), 2026-10-20

Command: `curl -sS -o <file> "<url>"` (no decompression asked for: the bytes stored are the bytes received). Digests in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-20T05:22:28Z | 200 | 2074 | gallup_mip_vEKjO_47_dataset.csv | https://datawrapper.dwcdn.net/vEKjO/47/dataset.csv |
| 2026-10-20T05:22:28Z | 200 | 28433 | wayback_gallup_mip_page_20240909060209.html.gz | https://web.archive.org/web/20240909060209id_/https://news.gallup.com/poll/1675/most-important-problem.aspx (the Internet Archive's raw replay; the body is gzip as archived - `gunzip -c` reads it) |

Probed, not kept:
- https://news.gallup.com/poll/1675/most-important-problem.aspx (direct, 200): today's page carries only the months from March 2026; the 2024 months are not on it.
- https://datawrapper.dwcdn.net/vEKjO/44/, /45/, /46/, /48/ `dataset.csv` (200 each): the chart's published versions around the campaign - 44 newest column June 2024, 45 July 2024, 46 August 2024 (byte for byte 47's), 48 September 2024.
- https://datawrapper.dwcdn.net/vEKjO/2/ (200): a redirect to the chart's current version (79 on the day).

Notes
- **Whose figures.** Gallup's "Most Important Problem" trend table (open-ended: "What do you think is the most important problem facing this country today?"), published on Gallup's trends page through a Datawrapper chart whose id is `vEKjO`. The archived page of 9 September 2024 embeds that chart (`datawrapper.dwcdn.net/vEKjO/2/`, which redirects to the chart's newest published version); version 47's dataset is the last published version whose newest column is August 2024 ("Aug-24"), so it is what that page showed before Gallup's September release.
- **The wave US-30 reads** is the newest column fielded before the 2024 campaign opened (10 September 2024): August 2024. The issues the model has a slot for among its top five: Immigration 19 %, and the economy 18 % ("Economy in general" 18, "High cost of living/Inflation" 15 - the larger stands where two items map to one slot, `StanceModel`'s convention). "The government/Poor leadership" (16) has no slot and is dropped, as in the July 2026 seed. `UsCampaignDiagnostic` re-reads this file against its digest and holds the staging's figures to it.
- The column carries a month, not fieldwork dates.
