# USA — General election 2024 returns + rules [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; overnight 2026-08-28→29, research agent; FEC official results
files, the Clerk of the House's certified statistics, NARA Electoral College records; UF
Election Lab turnout named as the VEP estimator). `[PROVISIONAL]` until re-verified (R-K9).
Granularity call (logged in the morning report): BOTH races — the presidential result with the
state table as the regional spine, and the House national totals + seats. Absolute counts
carried throughout.

## USA — General election, 2024-11-05
### Source register
- returns: https://www.fec.gov/documents/5644/2024presgeresults.pdf and https://www.fec.gov/documents/5645/2024presgeresults.xlsx (Federal Election Commission, "Official 2024 Presidential General Election Results", compiled Jan 16, 2025 — popular vote from State Elections Offices, electoral vote from State Certificates of Vote at NARA; accessed 2026-08-28, basis: official); https://clerk.house.gov/member_info/electionInfo/2024/statistics2024.pdf (Clerk of the U.S. House of Representatives, "Statistics of the Presidential and Congressional Election of November 5, 2024"; accessed 2026-08-28, basis: official/certified); https://www.archives.gov/electoral-college/2024 (U.S. National Archives, Electoral College results; accessed 2026-08-28, basis: official)
- rules: https://www.archives.gov/electoral-college/allocation (NARA); https://www.govinfo.gov/content/pkg/USCODE-2022-title2/html/USCODE-2022-title2-chap1-sec2c.htm (2 U.S.C. §2c); https://www.census.gov/topics/public-sector/congressional-apportionment/about.html (435 seats); https://georgia.gov/vote-runoff-elections; https://www.sos.ca.gov/elections/primary-elections-california; https://www.sos.wa.gov/elections/voters/helpful-information/top-two-primary-faqs-voters; https://www.maine.gov/sos/cec/elec/upcoming/rcv.html; https://www.elections.alaska.gov/RCV.php
- turnout: https://election.lab.ufl.edu/data-downloads/turnoutdata/Turnout_2024G_v0.4.csv (University of Florida Election Lab, v0.4)

### National result — President
| candidate (party) | popular vote share % | electoral votes |
|---|---|---|
| Donald J. Trump (R) | 49.80 (77,302,580) | 312 |
| Kamala D. Harris (D) | 48.32 (75,017,613) | 226 |
| Jill Stein (Green) | 0.56 (862,049) | 0 |

Total ballots (presidential): 155,238,302 (FEC). Turnout: 64.3% of voting-eligible population (University of Florida Election Lab VEP estimate 243,803,423; total ballots counted 156,766,239).

### National result — House
| party | national vote share % | seats |
|---|---|---|
| Republican | 49.75 (74,390,864) | 220 |
| Democratic | 47.19 (70,571,330) | 215 |

Total House votes 149,543,421; seats per the Clerk's "Political Divisions… 119th Congress" table (immediate election results), 435 total. Source: Clerk of the House statistics2024.pdf.

### Regional table — President by state
Source: FEC Official 2024 Presidential General Election Results (2024presgeresults.xlsx); shares computed as winner's votes / state total votes from that file; electoral votes per FEC/NARA.

| state | EV | winner | winner share % |
|---|---|---|---|
| PA | 19 | Trump (R) | 50.20 |
| GA | 16 | Trump (R) | 50.72 |
| MI | 15 | Trump (R) | 49.73 |
| WI | 10 | Trump (R) | 49.60 |
| AZ | 11 | Trump (R) | 52.22 |
| NV | 6 | Trump (R) | 50.59 |
| NC | 16 | Trump (R) | 50.86 |
| CA | 54 | Harris (D) | 58.47 |
| TX | 40 | Trump (R) | 56.14 |
| NY | 28 | Harris (D) | 55.91 |
| FL | 30 | Trump (R) | 56.09 |
| OH | 17 | Trump (R) | 55.14 |

Split states: ME 4 EV — Harris 3 (statewide + CD-1), Trump 1 (CD-2); NE 5 EV — Trump 4 (statewide + CD-1, CD-3), Harris 1 (CD-2) (archives.gov/electoral-college/2024).

### Electoral rules
- Electoral College: 538 electors total, 270 needed to win ("Total Electoral Votes: 538; Majority Needed to Elect: 270") — https://www.archives.gov/electoral-college/allocation (also stated in the FEC results file: "Total Electoral Vote = 538. Total Electoral Vote Needed to Win = 270").
- Winner-take-all: "All States, except for Maine and Nebraska, have a winner-take-all policy"; ME and NE appoint electors by congressional-district winner plus 2 at-large for the statewide winner — https://www.archives.gov/electoral-college/allocation.
- House: 435 seats apportioned among the states — https://www.census.gov/topics/public-sector/congressional-apportionment/about.html; single-member districts ("no district to elect more than one Representative") — 2 U.S.C. §2c, https://www.govinfo.gov/content/pkg/USCODE-2022-title2/html/USCODE-2022-title2-chap1-sec2c.htm; plurality decides in most states.
- Exception — Georgia: majority required; "Runoff elections are held when no candidate wins the required majority of votes… The top 2 vote-getters will face each other in a runoff" — https://georgia.gov/vote-runoff-elections.
- Exception — CA top-two: "only the top two vote-getters in the primary election – regardless of party preference – move on to the general election" (congressional offices are voter-nominated) — https://www.sos.ca.gov/elections/primary-elections-california; WA uses the same top-two system — https://www.sos.wa.gov/elections/voters/helpful-information/top-two-primary-faqs-voters.
- Exception — ranked choice: ME uses RCV in federal general elections — https://www.maine.gov/sos/cec/elec/upcoming/rcv.html; AK uses a top-four open primary with RCV general (majority 50%+1 via elimination rounds, applies to U.S. President/Senate/House) — https://www.elections.alaska.gov/RCV.php.

### Caveats
- The FEC's full biennial "Federal Elections 2024" publication is not yet posted (the 2020 edition exists at fec.gov/resources/cms-content/documents/federalelections2020.pdf; the 2024 path returns 403), so House national totals/seats come from the Clerk of the House's certified statistics instead — also a primary official source.
- FEC gives national presidential percentages itself (Trump 49.7961%, Harris 48.3242%, Stein 0.5553%); state winner shares and House percentages were computed from the official vote totals in the same files (not independently published as percentages).
- Stein's party label (Green) is not printed in the FEC results grid itself (candidates listed by surname only); label per common certification — [UNCONFIRMED as a label, votes confirmed]. Kennedy (756,393; 0.49%) and Oliver (650,126; 0.42%) trail Stein.
- WA and AK rules pages blocked direct fetches (403/405); their quoted content came via search-engine extracts of the official sos.wa.gov and elections.alaska.gov pages — mark [UNCONFIRMED-direct-fetch], secondary: Bing/Google snippets of those official pages.
- Turnout is the UF Election Lab v0.4 estimate (updated May 12, 2026, marked as subject to revision); 64.30% = total ballots (156,766,239) / VEP (243,803,423). An earlier v0.1 figure of ~64.07% circulates in secondary sources.
- Nevada ballots include 19,625 "None of these candidates"; House seat figures are "immediate results of elections" (subsequent vacancies not reflected). No 2024 Georgia congressional general race went to a runoff.

*(Filed verbatim from the research agent's return, 2026-08-28 night.)*

## Raw pages (`COMPLETED.md` §786, PS-6 US-3)

Since §786 every page this file cites is saved byte for byte — under `raw/` and registered (publisher, fetch time, basis, SHA-256) in `president_returns.md` (the 2024 NARA page in `records_by_date.md`), or, for the FEC's 2020 volume, kept out of tree by SHA-256 and bytes in `raw/out_of_tree.txt` — but for the one host that refused:

| Cited above | Saved as |
|---|---|
| fec.gov/documents/5644/2024presgeresults.pdf, fec.gov/documents/5645/2024presgeresults.xlsx | `raw/returns/fec_2024presgeresults.pdf`, `raw/returns/fec_2024presgeresults.xlsx` — saved on 2026-10-05 by requesting the cited URLs, which answered 302 to `fec.gov/resources/cms-content/documents/`: `raw/returns/fetch_log.txt` records the URL as reached, with a NOTE line on the redirect |
| clerk.house.gov/member_info/electionInfo/2024/statistics2024.pdf | `raw/returns/clerk_statistics2024.pdf` |
| archives.gov/electoral-college/2024 | `raw/executive/archives_electoral_college_2024.html` |
| archives.gov/electoral-college/allocation | `raw/returns/archives_electoral_college_allocation.html` |
| govinfo.gov, 2 U.S.C. §2c (2022 edition) | `raw/apportionment/govinfo_uscode_2_2c.html` — the 2023 edition; the section's only source is Pub. L. 90-196 (1967), and the quoted words are on it |
| census.gov/topics/public-sector/congressional-apportionment/about.html | `raw/apportionment/census_congressional_apportionment_about.html` |
| georgia.gov/vote-runoff-elections | `raw/rules/georgia_vote_runoff_elections.html` |
| sos.ca.gov/elections/primary-elections-california | `raw/rules/ca_sos_primary_elections.html` |
| sos.wa.gov/elections/voters/helpful-information/top-two-primary-faqs-voters | `raw/rules/wa_sos_top_two_primary_faqs.html` — fetched directly on 2026-10-05, so the caveat's search-engine route is no longer this rule's source |
| maine.gov/sos/cec/elec/upcoming/rcv.html | `raw/district/maine_sos_rcv.html` |
| elections.alaska.gov/RCV.php | **NOT SAVED** — HTTP 405 on 2026-10-05, as in August: BILLED; the Alaska rule stays `[UNCONFIRMED-direct-fetch]` |
| election.lab.ufl.edu/data-downloads/turnoutdata/Turnout_2024G_v0.4.csv | `raw/returns/ufl_turnout_2024G_v0.4.csv` |
| fec.gov/resources/cms-content/documents/federalelections2020.pdf | kept out of tree (`raw/out_of_tree.txt`; fetched 2026-10-05 from this URL, HTTP 200): its tables are `raw/returns/fec_federalelections2020.xlsx`, which the FEC's 2020 page (`raw/returns/fec_federal_elections_2020.html`) links beside it. The caveat's "the 2024 path returns 403" was not re-fetched |

**Read back against the saved pages on 2026-10-05 — every figure agrees:** the national presidential votes and the FEC's own percentages; Stein, Kennedy and Oliver; all twelve rows of the state table (electors and winner shares); the split states; Nevada's "None of these candidates"; the House totals and total vote (the Clerk's statistics, its "Total" line); the turnout, VEP and ballots counted (the UF file's United States row). Every quoted rule is on its saved page in the quotation's words; the typography differs in three places: NARA's allocation page prints "538\*" and "270\*", footnote marks the quotation drops; California's page closes its dash pair with a spaced hyphen ("regardless of party preference - move on") where the quotation prints a second en dash; the FEC's workbook has two spaces after "538." where the quotation has one. No tool reads this file: its figures rest on the read-back above. `president_by_state.csv`'s 2024 rows carry the same jurisdictions' votes and electors - each one's two nominees, all others together, its total, its electors and its electoral votes as cast; the national figures are their sums - and `Tools/us_returns_prep.pl` holds those rows to the saved FEC workbook and NARA's page. The winner shares, the FEC's percentages, Stein, Kennedy and Oliver are in no CSV.

**Stein's label, settled from the saved PDF.** The FEC's PDF (pp. 7–9) prints each candidate's ballot labels with a key: Jill Stein ran under eleven — BP, GPF, GR, GRE, IND, KYP, MTP, OTH, PET, PG, WG — "GRE = Green" among them (with Green-Rainbow, the Green Party of Florida, Pacific Green, Independent). "Green" in the table above is one of her labels, the single label a simplification; the `[UNCONFIRMED as a label]` caveat is answered.
