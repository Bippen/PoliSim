# USA — the district method (Maine, Nebraska) and the 2024 results by district [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; 2026-08-29, research agent). This is what R-EL8 required: the
statutes that CREATE the district method, cited to the standard the other five countries met,
plus the per-district results the rule consumes. `[PROVISIONAL]` until re-verified (R-K9).

⚠ **A citation correction, recorded rather than quietly fixed:** the kickoff's own working cite
for Nebraska (§32-714) is NOT the district-method section — §32-714 governs elector vacancies
and faithless electors. The district method lives in §32-710 (structure) and §32-1038(1)
(allocation). Do not ship §32-714 as the district-method cite.

## The statutes

### Maine — 2 at-large + 2 by district = 4 electors
- **Me. Rev. Stat. tit. 21-A §802** ("Representation"), Ch. 9, Subch. 5 (Presidential Electors)
  — https://legislature.maine.gov/statutes/21-A/title21-Asec802.html
  > "One presidential elector shall be chosen from each congressional district and 2 at large."
- Supporting: **§801** ("Election") — "In a presidential election year, the presidential
  electors shall be chosen at the general election."
  https://legislature.maine.gov/statutes/21-A/title21-Asec801.html

### Nebraska — 2 at-large + 3 by district = 5 electors
- **Neb. Rev. Stat. §32-710** (structure; state conventions, selection of presidential electors)
  — https://nebraskalegislature.gov/laws/statutes.php?statute=32-710
  > "One presidential elector shall be chosen from each congressional district, and two
  > presidential electors shall be chosen at large."
- **Neb. Rev. Stat. §32-1038(1)** (allocation; board of state canvassers) —
  https://nebraskalegislature.gov/laws/statutes.php?statute=32-1038
  > "Receipt by the presidential electors of a party or a group of petitioners of the highest
  > number of votes statewide shall constitute election of the two at-large presidential
  > electors of that party or group of petitioners. Receipt by the presidential electors of a
  > party or a group of petitioners of the highest number of votes in a congressional district
  > shall constitute election of the congressional district presidential elector of that party
  > or group of petitioners."
- ⚠ **Sourcing flag:** `nebraskalegislature.gov` refused connections from this environment
  throughout the session (164.119.161.105:443). Both quotes are verbatim Internet Archive
  captures of those exact primary URLs (`web/20241212015406/…32-1038`,
  `web/20260216110605/…32-710`), corroborated by the Nebraska Secretary of State's own 2024
  canvass book, which recites the rule and cites §32-1038. High confidence, but NOT a live
  primary fetch — re-verify when the host is reachable.

### The federal frame
- **U.S. Const. art. II §1 cl. 2** — "Each State shall appoint, in such Manner as the
  Legislature thereof may direct, a Number of Electors, equal to the whole Number of Senators
  and Representatives to which the State may be entitled in the Congress…" (NARA transcript:
  https://www.archives.gov/founding-docs/constitution-transcript)
- **NARA, Electoral College allocation** — https://www.archives.gov/electoral-college/allocation
  — "Total Electoral Votes: 538; Majority Needed to Elect: 270" (confirmed), and: "Maine and
  Nebraska, however, appoint individual electors based on the winner of the popular vote within
  each Congressional district and then 2 'at-large' electors based on the winner of the overall
  state-wide popular vote."

## The 2024 results the rule consumes

| jurisdiction | winner | winner votes | runner-up votes |
|---|---|---|---|
| ME statewide | Harris (D) | 435,652 | Trump (R) 377,977 |
| ME-1 | Harris (D) | 258,863 | Trump (R) 165,214 |
| ME-2 | Trump (R) | 212,763 | Harris (D) 176,789 |
| NE statewide | Trump (R) | 564,816 | Harris (D) 369,995 |
| NE-1 | Trump (R) | 177,666 | Harris (D) 136,153 |
| NE-2 | Harris (D) | 163,541 | Trump (R) 148,905 |
| NE-3 | Trump (R) | 238,245 | Harris (D) 70,301 |

Sources: Maine Secretary of State — statewide
`…/President%20and%20Vice%20President%20FINAL-Corrected%2020241205.xlsx`, by district
`…/President%20and%20Vice%20President%20by%20Congressional%20District%202024.xlsx`
(https://www.maine.gov/sos/elections-voting/election-results-data/election-results-2024);
Nebraska Secretary of State, 2024 General Canvass Book pp. 10–13
(https://sos.nebraska.gov/sites/default/files/doc/elections/2024/2024%20General%20Canvass%20Book.pdf).

**Cross-foot (what makes these numbers trustworthy):** ME-1 + ME-2 = statewide exactly for both
major candidates (258,863+176,789 = 435,652; 165,214+212,763 = 377,977); NE-1 + NE-2 + NE-3 =
statewide across **all six** candidate/write-in columns with zero residual. The Nebraska figures
were recovered by `pdftotext` from a PDF whose wrapped column headers scramble positional
extraction — they were validated by that reconciliation, not read off positionally.

**Resulting elector split:** Maine 3 Harris (2 at-large + ME-1) / 1 Trump (ME-2); Nebraska
4 Trump (2 at-large + NE-1 + NE-3) / 1 Harris (NE-2).

## Maine's ranked-choice status (the trap that isn't one, in 2024)

Maine's presidential general election IS legally an RCV contest — **§1(27-C)(D)** lists "General
elections for presidential electors" among elections determined by ranked-choice voting when 3+
candidates qualify (5 did in 2024) — but the 2024 race was decided in the FIRST ROUND, so no
rounds were run: **§723-A(2)** declares a winner outright at more than 50 % of all ballots cast
*including blanks and overvotes*, and Harris took 435,652 of 842,447 ballots = **51.71 %**.
Confirmed by the Secretary of State's own filing structure: the 2024 results separate
"Non-Ranked Choice Offices" (where President sits) from "Ranked Choice Office", which contains
exactly one race — Representative to Congress, District 2. A model may therefore treat ME 2024
as plurality WITHOUT error, but must not generalise that to future cycles.
(§723-A(7) holds separate presidential-RCV procedures that operate only if the National Popular
Vote Interstate Compact governs elector appointment — not operative in 2024.)

## The LB3 question — RESOLVED 2026-08-29 (R-EL12)

**VERDICT: NOT ENACTED. The district method stands.** Nebraska still allocates 5 electors as
2 at-large + 1 per congressional district, and §32-1038's operative sentences are unchanged.

**The bill's history, as far as it went:** LB3 (109th Legislature) was introduced 9 January 2025
by Sen. Loren Lippincott at Governor Pillen's request, to amend §§32-710, 32-714 and 32-1038 and
§32-713 (the fourth section added at `COMPLETED.md` §786 from the bill's title on the saved BillTrack50 page) so
that all five electors follow the statewide winner. Hearing 30 January 2025 ("over 75 people testified — the majority of them in opposition to one or both
measures" - the saved hearing page's words, §786); advanced to General File; **cloture FAILED 31–18 on 8 April 2025** — two short
of the 33 required, two Republicans voting against — and it was never rescheduled.
**Indefinitely postponed 17 April 2026** at the biennium's sine die. The companion constitutional
amendment **LR24CA** was never floor-debated and is **not** on the 2026 ballot; a citizen-initiative
route launched in October 2025 was **withdrawn in June 2026** before the signature deadline. The
Nebraska Secretary of State's 2026 elections page carries no notice of any change.

**Sources** (accessed 2026-08-29): the Legislature's own Unicameral Update —
https://update.legislature.ne.gov/?p=37216 (the hearing) and
https://update.legislature.ne.gov/?p=39341 (session review: the cloture motion "failed on a vote of 31-18. Thirty-three votes were needed"; LR24CA "was considered by
the Government, Military and Veterans Affairs Committee but was not advanced" — both quotations corrected to the saved
page's words at `COMPLETED.md` §786, the earlier ones having paraphrased it); action history at
https://www.billtrack50.com/billdetail/1771479 ("Indefinitely postponed (on 04/17/2026)");
contemporaneous press (WOWT, Nebraska Examiner) for the roll call.

⚠ **Sourcing gap, stated:** `nebraskalegislature.gov` / `leg.ne.gov` refused connections
throughout (ECONNREFUSED on two IPs) and the Internet Archive is blocked at the tool level, so the
official bill-history page and official statute text were **not** directly verified. Three
independent lines — the Legislature's own news service, the aggregator's action history, and press
— agree, which is why this is rated high-confidence rather than certain. §32-1038's text was
re-read from a commercial reproduction (FindLaw, "current as of January 01, 2024"); since LB3 was
the only vehicle to amend it and LB3 died, the text is unchanged.
⚠ **One propagated error not to carry forward:** the aggregator's summary calls LB3 a bill about
"seven electors". Nebraska has **five**.

**The ruling's forward half still binds.** R-EL12 requires that if this ever changes it is
implemented as a **dated rule variant** — the rule set carrying both forms, selected by election
date — never an edit that erases the district method the 2024 backtest validates against.
`ElectoralCollege.Jurisdiction` is already shaped for exactly that: winner-take-all and
district-method are two constructors over one type, so a date-selected variant adds a selector,
not a rewrite. **This finding has an expiry:** the 110th Legislature convenes January 2027 and the
Governor has said he intends to keep pursuing winner-take-all before 2028. **Re-check before
modelling any cycle after 2026.**

## Raw pages (`COMPLETED.md` §786, PS-6 US-3)

Since §786 the pages this file cites - all but those the bullets below name as not saved or not fetched - are saved byte for byte under `raw/` and registered — publisher, fetch time, basis, SHA-256 — in `president_returns.md` (NARA's constitution transcript in `records_by_date.md`):

- **The statutes, verbatim on the saved pages:** Me. 21-A §801 and §802 (`raw/district/maine_legislature_21a_801.html`, `_802.html`); Neb. §32-710 and §32-1038(1) — the Legislature's host gave no connection on 2026-10-05 (curl status 000; it refused connections in August), so the saved copies are Internet Archive captures of 2026-02-17 (§32-710) and 2026-02-06 (§32-1038), not the 2026-02-16 and 2024-12-12 captures quoted above; the quoted words are on both. §32-1038 cites Laws 1994, LB 76 as its only source. Maine's pages are the Revisor's current text ("extracted on 10/20/2025"); §802 is unamended since 1985. The RCV sections quoted (§1(27-C)(D), §723-A) are saved too.
- **NARA's allocation page** (`raw/returns/archives_electoral_college_allocation.html`) prints "538\*" and "270\*": the quotation above drops the two footnote marks. The constitution transcript is `raw/records/archives_constitution_transcript.html`.
- **The 2024 results this file consumes** are `president_by_district.csv`'s 2024 rows and `president_by_state.csv`'s Maine and Nebraska rows, figure for figure — read by `Tools/us_returns_prep.pl` from the saved workbook by district (`raw/district/maine_sos_president_by_cd_2024.xlsx`) and canvass book (`raw/district/nebraska_sos_general_canvass_book_2024.pdf`), and held to the FEC's state rows. The statewide Maine workbook cited is `raw/district/maine_sos_president_by_town_2024.xlsx`.
- **LB3:** saved - the Unicameral Update's hearing page (`raw/district/nebraska_unicameral_update_37216.html`, published 2025-01-31: the bills "heard by the Government, Military and Veterans Affairs Committee Jan. 30", "over 75 people testified — the majority of them in opposition to one or both measures") and its session review (`_39341.html`); BillTrack50's page (`raw/district/billtrack50_ne_lb3_1771479.html`: "Indefinitely postponed (on 04/17/2026)", and the bill's title "to amend sections 32-710, 32-714, and 32-1038, Reissue Revised Statutes of Nebraska, and section 32-713, Revised Statutes Cumulative Supplement, 2024" - four sections, where this file named three until §786); and the Nebraska Secretary of State's 2026 elections page (`raw/district/nebraska_sos_elections.html`), which carries no notice of a change to the electors.
- **LR24CA's 2026 ballot status** is on two saved pages: the session review says that "If approved by the Legislature, LR24CA ... would place the question of reinstating winner-take-all on the 2026 general election ballot", and the Secretary of State's 2026 elections page lists the "Ballot Measures for 2026 General Election" as "Initiative Nos. 440-442" and, for the "Constitutional Amendment passed by the Nebraska Legislature for the 2026 General Election", "LR19CA" - LR24CA is not among them (the page names those measures without describing them).
- **Not fetched:** the press named above - the cloture's date (8 April 2025; BillTrack50 lists a motion of Lippincott's dated 04/08/2025 without calling it cloture) and the vote's party breakdown ("two Republicans voting against"; the session review gives only 31-18 and the 33 needed), that LR24CA was never floor-debated in 2026 (the saved session review is of 2025), the 2025 initiative and its withdrawal; and FindLaw's reproduction of §32-1038, whose place the saved capture of the Legislature's own page takes. Three sentences above rest on no saved page and no named source: LB3 postponed "at the biennium's sine die" (BillTrack50 gives only "Indefinitely postponed (on 04/17/2026)"), "the 110th Legislature convenes January 2027", and the Governor's stated intent to pursue winner-take-all before 2028.
- **Maine's ballots cast:** the 842,447 above is the sum of the two district workbooks' TBC columns (`raw/district/maine_sos_president_by_cd_2024.xlsx`); that it counts ballots overvoted at the first rank, as §723-A(2)'s denominator does, is a READING - no saved page says whether the workbooks' "Blank" holds them (`president_returns.md`, reading 8).
