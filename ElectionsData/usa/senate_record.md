# The Senate by state and by date - the 118th and 119th Congresses (PS-6 US-12, part one)

**The record of `Tools/us_senate_prep.pl` (`COMPLETED.md` §792).** Every Senate seat of the 118th and 119th Congresses, its holders and the days they held it, read from the Senate's own pages - the base for US-12's roster diagnostic, for US-15's Senate as a chamber, and for R-US10's caucus. The race half of US-12 (the Class I returns of 2018 and 2024, R-US19's measurement) is part two; nothing here is a race.

**What the catalog holds** (generated, never typed; re-read by `GeneratedCatalogCheck.CheckUsSenate`, which is US-12's roster diagnostic):
- `senate_seats.csv` - a row a senator who held a seat on or after 3 Jan 2023: state, class, name, party (D, R, I as the state's page prints it; D/I a Democrat who became an independent, with the day), the day he took the seat (his oath for a senator new in the 118th or 119th Congress; for one seated before the window his state page's day, which can be an appointment's rather than an oath's), the day his service ended, how he came and went, and an independent's caucus with its source;
- `senate_changes.csv` - every seat change of the two Congresses to the record's reach, a Congress's opening apart (its retiring senators' term ends and its new senators' oaths on 3 Jan): resignations, deaths, an oath on another day, a party change;
- `senate_on.csv` - the Senate at the end of named days, by class and party and with the independents in their caucus: the 118th's opening, the US start (`WorldClock.StartDate`), each source's own day, the 118th's last day, the 119th's opening, and the record's reach;
- `senate_division.csv` - the Senate's own undated party-division line for each Congress (`[SEN-DIV]`), and each stretch of the Congress's days on which the roster gives it;
- `Assets/Scripts/Elections/Generated/UsSenateRecord.cs` - the same tables, a part of the partial class `UsPresidentialReturns`, each built by a method.

## The sources (`raw/senate/`, fetched 2026-10-06; `fetch_log.txt`, `SHA256SUMS.txt`, `sha256sum -c` clean)

senate.gov answers 403 to a direct fetch (Akamai), as `records_by_date.md`'s G2 records, so its pages are the Internet Archive's captures through its timegate (`id_`: the bytes as captured), each named by its capture time.

| pages | what the tool reads in it | captured |
|---|---|---|
| `wayback_senate_state_<XX>_<capture>.html`, the 50 "States in the Senate" pages | each seat's holders by class: name, party, the day each began and ended, how (comments, footnotes) - **the base** | 2026-02-10 to 2026-10-04, a state each |
| `wayback_senate_NewSenators_*.html` | every new senator of each Congress with the method of selection and **the date of swearing** | 2026-09-06 |
| `wayback_senate_AppointedSenators_*.html` | every appointment: appointed, sworn, subsequently elected | 2026-09-15 |
| `wayback_senate_SenatorsDiedinOffice_*.html` | every death in office, Congress and session, day | 2026-09-22 |
| `wayback_senate_SenatorsWhoChangedPartiesDuringSenateService_*.html` | every party change, its days and its words | 2026-10-04 |
| `wayback_senate_SenatorsRepresentingThirdorMinorParties_*.html` | every independent and his years | 2026-09-13 |
| `govinfo_cdir_2024-04-25.pdf`, `govinfo_cdir_2026-02-20.pdf` (and their MODS records), the Congressional Directory (GPO), by `pdftotext -raw` | the Senate's party count on the closing day (25 Apr 2024; 1 Oct 2025), the 2024 edition's class listings, each edition's table of changes | fetched direct from govinfo |
| `wayback_democrats_senate_gov_members_<capture>.html`, the Senate Democrats' own list of their members | the caucus (R-US10 (a)) | 2024-03-18 and 2026-09-05 |

From `raw/records/` (already saved): the three class pages (`[SEN-C1]`-`[SEN-C3]`, each with its own page date) and the party-division page (`[SEN-DIV]`).

**Logged and not kept:** `king.senate.gov` and `sanders.senate.gov` (403 directly; their captures read and discarded - neither senator's own page states a caucus). The tool reads no other source - not bioguide, not the unofficial congress-legislators data.

## Readings - each DECLARED, and named in the plan's US-12

1. **A seat is held from the oath.** The day a senator holds his seat is the New Senators page's date of swearing - not his appointment, not his term's first day. The state pages do not agree among themselves on what "term began" means: Ricketts', Helmy's and Darline Graham's rows give the appointment; Butler's, Husted's, Moody's and Justice's the oath; Schiff's and Kim's the appointment the day before their oath. Between a departure and the next oath the seat is vacant. Schiff and Kim, elected to the next term and then appointed to the old one's last weeks, are "Appointed & ... election" on the New Senators page and absent from the Appointed Senators page - DECLARED in the tool, every other appointee of the window held to both pages.
2. **The roster on a day is the Senate at that day's end.** A senator counts from his oath's day and not on his last day - a term ending at noon on 3 January, a resignation's effective day, a death. So the 119th's opening day already holds its new senators, and on 8 Dec 2024 California's and New Jersey's Class 1 seats are vacant (both appointees resigned that day; Schiff and Kim were sworn on the 9th).
3. **A senator-elect not yet sworn holds no seat.** Justice took his oath on 14 Jan 2025 (the New Senators page; `[SEN-DIV]`'s note says the same, and the tool holds the two equal), so West Virginia's Class 1 seat is vacant from 3 to 13 January. `[SEN-DIV]`'s 119th line counts him ("This total includes Senator-elect James Justice"): the named difference, dated in `senate_division.csv`.
4. **Party by the state's page, a change by the Changed Parties page.** Each declared change takes its day from the page's own words: Manchin from 5 Jun 2024 ("On June 5, 2024, he switched his affiliation to Independent"); Sinema from 3 Jan 2023 - the page's party line is "Independent, 2023-present", after her announcement of 9 Dec 2022, and its "Effective January 3, 2023, at the beginning of the 118th Congress" gives the day; it is the window's first day, so no earlier day would count differently. Every chapter of the page's contents is read: a senator of the window among them who is not a declared change stops the run.
5. **The caucus (R-US10 (a), "each counted with the conference it caucuses with, sourced and dated").** An independent caucuses with the Democrats where the Senate Democrats' own list carries him on its capture day - King and Sanders on both lists (2024-03-18 and 2026-09-05), Sinema on the 2024 list - and the caucus so read is applied over his whole service in the window (the lists are two days; nothing on the pages read dates a change of caucus). Manchin's caucus as an independent rests on the Changed Parties page's own words ("he continued to caucus with the Democrats"); the 2024 list carries him while still a Democrat. **Sinema is a reading put to Elias:** the Senate's page says that from 3 Jan 2023 she "received her seniority and committee assignments through the Democratic Conference for the purposes of organizing the Senate, but would not participate in either party caucus", while the Democrats listed her as a member in March 2024. The catalog counts her with the Democrats - the conference she organized through, the list that names her - and says so in her row's `caucus_by`, so the open reading travels with the catalog; the alternative is an independent in no caucus, which a two-unit chamber cannot seat (US-15's question).
6. **Which source decides a day.** The Senate's own pages; the Congressional Directory's change days are printed where they differ - Sasse's resignation 6 Jan 2023 in the Directory, 8 Jan on Nebraska's page; Rubio's 25 Jan 2025 in the Directory, 20 Jan on Florida's page (Moody was sworn on the 21st) - and so are its "chosen" days, which differ from the Appointed Senators page's appointment day in all four of its rows (Ricketts, Butler, Husted, Moody: printed beside each other in the run); none decides a seat, which is held from the oath (reading 1). Its oaths and its counts must agree with the roster, as they do.
7. **The record's reach is 2026-09-06**, the New Senators page's capture, and the tool applies it: a state page's row that began after it with no oath on that page is set aside, and a service ending after it is written as still serving - both printed (the run: nothing beyond it). The named days and `[SEN-DIV]`'s stretches end at it. A state's page captured before it is covered for appointments and deaths by the national lists (captured 2026-09-15 and 2026-09-22), not for a resignation left unfilled.
8. **The anchors, and the Directory's slips.** The roster equals each class page on that page's own date, senator for senator (Class I on 14 Jan 2025, Class II on 14 Jul 2026, Class III on 21 Jan 2025), and the Directory's Senate counts on its two closing days. The 2024 Directory's Class III listing omits Ted Budd (NC), whom its own totals count - DECLARED in the tool, so any other omission stops the run - and its bracketed Class III count ("Republicans, 20; Democrats, 14") is its listing's neither with nor without him.

## The run (2026-10-06, pasted)

```
Class I, its page's date 2025-01-14: 33 senators, the roster's the same
Class II, its page's date 2026-07-14: 33 senators, the roster's the same
Class III, its page's date 2025-01-21: 34 senators, the roster's the same
2023-01-03 the 118th Congress opens                                D 48 R 49 I 3 vacant 0 - with the caucus DEM 51 REP 49
2024-03-12 the US start (WorldClock.StartDate)                     D 48 R 49 I 3 vacant 0 - with the caucus DEM 51 REP 49
2024-03-18 the Senate Democrats' list captured                     D 48 R 49 I 3 vacant 0 - with the caucus DEM 51 REP 49
2024-04-25 the Congressional Directory of 2024 closes              D 48 R 49 I 3 vacant 0 - with the caucus DEM 51 REP 49
2025-01-02 the 118th Congress's last day                           D 47 R 49 I 4 vacant 0 - with the caucus DEM 51 REP 49
2025-01-03 the 119th Congress opens                                D 45 R 52 I 2 vacant 1 - with the caucus DEM 47 REP 52
2025-01-14 the Class I page's date                                 D 45 R 52 I 2 vacant 1 - with the caucus DEM 47 REP 52
2025-01-21 the Class III page's date                               D 45 R 53 I 2 vacant 0 - with the caucus DEM 47 REP 53
2025-10-01 the Congressional Directory of 2026 closes              D 45 R 53 I 2 vacant 0 - with the caucus DEM 47 REP 53
2026-07-14 the Class II page's date                                D 45 R 53 I 2 vacant 0 - with the caucus DEM 47 REP 53
2026-09-05 the Senate Democrats' list captured                     D 45 R 53 I 2 vacant 0 - with the caucus DEM 47 REP 53
2026-09-06 the record's reach (the New Senators page captured)     D 45 R 53 I 2 vacant 0 - with the caucus DEM 47 REP 53
[SEN-DIV] 118th: D 47 R 49 I 4 - holds 2024-06-05 to 2024-08-19, 2024-09-09 to 2024-12-07, 2024-12-09 to 2025-01-02
[SEN-DIV] 119th: D 45 R 53 I 2 - holds 2025-01-21 to 2026-03-22, 2026-03-24 to 2026-07-10, 2026-07-14 to 2026-09-06
change: 2023-01-08 NE 2 resigned Ben Sasse NE's page
change: 2023-01-23 NE 2 sworn John Peter "Pete" Ricketts appointed 2023-01-12; NE's page has 2023-01-12
change: 2023-09-29 CA 1 died Dianne Feinstein CA's page
change: 2023-10-03 CA 1 sworn Laphonza R. Butler appointed 2023-10-01
change: 2024-06-05 WV 1 independent Joe Manchin III the Changed Parties page
change: 2024-08-20 NJ 1 resigned Robert Menendez NJ's page
change: 2024-09-09 NJ 1 sworn George S. Helmy appointed 2024-08-23; NJ's page has 2024-08-23
change: 2024-12-08 CA 1 resigned Laphonza R. Butler CA's page
change: 2024-12-08 NJ 1 resigned George S. Helmy NJ's page
change: 2024-12-09 CA 1 sworn Adam B. Schiff appointed; CA's page has 2024-12-08
change: 2024-12-09 NJ 1 sworn Andy Kim appointed; NJ's page has 2024-12-08
change: 2025-01-10 OH 3 resigned J. D. Vance OH's page
change: 2025-01-14 WV 1 sworn James C. Justice general election
change: 2025-01-20 FL 3 resigned Marco Rubio FL's page
change: 2025-01-21 FL 3 sworn Ashley Moody appointed 2025-01-21
change: 2025-01-21 OH 3 sworn Jon Husted appointed 2025-01-18
change: 2026-03-23 OK 2 resigned Markwayne Mullin OK's page
change: 2026-03-24 OK 2 sworn Alan Armstrong appointed 2026-03-24
change: 2026-07-11 SC 2 died Lindsey Graham SC's page
change: 2026-07-14 SC 2 sworn Darline Graham appointed 2026-07-13; SC's page has 2026-07-13
the Directory's table: NE Ben Sasse: Resigned 2023-01-06 by the Directory, 2023-01-08 by NE's page; Pete Ricketts chosen 2023-01-08 by the Directory, 2023-01-12 by the Appointed Senators page, sworn 2023-01-23
the Directory's table: CA Dianne Feinstein: Died 2023-09-29 by the Directory, 2023-09-29 by CA's page; Laphonza Butler chosen 2023-10-02 by the Directory, 2023-10-01 by the Appointed Senators page, sworn 2023-10-03
the Directory's table: OH James David ``JD'' Vance.: Resigned 2025-01-10 by the Directory, 2025-01-10 by OH's page; Jon Husted chosen 2025-01-17 by the Directory, 2025-01-18 by the Appointed Senators page, sworn 2025-01-21
the Directory's table: FL Marco Rubio: Resigned 2025-01-25 by the Directory, 2025-01-20 by FL's page; Ashley Moody chosen 2025-01-16 by the Directory, 2025-01-21 by the Appointed Senators page, sworn 2025-01-21
the Directory's slip: class 3's listing omits NC budd (DECLARED)
the Directory's slip: class 3's bracket says Republicans, 20; Democrats, 14, its own listing D 15 R 18 I 0
the record's reach 2026-09-06: nothing beyond it on the pages read
caucus: AZ Kyrsten Sinema (D/I) with the Democrats, by the Senate Democrats' list of 2024-03-18 - a reading put to Elias (R-US10): the Senate's page says she "would not participate in either party caucus"
caucus: ME Angus S. King Jr. (I) with the Democrats, by the Senate Democrats' list of 2024-03-18 and 2026-09-05
caucus: VT Bernard Sanders (I) with the Democrats, by the Senate Democrats' list of 2024-03-18 and 2026-09-05
caucus: WV Joe Manchin III (D/I) with the Democrats, by the Changed Parties page ("he continued to caucus with the Democrats")
wrote ElectionsData/usa/senate_seats.csv (119 senators), senate_changes.csv (20), senate_on.csv (12 days), senate_division.csv, Assets/Scripts/Elections/Generated/UsSenateRecord.cs
```

**What the run says.** On the US start, 12 Mar 2024, the Senate was 48 Democrats, 49 Republicans and 3 independents - 51 with the Democrats - not `[SEN-DIV]`'s undated 47 / 49 / 4 for the 118th, which holds only from Manchin's change on 5 Jun 2024 (and not while New Jersey's seat stood vacant, 20 Aug - 8 Sep, nor on 8 Dec). On the 119th's opening, 3 Jan 2025, 45 / 52 / 2 with West Virginia's seat vacant; `[SEN-DIV]`'s 45 / 53 / 2 holds from 21 Jan 2025, when Ohio's and Florida's appointees were sworn, until a resignation or a death leaves a seat vacant (Oklahoma 23 Mar 2026; South Carolina 11-13 Jul 2026).

## The checks

**The tool** refuses, writing nothing, on every disagreement its header names - a page off its digest; a state page's row without class, name, party or day, overlapping holders, an undeclared label, a footnote without its note, a class whose last holder has ended; a new senator matching no row, a row of the window with no oath; an appointment's oath off the New Senators page's, or an appointment after it, or an appointee by the New Senators page the Appointed Senators page does not list (the two declared apart); a death off its state's day, or a "Died" row the Died page does not list; a party change undeclared or a chapter of the Changed Parties contents unread; an independent off the Third or Minor Parties page, or one it names who is not an independent here on his span's first and last days, or whose service does not end in its last year; a Democrats' list not exactly the roster's Democrats and the independents it names, or an independent with no caucus source; the class pages, the Directory's counts and the day they are as of, its class listings (the declared omission apart), its table read short or a reason in it other than a resignation or a death, or its oaths off the roster; `[SEN-DIV]` missing a line, a line holding on no day, or its note's delayed oath or resignation off the roster's day; a perl warning; a non-ASCII byte, or a CSV value with a comma.

**`Tools/us_senate_mutations.sh`** proves refusals on a copy, as `us_house_mutations.sh` does for US-11. On 2026-10-06 it caught all thirty-nine cases, each printing its exact mismatch, and the control reproduced the repository's bytes: four the pages and their sums (a byte changed, a page out of its sums by pattern and by name, a state's page missing); five the state pages (an undeclared label, overlapping holders, a last holder ended, a footnote without its note, a row without its party); four the oaths (a name matching no row, a row with no oath, Justice's oath against `[SEN-DIV]`'s note, a party disagreement); five the appointments and deaths (an oath off, an appointment after its oath, an appointee missing from the Appointed page, a death's day off, a death unlisted); five the party changes and independents (Manchin's words, Sinema's change undeclared, an independent unlisted, a Democrat named an independent, a span ended early); two the caucus; eight the anchors (a class page's party, the Directory's count, its count's day, its oath, its listing, the Budd omission undeclared, its closing day, an unknown reason in its table); three `[SEN-DIV]` (a line holding on no day, the note's resignation, a line missing); three the copy's own tool (a perl warning, a non-ASCII byte, a comma in a name). Refusals with no case: two pages for one state, a state holding a class count other than two, the classes' 33 / 33 / 34, a New Senators entry matching two holders or sworn twice, the Directory's table read short, the record's reach off the New Senators capture, a chapter of the Changed Parties contents unread.

**`GeneratedCatalogCheck.CheckUsSenate`** (the cheap bar) - US-12's roster diagnostic: every page re-hashed; every row of the four CSVs the catalog's; each seat's holders in order - each took his seat before he left it and left it no later than the next took his, only the last still serving - and never two on one day; the roster derived again from the seat rows alone on every named day - 100 seats, each state in two classes, the classes 33, 33 and 34 states, the counts by class and party and with the caucus the generated rows'; the US start among the days, the day `WorldClock.StartDate` gives; `[SEN-DIV]`'s stretches derived again over every day of the 118th and 119th Congresses to the record's reach (the day named so), the rows those stretches one for one.

## Not reached

- **The race half** - the Class I returns of 2018 and 2024 and the 2024 specials, and R-US19's measurement - is US-12's part two, its rule's details declared before it runs, as R-US18's were (§791).
- **The 2026 specials** (`records_by_date.md` G6): the four seats held by appointment at the record's reach - Ohio's and Florida's Class 3 (Husted, Moody; their terms run to 2029, so a special would fill the rest of the term) and Oklahoma's and South Carolina's Class 2 (Armstrong, Darline Graham; the class is up at the regular election of 3 Nov 2026 for the next term). Whether and when each state holds a special stays BILLED.
- **Changes after the record's reach** (2026-09-06), and a resignation left unfilled after a state's own capture.
