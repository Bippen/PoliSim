# fetch_log - EnergyData/raw/reliability/ (MF q26: the player's fleet orders meet the AI ministry's reliability check, "the grid operator's statute where one exists, e.g. EnWG §13b"), 2026-10-28

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -o <file> "<url>"`; bytes stored as received (the page is served ISO-8859-1). Digest in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-28, before 10:35:04Z (time not taken to the second) | 200 | 12102 | gesetze_im_internet_enwg_2005___13b.html | https://www.gesetze-im-internet.de/enwg_2005/__13b.html |

Notes
- **EnWG § 13b "Stilllegungen von Anlagen"** (the Bundesministerium der Justiz's consolidated text). What the game reads: Abs. 2 Satz 2 - "Eine Anlage ist systemrelevant, wenn ihre Stilllegung mit hinreichender Wahrscheinlichkeit zu einer nicht unerheblichen Gefährdung oder Störung der Sicherheit oder Zuverlässigkeit des Elektrizitätsversorgungssystems führen würde und diese Gefährdung oder Störung nicht durch andere angemessene Maßnahmen beseitigt werden kann"; Abs. 4 Satz 1 (a provisional closure is barred while the TSO designates the plant system-relevant) and Abs. 5 Satz 1 (a final closure of a plant of 50 MW or more is barred while the designation stands, the Bundesnetzagentur has approved it and continued operation is technically and legally possible). The game's reading (`EnergyReliability`, DECLARED): a retirement that would leave load unserved is a closure of system-relevant capacity, refused.
- **The other five countries**: no grid operator's statute was fetched in this pass; their refusal is the AI ministry's own security-of-supply test and the refusal's sentence says so. Sourcing each country's equivalent (a closure veto or authorisation in its own law) is owed.
