# USA — the chambers, the executive and the party leaders of record by date, 2022-07-01 → 2026-09-24 [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; PS-1 world-clock sourcing pass 2026-09-24, sourcing agent; `docs/specs/POLITICAL_SYSTEM_SPEC.md` §9 stage 1, §11). `[PROVISIONAL]` until a second session re-verifies (R-K9).

**What this file is for.** The game's one world clock seats every country in its chamber of record and its government of record on the chosen start date (spec §3). For the United States this file gives, dated, the House and Senate by Congress, the president and vice president, the party chairs and the congressional leaders, and the constitutional rules the spec's §6 and §11 name. It does not allocate seats or name a start date; it dates the record.

**Fetched, never recalled.** Every quote below was read in a page fetched on 2026-09-24 (16:11–16:36 UTC; `raw/records/fetch_log.txt` carries each URL, file, time and HTTP status) and saved byte for byte under `raw/records/`. Each file's SHA-256 is in `raw/records/SHA256SUMS.txt` and in the register below. Quotes are English and verbatim. They were checked against the saved bytes by a script (tags stripped, script blocks kept because the rules page embeds Rule XXII in one, entities decoded, whitespace collapsed): every block quote in this file was found in the saved file it is attributed to. Lines marked **DERIVED** state their arithmetic or reading. Where a page could not be fetched, the item is a **GAP**, never an estimate.

**The senate.gov detour.** `www.senate.gov`, `www.rules.senate.gov`, `constitution.congress.gov`, `www.congress.gov`, `crsreports.congress.gov` and `gop.com` answered every fetch from this machine with an Akamai "Access Denied" 403 (over IPv4; IPv6 unreachable), whatever the User-Agent. The senate.gov pages were therefore taken as the Internet Archive's captures of those pages (`web.archive.org/web/<timestamp>id_/<url>`, which serves the captured bytes without the archive's toolbar); each register row names the capture timestamp, and the pages are the Senate's own text as captured, not the live page on 2026-09-24. The Constitution's text is NARA's transcript (archives.gov) instead of constitution.congress.gov; the reconciliation statute is the U.S. Code on govinfo.gov (GPO) instead of congress.gov. `gop.com` is not served by the Wayback Machine either (403 stubs), so the RNC chair is a GAP (G1).

**Party keys** are the project's (`Assets/Scripts/Data/PartySystem.cs`, the USA block): `REP` Republican Party, `DEM` Democratic Party. Independents are written `I`.

## 1. THE CHAMBERS OF RECORD BY DATE

### 1a. The House of Representatives by Congress

| from | to | Congress | elected | House party division at the opening (history.house.gov, "based on election day results") | source ids |
|---|---|---|---|---|---|
| (before the window) | 2023-01-03 | 117th (2021–2023) | 2020-11-03 | 435 seats: **222 DEM, 212 REP**, 0 other; 5 delegates / 1 resident commissioner | [HH-DIV] [HH-117] [SEN-DATES] |
| 2023-01-03 | 2025-01-03 | 118th (2023–2025) | 2022-11-08 | 435 seats: **213 DEM, 222 REP**, 0 other; 5/1 | [HH-DIV] [HH-118] [SEN-DATES] |
| 2025-01-03 | (open; the 120th convenes 2027-01-03) | 119th (2025–2027) | 2024-11-05 | 435 seats: **215 DEM, 220 REP**, 0 other; 5/1. The Clerk's live count on 2026-09-24: 218 REP, 214 DEM, 1 I, 2 vacancies | [HH-DIV] [CLK-R1] [SEN-DATES] |

**The dates.** The Senate's table of sessions [SEN-DATES] (columns: Congress, Session, Begin Date, Adjourn Date; the row reads session 2 then session 1):
> "119 2 1 Jan 3, 2026 Jan 3, 2025 Jan 3, 2026 118 2 1 Jan 3, 2024 Jan 3, 2023 Jan 3, 2025 Jan 3, 2024 117 2 1 Jan 3, 2022 Jan 3, 2021 Jan 3, 2023 Jan 3, 2022"

**DERIVED** reading: the 117th's first session began 2021-01-03 and its second adjourned 2023-01-03; the 118th ran 2023-01-03 to 2025-01-03; the 119th's first session began 2025-01-03 and its second 2026-01-03. The boundary dates match the 20th Amendment's "noon on the 3d day of January" (§4 below). The election dates are the statutory election day (§4, 2 U.S.C. §7): **DERIVED** 2022-11-08, 2024-11-05 (the Tuesday after the first Monday); 2024-11-05 is stated outright by NARA [EC-DATES].

**The divisions.** [HH-DIV], the History, Art & Archives table "Party Divisions of the House of Representatives" (columns: Congress (Years), # of House Seats, Democrats, Republicans, Other, Del./Res.):
> "117th (2021–2023) 435 6 222 212 0 5/1 118th (2023–2025) 435 213 222 0 5/1 119th (2025–2027) 435 215 220 0 5/1"

(The "6" after the 117th's 435 is a footnote mark, not a figure.) The Congress profiles state the same and say what the figure is:
- [HH-117]: > "Total Membership: 435 Representatives 5 Delegates 1 Resident Commissioner Party Divisions: * 222 Democrats 212 Republicans * Party division totals are based on election day results."
- [HH-118]: > "Total Membership: 435 Representatives 5 Delegates 1 Resident Commissioner Party Divisions: * 222 Republicans 213 Democrats * Party division totals are based on election day results."
- [HH-119]: the profile page carries no membership or leadership figures yet ("Total Membership: Party Divisions: * *"); the 119th's opening figure above is the [HH-DIV] table's.

**The Clerk's live count** (the roll-call pages' sidebar, as served on 2026-09-24) [CLK-R1]:
> "Republicans 218 218 Democrats 214 214 Independents 1 1 Vacancies 2 2"

**DERIVED**: the House's composition on 2026-09-24 differs from its opening by the vacancies and one member sitting as an independent; the Clerk's page does not date the changes, so the dates of the vacancies inside the 119th are a GAP (G4).

**The Speaker's election and the vacancy of October 2023** (the 118th Congress's profile footnotes, [HH-118]):
> "1 Kevin McCarthy was elected Speaker on calendar day January 7, 2023, and legislative day January 6, 2023. Pursuant to H. Res. 757, "Declaring the office of Speaker of the House of Representatives to be vacant," Speaker McCarthy was removed from the Speakership on October 3, 2023. 2 Mike Johnson was elected Speaker on October 25, 2023, to fill the vacancy caused by the removal of Speaker McCarthy from the Speakership pursuant to H. Res. 757."

**DERIVED**: two periods inside the window when the House had no elected Speaker: **2023-01-03 to 2023-01-07** (the 118th convened without electing a Speaker until calendar day 7 January) and **2023-10-03 to 2023-10-25** (the vacancy declared by H. Res. 757). Who presided pro tempore in October 2023 is not on a fetched page (G5). The 119th's Speaker election on its opening day is the Clerk's roll call 2 [CLK-R2]:
> "Roll Call 2 Share XML View Jan 03, 2025, 02:33 PM | 119th Congress, 1st Session Vote Question: Election of the Speaker Vote Type: Yea-And-Nay Status: Johnson (LA) VOTES Johnson (LA): 218 Jeffries: 215 Emmer: 1 Present: 0 Not Voting: 0"

### 1b. The Senate by Congress

| from | to | Congress | Senate party division (senate.gov, "Party Division") | leader of the majority | source ids |
|---|---|---|---|---|---|
| (before the window) | 2023-01-03 | 117th (2021–2023) | 100 seats: **DEM 48 + 2 I caucusing with DEM = 50; REP 50** (majority party: Democrats, on the Vice President's vote — see the note) | Schumer (DEM) | [SEN-DIV] [SEN-LEAD-H] |
| 2023-01-03 | 2025-01-03 | 118th (2023–2025) | 100 seats: **DEM 47, REP 49, 4 I** (majority party: Democrats) | Schumer (DEM) | [SEN-DIV] [SEN-LEAD-H] |
| 2025-01-03 | (open) | 119th (2025–2027) | 100 seats: **REP 53, DEM 45, 2 I** (majority party: Republicans) | Thune (REP) | [SEN-DIV] [SEN-LEAD-H] [SEN-LEAD] |

[SEN-DIV], the Senate's "Party Division" page as captured 2026-09-19:
> "117th Congress (2021–2023) Majority Party: Democrats (48 seats) Minority Party: Republicans (50 seats) Other Parties: 2 Independents (all caucus with the Democrats) Total Seats: 100 Note: From January 3, 2021, to January 20, 2021, party division stood at 51 Republicans, 46 Democrats, 2 Independents (who caucused with the Democrats), and 1 vacancy."
>
> "118th Congress (2023–2025) Majority Party: Democrats (47 seats) Minority Party: Republicans (49 seats) Other Parties: 4 Independents Total Seats: 100"
>
> "119th Congress (2025–2027) Majority Party: Republicans (53 seats) Minority Party: Democrats (45 seats) Other Parties: 2 Independents Total Seats: 100 Note: This total includes Senator-elect James Justice of West Virginia, who chose to delay his swearing-in to January 14, 2025, to complete his term as governor. With the resignation of JD Vance effective January 10, 2025, Ohio's Class 3 seat remains temporarily vacant."

**DERIVED**: the 118th's majority was 47 + 4 = 51 only with the four independents; the page does not say whom the four caucused with, so the "Democrats" majority label is the page's own, not a count of this file's. The Senate's leaders page explains the 117th's 50–50 majority [SEN-LEAD-H]:
> "17 From January 3 to January 20, 2021, with Republicans holding a majority with 51 senators, Mitch McConnell served as the majority leader and Charles Schumer remained the minority leader."

and the same footnote continues that the Vice President's
> "tie-breaking vote established a Democratic majority in the Senate, making Charles Schumer the majority leader and Mitch McConnell the minority leader."

**The three classes, and which class was up** ([SEN-CLASSES], captured 2026-09-18; the class pages captured 2026-08-30 / 2026-09-13):
> "Senators are elected to six-year terms, and every two years the members of one class--approximately one-third of the senators--face election or reelection."

| class | term (the class page's own sentence) | elected | up inside the window | senators on the captured page (**DERIVED** count of "(D-XX)/(R-XX)/(I-XX)" tags) | source id |
|---|---|---|---|---|---|
| Class III | "Class III terms run from the beginning of the 118th Congress on January 3, 2023, to the end of the 120th Congress on January 3, 2029. Senators in Class III were elected to office in the November 2022 general election, unless they took their seat through appointment or special election." | **2022-11-08** | 2022 | 34: 15 D, 19 R (capture 2026-09-13) | [SEN-C3] |
| Class I | "Class I terms run from the beginning of the 119th Congress on January 3, 2025, to the end of the 121st Congress on January 3, 2031. Senators in Class I were elected to office in the November 2024 general election, unless they took their seat through appointment or special election." | **2024-11-05** | 2024 | 33: 17 D, 14 R, 2 I (capture 2026-08-30) | [SEN-C1] |
| Class II | "Class II terms run from the beginning of the 117th Congress on January 3, 2021, to the end of the 119th Congress on January 3, 2027. Senators in Class II were elected to office in the November 2020 general election, unless they took their seat through appointment or special election." | 2020-11-03 | **2026** (terms expire 2027-01-03) | 33: 13 D, 20 R (capture 2026-08-30) | [SEN-C2] |

**DERIVED**: the class up in 2026 is Class II, at the regular election "next preceding the expiration of the term" (2 U.S.C. §1, [USC-1]) on the statutory day (2 U.S.C. §7, [USC-7]: "The Tuesday next after the 1st Monday in November, in every even numbered year") — **2026-11-03**. Special elections for the seats vacated inside the window (Ohio's Class 3 after Vance, [SEN-DIV]'s note) fall on the same day where a state schedules them so; the states' schedules were not fetched (G6). The senators by state are on the three class pages; a per-state seat table is not extracted here (spec §6 "sourced by state" is owed to the stage that allocates seats — G7).

## 2. THE EXECUTIVE OF RECORD BY DATE

| from | to | president | vice president | boundary event | source ids |
|---|---|---|---|---|---|
| 2021-01-20 (before the window) | 2025-01-20, noon | **Joseph R. Biden Jr. (DEM)**, "the 46th President" | **Kamala D. Harris (DEM)** | sworn in 2021-01-20; term ended at noon 2025-01-20 by the 20th Amendment §1 (**DERIVED** from the rule; no fetched page states Biden's last day in words) | [WH-BIDEN] [WH-HARRIS] [CONST-AM] |
| 2025-01-20, noon | (open; the term ends at noon 2029-01-20, **DERIVED** by the 20th Amendment) | **Donald J. Trump (REP)**, "45th & 47th President" | **JD Vance (REP)** | the 2024 election (2024-11-05), electors' vote 2024-12-17, count 2025-01-06, inauguration 2025-01-20 | [WH-TRUMP] [WH-VANCE] [WH-INAUG] [EC-2024] [EC-DATES] |

- [WH-BIDEN] (the Biden White House archive): > "After being sworn in as the 46th President on January 20th, 2021, he took swift action to get America vaccinated and jumpstart an economic recovery that created more jobs than any other President has created in four years."
- [WH-HARRIS]: > "On January 20, 2021, Kamala Harris was sworn in as Vice President – the first woman, the first Black American, and the first South Asian American to be elected to this position."
- [WH-TRUMP] (the White House, as served 2026-09-24): > "Donald J. Trump 45th & 47th President of the United States" and > "After a landslide election victory in 2024, President Donald J. Trump is returning to the White House to build upon his previous successes and use his mandate to reject the extremist policies of the radical left while providing tangible quality of life improvements for the American people."
- [WH-VANCE]: > "JD Vance Vice President of the United States"
- [WH-INAUG], the inaugural address as published by the White House: > "The Inaugural Address The White House January 20, 2025 U.S. Capitol Washington, D.C. 12:10 P.M. EST THE PRESIDENT: Thank you. Thank you very much, everybody. (Applause.) Wow. Thank you very, very much. Vice President Vance, Speaker Johnson, Senator Thune, Chief Justice Roberts, justices of the Supreme Court of the United States, President Clinton, President Bush, President Obama, President Biden, Vice President Harris, and my fellow citizens"

**The 2024 presidential election's calendar** (NARA's Electoral College pages):
- [EC-2024] (the results page, "last reviewed on January 13, 2025"): > "President Donald J. Trump [R] Main Opponent Kamala D. Harris [D] Electoral Vote Winner: 312 Main Opponent: 226 Total/Majority: 538/270 Vice President JD Vance [R] V.P. Opponent Tim Walz [D]" and the timeline > "December 17, 2024 —Electors vote The electors in each State meet to select the President and Vice President of the United States. January 6, 2025 —Congress counts the vote Congress meets in joint session to count the"
- [EC-DATES] (the key-dates page for 2024): > "November 5, 2024—Election Day (first Tuesday after the first Monday in November*)"; > "December 17, 2024—electors vote in their States The electors meet in their respective States and vote for President and Vice President on separate ballots."; > "January 6, 2025—Congress counts the electoral votes Congress meets in joint session to count the electoral votes. The Vice President, as President of the Senate, presides over the count and announces the results"; > "January 20, 2025 at Noon—Inauguration Day The President-elect and Vice President-elect take the Oath of Office and become the President of the United States and Vice President of the United States, respectively."

| date | event | source |
|---|---|---|
| 2024-11-05 | election day | [EC-DATES] [USC-7] |
| 2024-12-11 | states' certificates of ascertainment due ("at least six days before the meeting of the electors") | [EC-DATES] |
| 2024-12-17 | the electors vote in their states | [EC-DATES] [EC-2024] |
| 2025-01-06 | Congress counts the electoral votes in joint session | [EC-DATES] [EC-2024] |
| 2025-01-20, noon | inauguration; the address was delivered at 12:10 P.M. EST | [EC-DATES] [WH-INAUG] |

The popular and electoral returns themselves are `returns_2024.md` (FEC, the Clerk, NARA) and are not repeated here.

## 3. PARTY LEADERS BY DATE

### 3a. The party chairs

| party | chair | from | to | evidence | source ids |
|---|---|---|---|---|---|
| DEM (DNC) | Jaime Harrison | (before the window) | 2025-02-01 | the DNC's own releases are headed "DNC Chair Jaime Harrison" on 2024-07-19, 2024-07-21, 2024-09-10, 2024-10-02, 2025-01-15 and 2025-01-20 (the site search's dated result list) | [DNC-SEARCH-H] |
| DEM (DNC) | Ken Martin | 2025-02-01 | (open; listed as Chair on the leadership page served 2026-09-24) | the DNC's release of 2025-02-01 | [DNC-MARTIN] [DNC-LEAD] [DNC-SEARCH-M] |
| REP (RNC) | **GAP (G1)** | — | — | gop.com refused every fetch and is not archived; no RNC page naming its chair or a change date was saved | — |

- [DNC-MARTIN] (democrats.org, "February 1, 2025"): > "Today, the DNC Membership elected Ken Martin, Minnesota Democratic-Farmer-Labor Chair and Association of State Democratic Committees President, as the new Chair of the Democratic National Committee."
- [DNC-LEAD] (the leadership page, served 2026-09-24): > "Ken Martin Chair Jane Kleeb ASDC President, Vice Chair"
- [DNC-SEARCH-H] (the site's search results for "DNC Chair Jaime Harrison", each item dated by the site): > "January 20, 2025 DNC Chair Jaime Harrison Statement on MLK Jr. Day On Martin Luther King Jr. Day, DNC Chair Jaime Harrison released the following statement:" and > "January 15, 2025 DNC Chair Jaime Harrison Statement on President Biden's Historic Achievements Following President Joe Biden's address to the nation, DNC Chair Jaime Harrison released the following statement: "Over the last four years, I have had the honor of serving as the…"
- **DERIVED**: Harrison's tenure ended with Martin's election on 2025-02-01; no fetched page states Harrison's last day in words, and his start date is before the window (not sourced here). The DNC's search page also shows Martin as chair through 2026-09-07 ("September 7, 2026 DNC Chair Ken Martin Statement on Labor Day", [DNC-SEARCH-M]).

### 3b. The congressional leaders

| office | holder | from | to | source ids |
|---|---|---|---|---|
| Speaker of the House | Nancy Pelosi (DEM) | (before the window) | 2023-01-03 (the 117th's end, **DERIVED**) | [HH-117] |
| Speaker of the House | **vacant** (no Speaker elected) | 2023-01-03 | 2023-01-07 | [HH-118] |
| Speaker of the House | Kevin McCarthy (REP) | 2023-01-07 ("calendar day January 7, 2023, and legislative day January 6, 2023") | 2023-10-03 (removed, H. Res. 757) | [HH-118] |
| Speaker of the House | **vacant** (H. Res. 757) | 2023-10-03 | 2023-10-25 | [HH-118] |
| Speaker of the House | Mike Johnson (REP) | 2023-10-25 | (open); re-elected 2025-01-03 by 218 votes to Jeffries's 215 and Emmer's 1 [CLK-R2]; listed as Speaker on house.gov 2026-09-24 | [HH-118] [CLK-R2] [HOUSE-LEAD] |
| House Majority Leader | Steny Hoyer (DEM) | (before the window) | 2023-01-03 (**DERIVED**) | [HH-117] |
| House Majority Leader | Steve Scalise (REP) | 2023-01-03 (the 118th's opening, **DERIVED** — the profile lists him for the Congress without a date) | (open); listed 2026-09-24 | [HH-118] [HOUSE-LEAD] |
| House Minority Leader | Kevin McCarthy (REP) | (before the window) | 2023-01-03 (**DERIVED**) | [HH-117] |
| House Minority Leader | Hakeem Jeffries (DEM) | 2023-01-03 (**DERIVED**, as above) | (open); "Democratic Leader Rep. Hakeem Jeffries" 2026-09-24 | [HH-118] [HOUSE-LEAD] |
| Senate Majority Leader | Charles E. Schumer (DEM) | 2021-01-20 (before the window) | 2025-01-03 (**DERIVED**: the table is by Congress) | [SEN-LEAD-H] |
| Senate Minority Leader | Mitch McConnell (REP) | 2021-01-20 (before the window) | 2025-01-03 (**DERIVED**) | [SEN-LEAD-H] |
| Senate Majority Leader | John Thune (REP) | 2025-01-03 (**DERIVED**) | (open); "Senate Majority Leader Thune, John (R-SD)" on the leadership page captured 2026-09-21 | [SEN-LEAD-H] [SEN-LEAD] |
| Senate Minority Leader | Charles E. Schumer (DEM) | 2025-01-03 (**DERIVED**) | (open); "Democratic Leader Chair of the Conference Schumer, Charles E. (D-NY)" captured 2026-09-21 | [SEN-LEAD-H] [SEN-LEAD] |

- [HH-117]: > "Speaker of the House: Nancy Pelosi (D–California) Majority Leader: Steny Hoyer (D–Maryland) Minority Leader: Kevin McCarthy (R–California)"
- [HH-118]: > "Speaker of the House: Kevin McCarthy (R–California) 1 Mike Johnson (R–Louisiana) 2 Majority Leader: Steve Scalise (R–Louisiana) Minority Leader: Hakeem Jeffries (D–New York)"
- [HOUSE-LEAD] (house.gov/leadership, served 2026-09-24): > "Speaker of the House Rep. Mike Johnson"; > "Republican Leadership Majority Leader Rep. Steve Scalise Represents Republicans on the House floor."; > "Democratic Leader Rep. Hakeem Jeffries Represents Democrats on the House floor"
- [SEN-LEAD-H] (the Senate's "Majority and Minority Leaders" table by Congress, columns Majority Leader then Minority Leader, captured 2026-09-05): > "117th Congress (2021–2023) 17 17 Schumer Charles E. Schumer (D-NY) McConnell Mitch McConnell (R-KY)"; > "118th Congress (2023–2025) Schumer Charles E. Schumer (D-NY) McConnell Mitch McConnell (R-KY)"; > "119th Congress (2025–2027) Thune John Thune (R-SD) Schumer Charles E. Schumer (D-NY)" (the "17 17" is the footnote mark quoted under §1b).
- [SEN-LEAD] (senate.gov/senators/leadership.htm, captured 2026-09-21): > "Senate Majority Leader Thune, John (R-SD)" and > "Democratic Leader Chair of the Conference Schumer, Charles E. (D-NY)"

**DERIVED**: the Senate table gives leaders per Congress; the change from Schumer/McConnell to Thune/Schumer is dated here to the 119th's convening, 2025-01-03, which is the table's granularity, not a stated election date of the party conferences (G8). The House majority and minority leaders are likewise dated to the 118th's opening from the profile.

## 4. THE CONSTITUTIONAL AND PROCEDURAL RULES (spec §6, §11)

**The veto and the two-thirds override — Article I, Section 7** (NARA's transcript, [CONST]):
> "Every Bill which shall have passed the House of Representatives and the Senate, shall, before it become a Law, be presented to the President of the United States; If he approve he shall sign it, but if not he shall return it, with his Objections to that House in which it shall have originated, who shall enter the Objections at large on their Journal, and proceed to reconsider it. If after such Reconsideration two thirds of that House shall agree to pass the Bill, it shall be sent, together with the Objections, to the other House, by which it shall likewise be reconsidered, and if approved by two thirds of that House, it shall become a Law. But in all such Cases the Votes of both Houses shall be determined by yeas and Nays, and the Names of the Persons voting for and against the Bill shall be entered on the Journal of each House respectively. If any Bill shall not be returned by the President within ten Days (Sundays excepted) after it shall have been presented to him, the Same shall be a Law, in like Manner as if he had signed it, unless the Congress by their Adjournment prevent its Return, in which Case it shall not be a Law."

**Inauguration and term dates — the 20th Amendment, Section 1** ([CONST-AM]):
> "Section 1. The terms of the President and Vice President shall end at noon on the 20th day of January, and the terms of Senators and Representatives at noon on the 3d day of January, of the years in which such terms would have ended if this article had not been ratified; and the terms of their successors shall then begin."

**The two-term limit — the 22nd Amendment, Section 1** ([CONST-AM]):
> "Section 1. No person shall be elected to the office of the President more than twice, and no person who has held the office of President, or acted as President, for more than two years of a term to which some other person was elected President shall be elected to the office of the President more than once."

**DERIVED** (for the game's §6 rule "the constitutional two-term limit ends the run when it binds"): a president elected twice cannot be elected again; a vice president who succeeds and serves more than two years of the predecessor's term can be elected only once. The record here does not apply the rule to any person.

**Election day — 2 U.S.C. §7 and §1** (the U.S. Code, 2023 edition, GPO):
- [USC-7]: > "§7. Time of election The Tuesday next after the 1st Monday in November, in every even numbered year, is established as the day for the election, in each of the States and Territories of the United States, of Representatives and Delegates to the Congress commencing on the 3d day of January next thereafter."
- [USC-1]: > "§1. Time for election of Senators At the regular election held in any State next preceding the expiration of the term for which any Senator was elected to represent such State in Congress, at which election a Representative to Congress is regularly by law to be chosen, a United States Senator from said State shall be elected by the people thereof for the term commencing on the 3d day of January next thereafter."

**Cloture — Standing Rule XXII, paragraph 2** (the Rules of the Senate as published by the Committee on Rules and Administration, captured 2026-08-31, [SEN-RULES]):
> "Notwithstanding the provisions of rule II or rule IV or any other rule of the Senate, at any time a motion signed by sixteen Senators, to bring to a close the debate upon any measure, motion, other matter pending before the Senate, or the unfinished business, is presented to the Senate, the Presiding Officer, or clerk at the direction of the Presiding Officer, shall at once state the motion to the Senate, and one hour after the Senate meets on the following calendar day but one, he shall lay the motion before the Senate and direct that the clerk call the roll, and upon the ascertainment that a quorum is present, the Presiding Officer shall, without debate, submit to the Senate by a yea-and-nay vote the question:"
>
> "three-fifths of the Senators duly chosen and sworn -- except on a measure or motion to amend the Senate rules, in which case the necessary affirmative vote shall be two-thirds of the Senators present and voting -- then said measure, motion, or other matter pending before the Senate, or the unfinished business, shall be the unfinished business to the exclusion of all other business until disposed of."

(The question between the two fragments reads, in the saved bytes with escaped quotation marks, `\"Is it the sense of the Senate that the debate shall be brought to a close?\" And if that question shall be decided in the affirmative by` — the page embeds the rule text in a script block.) The Senate's own gloss [SEN-CLOTURE], captured 2026-09-20:
> "In 1975 the Senate reduced the number of votes required for cloture from two-thirds of senators voting to three-fifths of all senators duly chosen and sworn, or 60 of the 100-member Senate."

**DERIVED**: with 100 senators sworn, cloture on legislation needs 60 votes; on a change to the rules, two-thirds of those present and voting. The rules page does not name the 2013 and 2017 precedents on nominations; those are not part of the legislative threshold the spec asks for and were not sourced (G9).

**The budget-reconciliation exception — the Congressional Budget Act of 1974 as codified, 2 U.S.C. §641** (GPO's U.S. Code, 2023 edition, [USC-641]):
> "§641. Reconciliation (a) Inclusion of reconciliation directives in concurrent resolutions on the budget A concurrent resolution on the budget for any fiscal year, to the extent necessary to effectuate the provisions and requirements of such resolution, shall— (1) specify the total amount by which— (A) new budget authority for such fiscal year; (B) budget authority initially provided for prior fiscal years; (C) new entitlement authority which is to become effective during such fiscal year; and (D) credit authority for such fiscal year, contained in laws, bills, and resolutions within the jurisdiction of a committee, is"
>
> "(e) Procedure in Senate (1) Except as provided in paragraph (2), the provisions of section 636 of this title for the consideration in the Senate of concurrent resolutions on the budget and conference reports thereon shall also apply to the consideration in the Senate of reconciliation bills reported under subsection (b) and conference reports thereon. (2) Debate in the Senate on any reconciliation bill reported under subsection (b), and all amendments thereto and debatable motions and appeals in connection therewith, shall be limited to not more than 20 hours."

and the Byrd rule, 2 U.S.C. §644 ([USC-644]):
> "§644. Extraneous matter in reconciliation legislation (a) In general When the Senate is considering a reconciliation bill or a reconciliation resolution pursu ant to section 641 of this title (whether that bill or resolution originated in the Senate or the House) or section 907d of this title, upon a point of order being made by any Senator against material extraneous to the instructions to a committee which is contained in any title or provision of the bill or resolution or offered as an amendment to the bill or resolution, and the point of order is sustained by the Chair, any part of said title o"

*[sic: "pursu ant" is a line break in the GPO text.]* **DERIVED** reading: a reconciliation bill's Senate debate is limited by statute to 20 hours, so it cannot be filibustered and needs no cloture — a simple majority passes it — but the Byrd rule strikes matter extraneous to the budget instructions on a point of order. The statute does not say "simple majority" in words; that is the reading, and the rule that a Byrd-rule point of order is waived only by three-fifths (§644(e)) was not quoted (G10).

## Cross-check against the spec's §4 table

The spec's §4 row for the USA names the start as the standard run-up before the scheduled 2024-11-05 election and the government of record as "Biden's administration and the 118th Congress". **DERIVED**: Sweden's standard run-up is 26 weeks plus an 8-week campaign, i.e. 238 days before polling day; 2024-11-05 − 238 days = **2024-03-12**. On that date the record above gives: President Biden (DEM) and Vice President Harris (DEM); the 118th Congress (convened 2023-01-03, election-day division 222 REP / 213 DEM in the House, 47 DEM + 4 I / 49 REP in the Senate); Speaker Mike Johnson (since 2023-10-25 — not McCarthy), House Majority Leader Scalise, Minority Leader Jeffries; Senate Majority Leader Schumer, Minority Leader McConnell; DNC chair Harrison; RNC chair a GAP. **Nothing fetched contradicts the §4 line.** Two things the line does not say and a builder must not assume: the Speaker on the start date is Johnson, and the 118th's Senate majority rests on four independents.

## Source register
(all fetched 2026-09-24, 16:11–16:36 UTC; saved as `raw/records/<file>`; "page's own date" is the date the page shows, or the archive capture timestamp for a Wayback copy, as marked; bytes and SHA-256 are the saved file's, as in `raw/records/SHA256SUMS.txt`)

| id | URL | publisher | page's own date / capture | basis | file | bytes | SHA-256 |
|---|---|---|---|---|---|---|---|
| [HH-DIV] | https://history.house.gov/Institution/Party-Divisions/Party-Divisions/ | Office of the Historian, U.S. House (History, Art & Archives) | no page date shown; table runs to the 119th | official House historical table | `history_house_party_divisions.html` | 176436 | `58b1a5887688d0fad9e122204d5fc66958e5660c57a7892fa20a9c8016f99b6b` |
| [HH-117] | https://history.house.gov/Congressional-Overview/Profiles/117th/ | Office of the Historian, U.S. House | no page date shown | official Congress profile | `history_house_profile_117th.html` | 147546 | `ecac9116d85aa82aca9a53f8b96f905f83d4f2c53530969822f75cf2f3c9c191` |
| [HH-118] | https://history.house.gov/Congressional-Overview/Profiles/118th/ | Office of the Historian, U.S. House | no page date shown | official Congress profile | `history_house_profile_118th.html` | 140162 | `b461fc77ecd48324cd0a5b9071018c709df38ed23dc8b9761ade0f9b4a1e802a` |
| [HH-119] | https://history.house.gov/Congressional-Overview/Profiles/119th/ | Office of the Historian, U.S. House | no page date shown; figures not yet filled | official Congress profile (stub) | `history_house_profile_119th.html` | 129139 | `12f7503e37f15d909f9b498ffb389f69835c2e81aa4cebf9c8bbf6ab1502c177` |
| [HH-SPK] | https://history.house.gov/People/Office/Speakers/ | Office of the Historian, U.S. House | no page date shown | essay page; names Johnson as the current Speaker, no dates (not quoted) | `history_house_speakers.html` | 128288 | `0e9b2fd7232bc93e3d584152af624a968b8daa74ce540bd788e2bb481b926bd3` |
| [HH-MAJ] | https://history.house.gov/People/Office/Majority-Leaders/ | Office of the Historian, U.S. House | no page date shown | list page; the current era is not in the served HTML (not quoted) | `history_house_majority_leaders.html` | 185015 | `e9433b748451515fa9e79e56025a1b63c0444c5483684725b0775f7b6436b50c` |
| [HH-MIN] | https://history.house.gov/People/Office/Minority-Leaders/ | Office of the Historian, U.S. House | no page date shown | list page; the current era is not in the served HTML (not quoted) | `history_house_minority_leaders.html` | 172211 | `f3a16417cc6bc6d96329d4a34cf08d46a47a3e9fc459cbd7e9af44c0b160a068` |
| [HOUSE-LEAD] | https://www.house.gov/leadership | U.S. House of Representatives | live page as served 2026-09-24 | official current leadership | `house_gov_leadership.html` | 24279 | `afe9efc4b1c4b18b549e94f31bb929efe250af62914d47bcb4ab277d631dc143` |
| [CLK-R1] | https://clerk.house.gov/Votes/20251 | Clerk of the U.S. House | roll call dated "Jan 03, 2025, 12:33 PM"; the sidebar party count is live as served 2026-09-24 | official roll call (quorum call of the 119th's first day) | `clerk_house_vote_2025_roll001.html` | 567594 | `1a19a28648620219ee2d265e2ca09920fb3dd3dad8bbefb662b3b7d8f5e34e5d` |
| [CLK-R2] | https://clerk.house.gov/Votes/20252 | Clerk of the U.S. House | roll call dated "Jan 03, 2025, 02:33 PM" | official roll call (election of the Speaker) | `clerk_house_vote_2025_roll002.html` | 547568 | `45ffcf3e396e8f1512f91a8dc7ab49c0d33cbf2868d5b904baeec261f14c80ea` |
| [SEN-DATES] | https://www.senate.gov/legislative/DatesofSessionsofCongress.htm | U.S. Senate (Internet Archive capture) | capture 2026-09-18 16:19:55 UTC | official table, archived copy | `wayback_senate_dates_of_sessions_20260918.html` | 48458 | `8fe8c9e6c38290bba57fc1fbdc28cf3ad7afc1ec24082551989f281f134357bb` |
| [SEN-DIV] | https://www.senate.gov/history/partydiv.htm | U.S. Senate (Internet Archive capture) | capture 2026-09-19 18:11:41 UTC | official table, archived copy | `wayback_senate_party_division_20260919.html` | 63550 | `ed076a0c0c6be0f7cac83a99afba3c3077d0ed11312eb9c543458b3f5e75befc` |
| [SEN-CLASSES] | https://www.senate.gov/about/origins-foundations/senate-and-constitution/senate-classes.htm | U.S. Senate (Internet Archive capture) | capture 2026-09-18 16:19:56 UTC; its class labels ("Class I (term expires in 2025)") are stale on the captured page | official explainer, archived copy | `wayback_senate_classes_20260918.html` | 33291 | `d7ce4d7eae273638b78ac4198928239dc002e374955de2d97e027058cf5dfc59` |
| [SEN-C1] | https://www.senate.gov/senators/Class_I.htm | U.S. Senate (Internet Archive capture) | capture 2026-08-30 23:32:15 UTC | official list by class, archived copy | `wayback_senate_class_I_20260830.html` | 31064 | `cfed4b2ffad38fd9edbee4a3a437e0478e6deb934b44db79ff340f818063b3d1` |
| [SEN-C2] | https://www.senate.gov/senators/Class_II.htm | U.S. Senate (Internet Archive capture) | capture 2026-08-30 23:32:31 UTC | official list by class, archived copy | `wayback_senate_class_II_20260830.html` | 30937 | `4b266287eaee132c92e34682ee1874835d31a382cbdb02b39e6ae9fd4af1a936` |
| [SEN-C3] | https://www.senate.gov/senators/Class_III.htm | U.S. Senate (Internet Archive capture) | capture 2026-09-13 09:31:07 UTC | official list by class, archived copy | `wayback_senate_class_III_20260913.html` | 30925 | `ba3cfd31a8a75e4336774f8b1ef90877bf4c06091e2e36180977258fbbec8249` |
| [SEN-LEAD-H] | https://www.senate.gov/artandhistory/history/common/briefing/Majority_Minority_Leaders.htm | U.S. Senate (Internet Archive capture) | capture 2026-09-05 17:32:45 UTC | official historical table, archived copy | `wayback_senate_majority_minority_leaders_briefing_20260905.html` | 81610 | `52230b4a86761705c103df5c765a41c04765a7452bda19ae84620dc063f03416` |
| [SEN-LEAD] | https://www.senate.gov/senators/leadership.htm | U.S. Senate (Internet Archive capture) | capture 2026-09-21 01:04:13 UTC | official current leadership, archived copy | `wayback_senate_leadership_20260921.html` | 41800 | `ab82e79e228c0c06cdb0119891437f79af4eb0636e9ea0e1ddf04f88d4119b00` |
| [SEN-RULES] | https://www.rules.senate.gov/rules-of-the-senate | U.S. Senate Committee on Rules and Administration (Internet Archive capture) | capture 2026-08-31 12:51:06 UTC; the page's rule entries carry "modified" 2024-03-12 | Standing Rules of the Senate, archived copy | `wayback_senate_rules_of_the_senate_20260831.html` | 362789 | `f060da465e46f17f51a919bada174c1805d8f83358b51fe1c39359e6e8ba52cb` |
| [SEN-CLOTURE] | https://www.senate.gov/about/powers-procedures/filibusters-cloture.htm | U.S. Senate (Internet Archive capture) | capture 2026-09-20 23:12:14 UTC | official explainer, archived copy | `wayback_senate_filibusters_cloture_20260920.html` | 21966 | `7eae834dac4f27fe23d0f9a5e7c5e60f8cad81c405d0d581f5ce22ff29f24eef` |
| [CONST] | https://www.archives.gov/founding-docs/constitution-transcript | U.S. National Archives (NARA) | no page date shown | official transcript of the Constitution | `archives_constitution_transcript.html` | 70555 | `8e33e6ad173184d99527096792e47f16db2d9fbd510fbefb1fe0d212e2a0f5e4` |
| [CONST-AM] | https://www.archives.gov/founding-docs/amendments-11-27 | U.S. National Archives (NARA) | no page date shown | official transcript, Amendments 11–27 | `archives_amendments_11-27.html` | 57662 | `95560fca4fe948ffa1d156f466c405592a7057e557b220b5308fef5ae51e784f` |
| [EC-2024] | https://www.archives.gov/electoral-college/2024 | U.S. National Archives (NARA) | "This page was last reviewed on January 13, 2025." | official Electoral College results and timeline | `archives_electoral_college_2024.html` | 57762 | `45861f6999e950d22823ee3a952f74cd94b0266576feb278ca5bce053c629f1f` |
| [EC-DATES] | https://www.archives.gov/electoral-college/key-dates | U.S. National Archives (NARA) | no page date shown; the dates are the 2024 cycle's | official calendar | `archives_electoral_college_key_dates.html` | 41224 | `8d286ae3a5504cf53c5aa8974d7b0d4bbd12a5ba5a95bfa2fccb4312987c2288` |
| [WH-BIDEN] | https://bidenwhitehouse.archives.gov/administration/president-biden/ | The White House (Biden administration archive, NARA-hosted) | no page date shown | official biography, archived site | `bidenwhitehouse_president_biden.html` | 160458 | `e529cd9d2d8f1e3153f666070cff7f5f884286fb33990b394b1acc0a7f307102` |
| [WH-HARRIS] | https://bidenwhitehouse.archives.gov/administration/vice-president-harris/ | The White House (Biden administration archive) | no page date shown | official biography, archived site | `bidenwhitehouse_vice_president_harris.html` | 178063 | `03594405182d54d449721c6c77b289bb6dae3b1673784da1eb304a9a5f94b954` |
| [WH-TRUMP] | https://www.whitehouse.gov/administration/donald-j-trump/ | The White House | live page as served 2026-09-24 | official biography | `whitehouse_president_trump.html` | 245866 | `7b647976a64c2e7ecd5dd69990e62174635ffb9d3c75a539195dd12b3eba0b86` |
| [WH-VANCE] | https://www.whitehouse.gov/administration/jd-vance/ | The White House | live page as served 2026-09-24 | official biography | `whitehouse_vice_president_vance.html` | 244390 | `1d544ff6701b33d968488a1e2263b0dfbc94d812e6b520679ff993d8d5c80db0` |
| [WH-INAUG] | https://www.whitehouse.gov/remarks/2025/01/the-inaugural-address/ | The White House | "January 20, 2025" | official transcript | `whitehouse_inaugural_address_2025-01-20.html` | 280299 | `1b7b6b1ee569594d28538590d2fc4a8aef43a9f191e89f0ca663f96897003b08` |
| [USC-641] | https://www.govinfo.gov/content/pkg/USCODE-2023-title2/html/USCODE-2023-title2-chap17A-subchapI-sec641.htm | U.S. Government Publishing Office (U.S. Code, 2023 edition) | 2023 edition | statute as codified | `govinfo_uscode_2_641_reconciliation.html` | 20085 | `b2df1da271ceb7bbb5d8d526f8754773f9fda8acd302a6fd2857b29fe79043f4` |
| [USC-644] | https://www.govinfo.gov/content/pkg/USCODE-2023-title2/html/USCODE-2023-title2-chap17A-subchapI-sec644.htm | U.S. Government Publishing Office (U.S. Code, 2023 edition) | 2023 edition | statute as codified | `govinfo_uscode_2_644_byrd_rule.html` | 18245 | `e55b053583f637001bf895e9b938560fa4055bba154c5c2e46488e3df8bf35dc` |
| [USC-7] | https://www.govinfo.gov/content/pkg/USCODE-2023-title2/html/USCODE-2023-title2-chap1-sec7.htm | U.S. Government Publishing Office (U.S. Code, 2023 edition) | 2023 edition | statute as codified | `govinfo_uscode_2_7_election_day.html` | 3195 | `e619b160ad4579840f3471adb25da447b51607e74f8562dd0a674acddfebe786` |
| [USC-1] | https://www.govinfo.gov/content/pkg/USCODE-2023-title2/html/USCODE-2023-title2-chap1-sec1.htm | U.S. Government Publishing Office (U.S. Code, 2023 edition) | 2023 edition | statute as codified | `govinfo_uscode_2_1_senate_election_day.html` | 2838 | `84ebea761707d9af52956c7f502f202597ef2d505cd52ad44d7e4d8214089814` |
| [DNC-LEAD] | https://democrats.org/who-we-are/leadership/ | Democratic National Committee | live page as served 2026-09-24 | the party's own leadership page | `democrats_leadership.html` | 210914 | `949813d68681ab7dff8fbc0d271c76d0253f71dcf0ca6b82b84ef662e8606240` |
| [DNC-MARTIN] | https://democrats.org/news/icymi-democrats-enthusiastically-elect-ken-martin-as-new-dnc-chair/ | Democratic National Committee | "February 1, 2025" | the party's own press release | `democrats_20250201_elect_ken_martin_dnc_chair.html` | 196352 | `d5b196f90ef7ab9e2314c9a2e75b72900be3587b27bd421fc00f249eb2fb0ee8` |
| [DNC-SEARCH-H] | https://democrats.org/?s=DNC+Chair+Jaime+Harrison | Democratic National Committee (site search) | live results as served 2026-09-24; each item dated by the site | the party's own dated release titles | `democrats_search_jaime_harrison.html` | 198322 | `2bb1291362efa7fdf4d50c92ed9af0e2f3be3edbde933a16d83441103eb6bb36` |
| [DNC-SEARCH-M] | https://democrats.org/?s=Ken+Martin+elected+chair+DNC | Democratic National Committee (site search) | live results as served 2026-09-24 | the party's own dated release titles (finder for [DNC-MARTIN]) | `democrats_search_ken_martin_elected.html` | 199005 | `3a272e3b78fa46951833dcad09dc69846646a9194eae2843759b794c47ed0f2b` |
| [DNC-SEARCH-M0] | https://democrats.org/?s=Ken+Martin+chair | Democratic National Committee (site search) | live results as served 2026-09-24 | finder only (not quoted) | `democrats_search_ken_martin.html` | 198253 | `01c8f7614997a587607ea9c9db3850529a93f9a187fd392e571a3cea32f5d33c` |

Discarded fetches (logged in `fetch_log.txt`, files deleted, nothing cited from them): the direct senate.gov / rules.senate.gov / constitution.congress.gov fetches (403 stubs); a Wayback capture of `majority-minority-leaders.htm` that was senate.gov's 404 page; two gop.com captures (403 stubs); the Clerk's roll call 6 of 2025 (the Laken Riley Act, fetched on the wrong guess that it was the Speaker election — roll call 2 is).

## GAPS

- **G1: the RNC chair is not sourced.** gop.com refused every fetch (Akamai 403) and the Wayback Machine does not serve it. No RNC page naming its chair, or the changes of chair inside the window, was saved. The DNC's search index shows a release titled "attn-rnc-chair-michael-whatley…" (a slug seen in the Wayback index only, not fetched) — a rival party's naming, not a source. Billed: the chair on 2024-03-12 and today, and each change date.
- **G2: Wayback copies stand in for senate.gov.** Every senate.gov quote is from an Internet Archive capture (timestamps in the register), not the live page on 2026-09-24. The captures are the Senate's own bytes as archived; a second session on a network senate.gov serves should re-fetch them live and re-hash.
- **G3: constitution.congress.gov and congress.gov were not reachable**; the Constitution is quoted from NARA's transcript, the statutes from GPO's U.S. Code. Same text, different official publisher than the brief named.
- **G4: the 119th House's vacancies and its one independent are undated.** The Clerk's live count (218 REP, 214 DEM, 1 I, 2 vacancies) is as served on 2026-09-24; which seats and since when were not fetched.
- **G5: the Speaker pro tempore of 3–25 October 2023 is not on a fetched page.** The vacancy's dates are sourced [HH-118]; who presided is not.
- **G6: the 2026 special Senate elections are not sourced** (Ohio's Class 3 seat after Vance's resignation, [SEN-DIV]'s note, and any other); the states' schedules were not fetched.
- **G7: the Senate by state.** The three class pages list every senator with party and state (saved), but this file does not extract a per-state seat table; spec §6's "100 seats in three classes, sourced by state" is owed to the seat-allocation stage.
- **G8: the leaders' change dates are the Congress boundaries.** The Senate table [SEN-LEAD-H] and the House profiles list leaders per Congress; the party conferences' election dates (Thune's election as Republican leader, Scalise's and Jeffries's) were not fetched, so each change is dated to the convening day and marked DERIVED.
- **G9: the cloture precedents on nominations (2013, 2017) were not sourced**; only Rule XXII's text and the Senate's gloss on legislation.
- **G10: the Byrd rule's waiver threshold (§644(e), three-fifths) was not quoted**, only §644(a)'s point of order; the "simple majority passes reconciliation" reading is DERIVED from the 20-hour debate limit, not stated in the statute's words.
- **G11: Biden's last day and Harrison's last day are DERIVED**, from the 20th Amendment and Martin's election respectively; no fetched page states either in words.
- **G12: second-session re-verification (R-K9) is pending**, as for every SOURCED file.

*(Filed 2026-09-24 by the PS-1 sourcing agent. Nothing outside `ElectionsData/usa/records_by_date.md` and `ElectionsData/usa/raw/records/` was written. No code was touched, Unity was not run, nothing was committed.)*
