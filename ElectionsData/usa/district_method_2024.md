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

## Live watch item

**[UNCONFIRMED] Nebraska may abolish the district method.** LB3 in the 109th Legislature is
described as amending §32-1038 to make all five electors statewide (winner-take-all). The bill
could not be fetched (same unreachable host) and its status is unverified; best secondary
pointer seen: BillTrack50 https://www.billtrack50.com/billdetail/1771479. **Verify before
modelling Nebraska as permanently district-method** — this is a live-rule expiry of exactly the
kind rule 9 warns about for seed data.
