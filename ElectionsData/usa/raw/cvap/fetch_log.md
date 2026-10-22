# fetch_log - ElectionsData/usa/raw/cvap/ (PS-6 US-30: a state's eligible electorate - "EAC's EAVS 2024 or the Census Bureau's citizen voting-age population"), 2026-10-20

Command: `curl -sS -o <file> "<url>"`; bytes stored as received. The archive is kept OUT OF TREE (59 MB; its digest and size in `../out_of_tree.txt`), and the three members US-30 reads are extracted from it byte for byte (`unzip -t` passed their CRC-32s) into this folder. Digests in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-20T05:19:03Z | 200 | 59056628 | ../../../../PoliSim-captures/sources/usa_us30/cvap/CVAP_2020-2024_ACS_csv_files.zip (out of tree) | https://www2.census.gov/programs-surveys/decennial/rdo/datasets/2024/2024-cvap/CVAP_2020-2024_ACS_csv_files.zip |

Members extracted (name, length, CRC-32 as the archive lists them):

| member | bytes | CRC-32 |
|---|---|---|
| State.csv | 65415 | 29f03954 |
| Nation.csv | 1559 | c02d6291 |
| CVAP_2020-2024_ACS_documentation_v1.pdf | 194761 | 4abc5706 |

Probed, not kept:
- https://api.census.gov/data/2024/acs/acs1?get=NAME,B29001_001E,B29001_001M&for=state:* answered 302 to `missing_key.html` (the API now asks for a key): not used.
- EAC's EAVS 2024 was not probed: the Census Bureau's file answers the plan's second named source.

Notes
- **Whose figures.** The Census Bureau's Citizen Voting Age Population by Race and Ethnicity special tabulation from the 2020-2024 American Community Survey 5-year estimates (the archive's own documentation is kept beside the tables). `State.csv`'s "Total" line per state (`lntitle` Total, `lnnumber` 1); `cvap_est` the citizen voting-age population, `cvap_moe` its margin of error. Puerto Rico is in the file and is not one of the 51 jurisdictions.
- **The mapping to the returns catalog's postal codes** goes through the University of Florida Election Lab's file already on disk (`../returns/ufl_turnout_2024G_v0.4.csv`, columns STATE and STATE_ABV), never typed.
- **The vintage.** The five-year window ending in the election's year (2020-2024) for the 2024 campaign; the newest on disk for a later one (DECLARED in `UsRegions`).
