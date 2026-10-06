# The US veto on the record - every public law and every vetoed measure of the 115th-119th Congresses, each chamber's final agreement, every override (PS-6 US-10)

**The record of `Tools/us_veto_prep.pl` (`COMPLETED.md` §802).** This record covers:
- every public law and every vetoed measure decided by a president in office from noon on 20 January 2017;
- each chamber's final agreement to the text and its party split;
- every override.

It is the base `Tools/us_veto_backtest.pl` measures R-US17's candidate rules on. That tool writes `docs/generated/US_VETO_BACKTEST.md`, and `UsVetoBacktestCheck` holds the document to this record in the cheap bar.

**What the extract holds** (`us_veto_measures.csv`, generated, never typed) - a row a measure:
- the Congress, the measure and its kind (below), and the outcome: signed, law without signature, vetoed, pocket vetoed, or vetoed and overridden;
- the day decided, the president and his party, the public law;
- for each chamber, its final agreement:
  - where it was by roll call: the roll call's number, its question, and its yea, nay, present and not-voting count by party (independents apart, a senator's caucus beside them);
  - where it was not: the method its words name (voice vote, unanimous consent);
- each override roll call with its yeas, nays and result;
- `same_day` (reading 4);
- the measure's own title.

## The sources

| what | where | held to |
|---|---|---|
| The GPO's **BILLSTATUS** bulk records, the 115th-119th Congresses, bills and joint resolutions of both chambers (20 zips, 271 MB) | out of tree, `PoliSim-captures/sources/usa_us10/billstatus/` | `raw/vetoes/out_of_tree.txt`, one line each (sha256, bytes, path, URL) |
| The **roll calls** the measures' actions name: the Clerk's XML for the House (`clerk.house.gov/evs/<year>/roll<n>.xml`); the Senate's LIS XML through the Internet Archive (`senate.gov` answers 403 directly), each named by its capture | out of tree, `rolls/house/`, `rolls/senate/` (761 fetched, 748 read) | `out_of_tree.txt` |
| **P.L. 94-329 as enacted** (90 Stat. 729), for its §601(b) - not classified to the Code | out of tree, `statutes/` | `out_of_tree.txt` |
| The Senate's **veto lists by president** (Trump's first term, Biden, Trump's second term) and its counts page, through the Internet Archive; the **House Historian's** *Presidential Vetoes* counts | in tree, `raw/vetoes/` | `SHA256SUMS.txt`, `fetch_log.txt` |
| The **Senate procedures** of the expedited measures, from the GPO's *United States Code, 2023 Edition*: 5 U.S.C. 802 (the CRA), 50 U.S.C. 1546a (War Powers), 50 U.S.C. 1622 (national emergencies), 22 U.S.C. 2776 (arms sales); D.C. Code §1-206.04 (the D.C. Law Library) | in tree, `raw/vetoes/` | `SHA256SUMS.txt` |
| **CRS RS22654**, *Veto Override Procedure in the House and Senate* (updated 5 January 2026), through congress.gov's `crs_external_products` path (`crsreports.congress.gov` sits behind a challenge) | in tree, `raw/vetoes/` | `SHA256SUMS.txt` |

All are fetched by `Tools/us_veto_fetch.pl`, which logs each fetch and stops on any status other than 200 or a page missing its marker. The Internet Archive rate-limits, so the tool pauses before each request and retries on 429 or a dropped connection.

## The checks the tool runs (any failure stops it, listing every problem)

- **Digests.** Every file is held to its digest before it is read.
- **Party sums.** For every roll call, each party's member votes sum to the roll call's own totals: the House's totals by party, and the Senate's count.
- **The measure.** Every roll call is on its measure. The one exception is an en bloc Senate vote, whose question text names the measure (reading 5).
- **The question.** Every final agreement's question is an agreement's, never a cloture, a motion to proceed, to table or to recommit.
  - A Senate "On the Motion" must be a concurrence by its question text.
  - Every override's question is an override's.
- **The override base.** Every override's outcome is two thirds of those voting, yea against nay. CRS RS22654: "two-thirds of the Members voting, a quorum being present".
- **The vetoed set.** The vetoed measures are exactly the Senate's lists, measure by measure and override vote by override vote: number, yeas, nays.
- **The Historian's counts.** Each president's regular vetoes, pocket vetoes and overrides equal the House Historian's counts.

`Tools/us_veto_mutations.sh` proves each refusal. Each case mutates a fresh copy and must stop the tool with its own message; the control reproduces this extract byte for byte.

## The readings, declared

1. **A chamber's final agreement.**
   - It is the chamber's last action, on or before the day of presentment, that agrees to the measure's text:
     - a passage by any method (suspension and en bloc passage included);
     - a concurrence in the other chamber's amendment;
     - a conference report agreed to.
   - The record's own summary line ("Passed/agreed to in House:", "... in Senate:") counts as an agreement. En bloc House suspensions carry no other marker.
   - The wordings are read wherever the record uses them, any number of asides and either capital included: "Senate agreed, under the order of 3/22/2024, having achieved 60 votes in the affirmative, to the House amendment"; "Senate receded from its amendment and concurred"; "On motion the House agree"; "suspend the rules and recede ... and concur"; "Pursuant to the provisions of H. Res. 1061, the House agreed to the Senate amendment" (reading 9); and one garbled Clerk line ("On motion that the House suspend the rules and Agreed to", H.R. 1917, 117th). The review of s802 found about twenty measures where a missed wording had let an earlier, superseded passage stand as the final agreement.
   - **The guard.** The agreement that acts last overall can never be one "with an amendment", since the other chamber must agree after it; the tool stops on any measure where it is. On the same day, the chamber agreeing to the other's amendment is the later one: the Senate's actions carry no time.
2. **The president of a decision** is the one in office on its day. The terms change at noon on 20 January (the 20th Amendment, as `records_by_date.md` derives Biden's last day); its §2 table begins in 2021.
   - The three terms are named as the House Historian's table names them by their Congresses.
   - A decision dated 20 January is the incoming president's: every one is after noon.
   - The window opens at noon on 20 January 2017. No decision of the 115th Congress falls before it.
3. **The decision.**
   - Its day is the signing, the veto, or the day the measure became law without signature. "Sent to Archivist of the United States unsigned" counts as the last: two in Trump's second term, P.L. 119-101 and 119-102.
   - A measure vetoed and then overridden is VETOED.
4. **The same-day cases** (`same_day`). Where a chamber's last agreement day carries both a roll-call agreement and an unrecorded one, the later by the record's own time is taken; an unrecorded one without a time sorts first. Four rows, all signed laws, none vetoed, so no rule's vetoes move:
   - **H.R. 3823 (115th), the House:** its morning roll 542 was on its own bill; the Senate then amended it, and the House agreed to the amendment by unanimous consent at 16:01. The final text was agreed without a roll call, and that is taken.
   - **H.R. 133 (116th), the House:** the Consolidated Appropriations Act, 2021. Two divisions were agreed by roll calls 250 and 251; the closing agreement followed without objection at 21:11:31. The closing agreement is taken, and the division roll calls are set aside.
   - **H.R. 2471 (117th), the House:** the Consolidated Appropriations Act, 2022. Divisions were agreed by roll calls 65 and 66, then the closing agreement by voice vote at 22:50:51. The voice vote is taken, the division roll calls set aside.
   - **H.R. 3684 (117th), the Senate:** the record lists the same roll-call passage twice, once without its roll number. The roll call (314) is taken; there is no real ambiguity.
5. **En bloc Senate votes.** The Senate's LIS XML gives an en bloc vote's document as one measure, while its question text names them all (S.J.Res. 27-48 of the 116th, one roll call 179). A measure is accepted where that text names it. One roll call is taken this way (S.J.Res. 37, 116th, its Senate passage by roll 179).
6. **The president's party** is the party label: R for Trump, D for Biden. Independents are outside it, and a senator caucusing with it is in no count of it.
7. **The kinds.** A measure's Senate path is read from its title:
   - "congressional disapproval under chapter 8 of title 5", or "disapproving the rule submitted by" anywhere in the title ("A joint resolution disapproving the rule submitted by ...", S.J.Res. 18 and 28, 119th): a CRA disapproval (5 U.S.C. 802(d): debate "limited to not more than 10 hours");
   - "disapproval of the proposed export / sale / transfer": an arms-sale disapproval (22 U.S.C. 2776, considered under §601(b) of P.L. 94-329, whose (4)(B) limits Senate debate to 10 hours);
   - "removal of United States Armed Forces from hostilities": War Powers (50 U.S.C. 1546a, the same §601(b));
   - "a national emergency declared by the President": national emergency (50 U.S.C. 1622(c), "voted on within three calendar days");
   - "disapproving the action of the District of Columbia Council": a D.C. disapproval (D.C. Code §1-206.04(h), debate "limited to not more than 10 hours");
   - anything else is a bill or joint resolution under Rule XXII's cloture.
8. **An independent senator's caucus** comes from `senate_seats.csv` by surname and state (R-US10 (a); Sinema's is a READING, ruling G3).
9. **A House agreement by a rule's adoption** is "no recorded vote". It takes two forms:
   - an en bloc passage under a special rule: "Pursuant to section 10 of H. Res. 1396 ... the following bills passed under suspension of the rules" (H.R. 5641, 117th);
   - an agreement to the other chamber's amendment made by the rule itself: "Pursuant to the provisions of H. Res. 1061, the House agreed to the Senate amendment" (H.R. 4366, 118th, the 2024 appropriations).

   The measure is agreed when the rule is adopted, so no roll call is on the measure itself, and the rule's own roll call is not read as the measure's. 46 rows; such a final agreement names nothing.

## What the record holds

**1,564 measures** decided from noon on 20 January 2017: Trump's first term 795 (785 signed, 10 vetoed - one overridden), Biden 649 (636 signed, 13 vetoed), Trump's second term to the record's reach 120 (116 signed, 2 law without signature, 2 vetoed). The House's final agreement was a roll call on 549 of them, a voice vote on 740, unanimous consent on 229, and on 46 no recorded vote of its own (reading 9); the Senate's a roll call on 175, unanimous consent on 1,088, a voice vote on 301. Of the 761 roll calls fetched, 748 are read: the review's fixes moved 13 finals off earlier roll calls.

**Every veto of the window is here**:
- Trump's first term: 10, one overridden (the FY2021 NDAA, H.R. 6395);
- Biden: 13, all in the 118th Congress;
- Trump's second term: 2, H.R. 131 and H.R. 504, both sustained in the House on 8 January 2026.

These equal the House Historian's counts and the Senate's lists, override vote by override vote. No pocket veto falls in the window. S. 906 (116th) is a regular veto the Senate lists as "unchallenged".
