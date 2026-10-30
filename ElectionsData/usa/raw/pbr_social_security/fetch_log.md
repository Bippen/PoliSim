# fetch_log - ElectionsData/usa/raw/pbr_social_security/ (pbr, K293's filing: the USA's pension benefit ratio), 2026-10-29

Command: `curl -sS -m 180 -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36" -D <file>.headers.txt -o <file> <url>`;
bytes stored as received, each file's response headers beside it as `<file>.headers.txt`. Status is the final status after redirects. Digests in
`SHA256SUMS.txt`. The Internet Archive answered 429 (rate limit, a 620-byte notice) to most first attempts; the fetch waited 75 s between attempts
and kept only a 200 whose body is a zip (`PK`).

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-29T23:23:31Z | 200 | 109264 | wayback_20260921152701_cbo_62556-2026-Long-Term-Social-Security-Projections.xlsx | https://web.archive.org/web/20260921152701id_/https://www.cbo.gov/system/files/2026-09/62556-2026-Long-Term-Social-Security-Projections.xlsx |
| 2026-10-29T23:28:39Z | 200 | 36896 | wayback_20260919143154_cbo_62556-2026-Additional-Info.xlsx | https://web.archive.org/web/20260919143154id_/https://www.cbo.gov/system/files/2026-09/62556-2026-Additional-Info.xlsx |

Refused attempts before each 200 (all 429, 620 bytes): the projections workbook at 23:20:58Z and 23:22:14Z; the additional-information workbook at
23:24:49Z, 23:26:06Z and 23:27:22Z.

**What each is.**
- `wayback_20260921152701_cbo_62556-2026-Long-Term-Social-Security-Projections.xlsx` - CBO, *CBO's 2026 Long-Term Projections for Social Security*
  (17 September 2026, publication 62556; its Contents sheet: "This file presents data that supplement CBO's February 2026 publication The Long-Term
  Budget Outlook Data: 2026 to 2056"), the Internet Archive's capture of 21 September 2026 of cbo.gov's own file (`x-archive-orig-last-modified:
  Thu, 17 Sep 2026 17:54:51 GMT`). Read by pbr: sheet "3", **Table 3** "Social Security's Tax Revenues and Outlays, With Scheduled Benefits, Measured
  as a Percentage of Gross Domestic Product" (calendar years 1986-2100, actual to 2025; column C "Outlays": 2024 5.07, 2026 5.262, 2030 5.609, 2040
  5.828, 2050 5.670, 2070 6.378, 2100 7.103; the note: "Outlays consist of scheduled benefits and administrative costs"); sheet "2", **Table 2**
  "Projected Gross Domestic Product and Taxable Payroll" (trillions of dollars; GDP 2026 32.274, 2100 442.655).
- `wayback_20260919143154_cbo_62556-2026-Additional-Info.xlsx` - the same publication's "Projections Underlying Social Security Estimates", the
  Archive's capture of 19 September 2026 (`x-archive-orig-last-modified: Thu, 17 Sep 2026 17:56:48 GMT`). Read by pbr, calendar years 2026-2100:
  **1. Covered Workers** (total, thousands: 2026 186 300, 2100 180 900), **2. Covered and Taxable Earnings** (covered earnings, trillions: 2026
  13.576, 2100 185.089), **3. OASDI Beneficiaries** (total, thousands: 2026 72 200, 2100 113 800).

**What is derived from them** (pbr's prereg, `Reviews/2026-10-30_pbr_prereg.md`, Part 1): CBO publishes no benefit ratio; the USA's is the average
benefit (Table 3's outlays x Table 2's GDP / the beneficiaries) over the average covered wage (covered earnings / covered workers): 32.28 % in 2026,
31.61 % in 2040, 29.24 % in 2050, 27.87 % in 2070, 27.00 % in 2100.

**Where the links came from.** CBO's publication page `https://www.cbo.gov/publication/62556`, as the Archive kept it on 27 September 2026
(`https://web.archive.org/web/20260927195346id_/https://www.cbo.gov/publication/62556`), read at about 23:18Z (200, 37 902 bytes) for its two file
links (`/system/files/2026-09/62556-2026-Long-Term-Social-Security-Projections.xlsx`, `/system/files/2026-09/62556-2026-Additional-Info.xlsx`) and its
summary ("Each year, CBO publishes its projections of what revenues and outlays for the Social Security program would be for the next 75 years if
current laws generally remained unchanged ... They are consistent with the projections in The Long-Term Budget Outlook Data: 2026 to 2056").
The page itself is NOT kept: its re-fetch with headers was refused (429, 620 bytes) at eight attempts, 2026-10-29T23:29:56Z to 23:38:54Z; nothing
rests on it beyond the two links and the summary quoted here.
The captures' timestamps are the Archive's CDX index for each file (`https://web.archive.org/cdx/search/cdx?url=www.cbo.gov/system/files/2026-09/<file>`):
the projections workbook's earlier capture (2026-09-19T14:32:14Z) is indexed as status 900, type unk, 856 bytes - not the file; the 2026-09-21 capture
above is.

**BILLED.**
- cbo.gov itself - refused: `https://www.cbo.gov/publication/62556` answered 403 with a 767-byte JavaScript challenge (2026-10-29T23:1xZ), as K274's
  and K299's fetch logs record for cbo.gov. Not retried with another client (a bot check is not circumvented).
- The Social Security Trustees' report (its ratio of the average benefit to the average wage) - not sought: the item names CBO's Social Security path,
  and CBO's own five series give the ratio on the same projection as LTBO 2026's outlays.
