# fetch_log - ElectionsData/usa/raw/unsigned/ (PS-6 US-21c, Elias's ruling J6 (ii): a law of record that became law unsigned, cited for the Docket's law date), 2026-10-20

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -H "Accept: text/html,application/xhtml+xml,application/xml,application/pdf,*/*" -o <file> <url>`; bytes stored as received. Status is the final status after redirects. Digests in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-19T23:55:36Z | 200 | 397098 | govinfo_BILLSTATUS-119hr6644.xml | https://www.govinfo.gov/bulkdata/BILLSTATUS/119/hr/BILLSTATUS-119hr6644.xml |
| 2026-10-19T23:55:50Z | 200 | 525352 | govinfo_PLAW-119publ101.htm | https://www.govinfo.gov/content/pkg/PLAW-119publ101/html/PLAW-119publ101.htm |

Notes
- **The law of record: H.R. 6644, the 21st Century ROAD to Housing Act, P.L. 119-101.** Its slip law closes with the Office of the Federal Register's note: "The foregoing Act, having been presented to the President of the United States on Monday, June 29, 2026, and not having been returned by him to the House of Congress in which it originated within the time prescribed by the Constitution of the United States, has become law without his signature on July 11, 2026." Its BILLSTATUS dates the same two actions: "Presented to President." on 2026-06-29 and "Became Public Law No: 119-101." on 2026-07-11.
- **Against the ten-day rule** (Art. I §7 cl. 2, counted as `PresidentialDocket` counts it): from the day after presentment, Tuesday 30 June is day 1; Saturday 4 July day 5; Sunday 5 July not counted; Friday 10 July day 10, the last day to answer; law on Saturday 11 July, the day after the tenth counted day. The record's date and the rule's agree. `UsDocketDiagnostic` (b) holds the arithmetic against both pages.
- Found by a web search for laws that became law without signature (news reports of July 2026); the record above is the primary one, the reports are not relied on.
