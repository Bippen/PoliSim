# fetch_log - ElectionsData/usa/raw/debtlimit_episodes/ (PS-6 K78, Elias's ruling of 2026-10-23: the AI hostile majority's withhold of the debt-limit raise, read on the record's episodes), 2026-10-23

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -H "Accept: text/html,application/xhtml+xml,application/xml,application/pdf,*/*" -o <file> <url>`; bytes stored as received. Status is the final status after redirects. Digests in `SHA256SUMS.txt`. The reading is `ElectionsData/usa/debtlimit_episodes.md`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-23T19:19:26Z | 200 | 585531 | secondary_everycrsreport_R43389.html [SECONDARY] | https://www.everycrsreport.com/reports/R43389.html |
| 2026-10-23T19:19:26Z | 200 | 103132 | govinfo_PLAW-112publ25.htm | https://www.govinfo.gov/content/pkg/PLAW-112publ25/html/PLAW-112publ25.htm |
| 2026-10-23T19:19:27Z | 200 | 5802 | govinfo_PLAW-113publ3.htm | https://www.govinfo.gov/content/pkg/PLAW-113publ3/html/PLAW-113publ3.htm |
| 2026-10-23T19:19:27Z | 200 | 48761 | govinfo_PLAW-113publ46.htm | https://www.govinfo.gov/content/pkg/PLAW-113publ46/html/PLAW-113publ46.htm |
| 2026-10-23T19:19:28Z | 200 | 1426 | govinfo_PLAW-117publ50.htm | https://www.govinfo.gov/content/pkg/PLAW-117publ50/html/PLAW-117publ50.htm |
| 2026-10-23T19:19:28Z | 200 | 132862 | govinfo_PLAW-118publ5.htm | https://www.govinfo.gov/content/pkg/PLAW-118publ5/html/PLAW-118publ5.htm |
| 2026-10-23T19:19:29Z | 200 | 94150 | clerk_roll_2011_379.xml | https://clerk.house.gov/evs/2011/roll379.xml |
| 2026-10-23T19:19:29Z | 200 | 94772 | clerk_roll_2023_199.xml | https://clerk.house.gov/evs/2023/roll199.xml |
| 2026-10-23T19:19:30Z | 200 | 73115 | [NOT KEPT] treasury_letter_2011-05-16_geithner.pdf | http://www.treasury.gov/connect/blog/Documents/20110516Letter%20to%20Congress.pdf |
| 2026-10-23T19:19:31Z | 429 | 620 | [NOT KEPT] wayback_treasury_letter_2011-05-16_geithner.pdf | https://web.archive.org/web/2011id_/http://www.treasury.gov/connect/blog/Documents/20110516Letter%20to%20Congress.pdf |
| 2026-10-23T19:19:32Z | 200 | 157148 | treasury_letter_2021-08-02_yellen.pdf | https://home.treasury.gov/system/files/136/Debt-Limit-Letter-Congress_20210802_Pelosi.pdf |
| 2026-10-23T19:20:11Z | 429 | 620 | [NOT KEPT] the same Wayback replay, retried | (as above) |

Notes
- **CRS R43389 ("The Debt Limit Since 2011") - the primary is BILLED.** `crsreports.congress.gov/product/pdf/R/R43389` answered 403 (the Cloudflare challenge recorded in `raw/debt_limit/fetch_log.md`), and `www.congress.gov/crs_external_products/R/PDF/R43389/R43389.<n>.pdf` answered 404 for versions 1 to 30 (that path serves IF10292 and R48832, not this report). The text is read from everycrsreport's rendering [SECONDARY], "Updated December 23, 2022"; it covers 2011, 2013 and 2021, not 2023.
- **Secretary Geithner's letter of 16 May 2011 - BILLED.** treasury.gov answered 200 with an HTML page (`Featured Stories | U.S. Department of the Treasury`), not the PDF - not kept; the Internet Archive's raw replay answered 429 twice. R43389 [SECONDARY] quotes it: "On May 16, 2011, U.S. Treasury Secretary Timothy Geithner announced that the federal debt had reached its statutory limit", and Treasury's 1 July 2011 confirmation that "its borrowing authority would be exhausted on August 2".
- **Secretary Lew's letter of 25 September 2013 - not fetched** (R43389 gives no URL for it); R43389 [SECONDARY]: "the U.S. Treasury would exhaust its borrowing capacity no later than October 17".
- **The 2023 episode** reads the letters already in tree, `raw/treasury_debt/treasury_letter_2023-01-19_yellen.pdf` and `treasury_letter_2023-05-26_yellen.pdf` (page images; their reading is `ElectionsData/usa/debt_limit.md`'s, quoted there), with P.L. 118-5 and the House's roll 199 here.
- `treasury_letter_2021-08-02_yellen.pdf` is a page image (no text layer): the 2021 dates are read from R43389 [SECONDARY], which cites it.
