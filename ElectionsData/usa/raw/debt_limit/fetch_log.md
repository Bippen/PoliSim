# fetch_log - ElectionsData/usa/raw/debt_limit/ (item B7, the statutory debt limit), 2026-10-02

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -H "Accept: text/html,application/xhtml+xml,application/pdf,*/*" -o <file> <url>`; bytes stored as received. Status is the final status after redirects. Digests in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-02T06:28:31Z | 200 | 1217741 | govinfo_PLAW-119publ21.htm | https://www.govinfo.gov/content/pkg/PLAW-119publ21/html/PLAW-119publ21.htm |
| 2026-10-02T06:28:33Z | 200 | 968588 | govinfo_PLAW-119publ21.pdf | https://www.govinfo.gov/content/pkg/PLAW-119publ21/pdf/PLAW-119publ21.pdf |
| 2026-10-02T06:28:34Z | 403 | 5859 | [NOT KEPT] congress_HR1_119_text.html | https://www.congress.gov/bill/119th-congress/house-bill/1/text |
| 2026-10-02T06:28:34Z | 200 | 14616 | [NOT KEPT] uscode_31_3101.html | https://uscode.house.gov/view.xhtml?req=granuleid:USC-prelim-title31-section3101&num=0&edition=prelim |
| 2026-10-02T06:28:34Z | 403 | 5762 | [NOT KEPT] congress_crs_IF10292.html | https://www.congress.gov/crs-product/IF10292 |
| 2026-10-02T06:28:35Z | 403 | 5821 | [NOT KEPT] crsreports_IF10292.pdf | https://crsreports.congress.gov/product/pdf/IF/IF10292 |
| 2026-10-02T06:30:35Z | 200 | 394155 | crs_IF10292.22.pdf | https://www.congress.gov/crs_external_products/IF/PDF/IF10292/IF10292.22.pdf |
| 2026-10-02T06:30:36Z | 200 | 256684 | crs_IF10292.21.pdf | https://www.congress.gov/crs_external_products/IF/PDF/IF10292/IF10292.21.pdf |
| 2026-10-02T06:30:36Z | 200 | 25814 | secondary_everycrsreport_IF10292.html [SECONDARY] | https://www.everycrsreport.com/reports/IF10292.html |
| 2026-10-02T06:30:37Z | 200 | 394155 | [NOT KEPT] secondary_everycrsreport_2026-09-21_IF10292.pdf | https://www.everycrsreport.com/files/2026-09-21_IF10292_e0ba09f69e47a21c569578c96d7765d008c55d05.pdf |
| 2026-10-02T06:31:04Z | 200 | 30641 | treasury_mspd_table_2_2024-12_to_2026-08.json | https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/debt/mspd/mspd_table_2?filter=record_date:gte:2024-12-31,debt_limit_class1_desc:in:(Statutory%20Debt%20Limit,Total%20Public%20Debt%20Subject%20to%20Limit,Balance%20of%20Statutory%20Debt%20Limit)&sort=record_date&page%5Bsize%5D=200 |
| 2026-10-02T06:31:14Z | 200 | 56279 | govinfo_USCODE-2024-title31-sec3101.htm | https://www.govinfo.gov/content/pkg/USCODE-2024-title31/html/USCODE-2024-title31-subtitleIII-chap31-subchapI-sec3101.htm |

Notes
- **congress.gov pages are behind a Cloudflare challenge** ("Just a moment...", HTTP 403) for the bill text, the CRS product page and crsreports.congress.gov; bodies deleted. The enacted text of H.R. 1 was read from **govinfo.gov** instead (the GPO's official Public Law 119-21, slip law with Statutes at Large pagination).
- **uscode.house.gov answered HTTP 200 with an "Under Maintenance" page** (14,616 bytes, `<title>Under Maintenance`) on two URL forms at 06:28 and ~06:32 UTC; not kept. 31 U.S.C. 3101 was read from the GPO's **United States Code, 2024 Edition** on govinfo.gov (official; its notes do not yet carry P.L. 119-21).
- The CRS PDF path `crs_external_products/IF/PDF/IF10292/IF10292.<n>.pdf` serves without the challenge on GET (HEAD answers 403). Versions 1-10 and 23-70 returned 404; 11-22 returned 200. **Version 22 is the 21 Sep 2026 update**: its page footer reads "IF10292 · VERSION 22 · UPDATED", page 1 "Updated September 21, 2026", and it is byte-identical (SHA-256 35cdcd16a1336080efda0eafec4be720caba4c90669cd92b5137d89a8af2df27) to EveryCRSReport's file dated 2026-09-21, which was therefore not kept. Version 21 is the previous edition (EveryCRSReport lists Dec. 5, 2025 before Sep. 21, 2026), kept for comparison.
- `secondary_everycrsreport_IF10292.html` [SECONDARY]: a third-party HTML rendering of the same CRS text plus the revision history; every quote in the extract is from the primary PDF.
- `treasury_mspd_table_2_...json`: Treasury Fiscal Data, Monthly Statement of the Public Debt table 2 (amounts in USD millions), month-ends 2024-12-31 to 2026-08-31 - the statutory limit as Treasury carries it.
