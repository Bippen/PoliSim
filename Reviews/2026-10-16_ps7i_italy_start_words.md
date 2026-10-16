# Review - PS-7-interim: Italy's start says what it holds (2026-10-16)

A single-agent review of PS-7-interim (branch `sweep/ps7i`, commit `1b2d30f7` against `7c585d8f`): `WorldClock.NoElectionYet` and the folder card's line, `StartPoints.ModeLine`, `StartBrief`'s Italian clause and ledger row, `NationalElection.NotHeldReason` for Italy, the Parliament page's record-row slip, and the pins in `StartBriefDiagnostic` and `StartPointsDiagnostic`. Two lenses: the words' truth, and the checks' power, separability and the film. Each candidate finding was put to a refute-first test by the reviewer. The reviewer ran the compiled code out of the engine against both trees. Not a money path, so no ledger row.

## Disposition

- **DEFECT 1** (the start card's stamp and seat-bar caption still name 25 SEP 2022): not changed here. The stamp and the name are the start point's identity (SP-1) and stay, as US-1's did (§782). The caption names the election whose table is seated in place of the 2018 Camera's (E-47). It is the provenance of the seats, not a promise of a polling day. The record words the done-when narrowly: the start card's mode line names no snap election. The caption, the stamp and NOTE 5 go to Elias as one E-47 question.
- **DEFECT 2** (Italy's and France's party picker reads K-1's Sweden line "THE RIKSDAG HAS NOT YET CHOSEN A PRIME MINISTER"): pre-existing and outside this item. The line is shared by the Canvas and IMGUI pickers and allow-listed in `MetaTextCheck`, and changing it also moves France. Filed as a finding for the integrator.
- **NOTE 3** (the premise of Italy's words is not pinned): fixed in `992d862d`. `StartBriefDiagnostic` now holds that the record seats neither Italy's chamber nor its executive by date, and that its confidence rules are unsourced.
- **NOTE 4** (the snap-day check misses the ISO form): fixed in `992d862d`.
- **NOTE 5** (the ledger's "In the Camera" row, "400 SEATS · ELECTED 4 MAR 2018"): pre-existing (E-47). Goes to Elias with DEFECT 1.
- **NOTE 6** (the audit's tracking rows): draft text returned to the integrator, who writes the shared records.
- **The film stop**: taken. The film stops at `-shotstop=01a_selector_yielding`, so the canvas asserts of 01, 01f2 and 01g all count.

## The report (verbatim)

**Verdict.** The diff itself is correct. The new Italian words are true of what an Italian game does for the whole run. Every new or changed check fails on the base commit. The USA is unchanged byte for byte, and US-8 can retire the USA without touching an Italian word. Two problems survive, both in lines the diff does not touch:
- The start card still names 25 SEP 2022 in its seat-bar caption, so the done-when's "the start card" clause does not hold.
- The Italian party picker, on the frame the owed film shows, says "THE RIKSDAG HAS NOT YET CHOSEN A PRIME MINISTER".

There are also four notes.

**How I checked.** I built a probe that runs the real compiled code twice: once against the head tree and once against a `git archive` export of 7c585d8f. It replicates the six new checks and adds the start card's seat-bar caption. The gate passes on head ("GATE OK") and the worktree is clean.

### DEFECT 1: the done-when's "start card" clause is not met. The card still names 25 SEP 2022 twice (lines outside the diff)

**Evidence.**
- `BuildSeatBar` (CountrySelectorScreen.cs:657-659, called for every playable card at :611) reads `WorldClock.PickerViewOf(Italy)`. Its `Vintage = SeatedVintage(id, start)` (WorldClock.cs:715) is **Italy2022**, because `PartySystems.SeatsSourced(Italy2018)` is false (PartySystem.cs:737, E-47).
- The caption at :731-735 is built from `ElectionDayOf(id, start.Vintage)`, which returns 2022-09-25.
- The probe prints the caption as **"THE CAMERA OF 25 SEP 2022 · 400"**. The same card's stamp is "25 SEP 2022" (StartPoints.DateLine). StartPointsDiagnostic now *pins* that stamp.

**The scenario.** On 01f2_start_points with -shotcountry=Italy, the card reads:
- "25 SEP 2022 | NO ELECTION YET · OPENS 21 JUL 2022"
- "General election (snap)"
- the seat bar, then "THE CAMERA OF 25 SEP 2022 · 400"

The caption dates the Camera to an election that is in the start's future and that the game never holds. The ledger directly below says "In the Camera · 14 PARTIES · 400 SEATS · ELECTED 4 MAR 2018", so one frame gives the same Camera two dates. C3, the new snap-day check, covers the folder line, the mode line, the brief, the ledger and the reason, but not the stamp or the caption. Its message is scoped honestly to those five.

**Why it survives.** The stamp (and the name) follow US-1's precedent: §782 records "The card keeps the record's stamp and name". The USA's caption, though, is "THE HOUSE OF 8 NOV 2022", which is past. Italy's caption is a new case that no precedent covers. A record saying "an Italian start names no polling day the game does not hold … on the start card" would be false.

**Smallest fix**, one of:
- (a) No code. Word the done-when and the record narrowly: the start card's *mode line* names none. The stamp and name stay as the start's identity (SP-1, as US-1 ruled). The caption carries the E-47 stand-in table's day. Put the caption to Elias/Design.
- (b) Key the caption on `PickerView.Deviation`, which is computed and never drawn. Where the seated table is not the chamber of record's, say so instead of a day. Then expose the caption string so C3 can include it.

Do not change ModeLine's wording to fix the stamp/"NO ELECTION YET" reading: that line is shared with the USA, whose behaviour must stay exactly as it is.

### DEFECT 2 (pre-existing, not in the diff, but on the owed film's 01g frame): Italy's picker says "THE RIKSDAG HAS NOT YET CHOSEN A PRIME MINISTER"

**Evidence.** `ProvisionalLine` (CountrySelectorScreen.cs:185-187) is Sweden's K-1 text, hard-coded. It is drawn whenever `PickerView.Provisional` holds (:252-258). For Italy it holds: Draghi's cabinet is unsourced, so `SeatedGovernment.TryAt` returns Provisional (WorldClock.cs:720). The probe confirms provisional=True with cabinet FdI+Lega.

**The scenario.** 01g_party_picker for Italy reads "PROVISIONAL · THE RIKSDAG HAS NOT YET CHOSEN A PRIME MINISTER · THE CABINET MARKED IS THE MODEL'S FORMATION ON THESE SEATS". It names the wrong chamber, and it implies a prime minister's choice that no Italian game ever makes. That contradicts the new words "the government seated at the start hold[s]". France's picker shows the same line.

**Smallest fix.** Make the line country-aware (Sweden's words for Sweden; elsewhere something like "THE RECORD DOES NOT NAME THE CABINET'S PARTIES · THE CABINET MARKED IS THE MODEL'S FORMATION ON THESE SEATS"). That touches CountrySelectorScreen, a Canvas surface, but it is filmed real in this item anyway. If it is not folded in, file it.

### NOTE 3: the premise of the Italian words is not pinned

The words "the Camera and the government seated at the start hold" are hard-coded, while the USA's branch derives its words from predicates. They are true only because three things hold for Italy today:
- `RecordSeatsChamber(Italy)` is false.
- `RecordSeatsExecutive(Italy)` is false.
- `ConfidenceProcedure.RulesOf(Italy)` is Unsourced (ConfidenceProcedure.cs:34).

The follow-up the StartBrief comment itself names would turn the first two on: seating Meloni's government and the XIX Camera by their record dates. Sourcing Italy's confidence rules would turn the third. In any of those cases the brief, the ledger row and the not-held reason become false while C1–C3 stay green.

**Smallest fix:** one check in StartBriefDiagnostic: `!RecordSeatsChamber(Italy) && !RecordSeatsExecutive(Italy) && RulesOf(Italy) == Unsourced`, worded as the premise of Italy's words.

### NOTE 4: C3 checks only two date forms

C3 tests "25 September 2022" and "25 SEP 2022". The brief already carries ISO dates through `SeatingDeviation` ("on 2022-07-21", "elected 2018-03-04"), so an ISO "2022-09-25" would pass. **Fix:** add `snap.ToString("yyyy-MM-dd")` to the strings tested.

### NOTE 5 (pre-existing, on the same frame as the new row): the ledger's "In the Camera" row is false

It reads "14 PARTIES · 400 SEATS · ELECTED 4 MAR 2018". The 400 is the 2022 table's size; the 2018 Camera sat 630 (ElectionsData/italy/records_by_date.md:147). Only the brief paragraph states the deviation, and no screen draws that paragraph. Out of scope, but it sits directly under "THE START'S CAMERA AND GOVERNMENT HOLD".

### NOTE 6: three tracking rows still describe the bug as open

docs/audit/AUDIT_LEDGER.md:26 (GA-0-3), docs/audit/inventory/A_front_door.md:163 (S-02) and C_elections_office.md:143 say the card and folder promise "THE SNAP ELECTION OF 25 SEP 2022". Bring them up to date with the record.

### The film

**Changed strings on REAL frames:**
- **01_country_selector:** Italy's folder line "OPENS 21 JUL 2022 · NO ELECTION IN THIS GAME YET". All folders are drawn whatever the film's country, as §782 read Italy's on the USA's film.
- **01f2_start_points, -shotcountry=Italy:**
  - Card: the mode line "NO ELECTION YET · OPENS 21 JUL 2022", plus the unchanged stamp, name and caption from DEFECT 1.
  - Ledger: head "BRIEF · ITALY, 21 JUL 2022"; Government "Mario Draghi · CABINET NOT IN THE RECORD"; Since "13 FEB 2021"; Election "NONE IN THIS GAME YET · THE START'S CAMERA AND GOVERNMENT HOLD"; In the Camera row.
  - The Election row is 62 characters in a figure lane of 1058 units at 14 pt, so it fits on one line.
- **01g_party_picker:** no changed string, but DEFECT 2's line appears here.
- **No screen:** the brief's sentence and the not-held reason. `StartBrief.Text` has no UI caller, and `NotHeldReason` shows only for an election the Italian game never reaches. They are proved by the diagnostic alone, as with US-1.

**Stop: `-shotstop=01a_selector_yielding`**, with `-shotcountry=Italy -shotwidth=1280 -shotheight=720`.
- With 01h retired (§825), the declared groups before the warm-up are 00_main_menu+00a_settings, 01f2 and 01g (UiScreenshotDriver.cs:308, :354, :376). 01k* is Sweden-only (PartyCreationFlow.cs:25).
- 01a is the next capture, it is undeclared, and no assert follows it (:473-474). So every declared frame is captured and the canvas asserts for 01_country_selector, 01f2 and 01g all count.
- 01g_party_picker no longer fails TL-1, but stopping there still cuts 01g's own canvas assert. 01f2 would cut the subject's assert. 01c_desk also works (the §627/§629 precedent) but cuts 01c's desk asserts.
- bar_tier will not print a canvas line, because CountrySelectorScreen is untouched, and its CountrySelectorScreen shot list still omits 01f2. So this film is owed by the done-when alone.

### Checked and sound

- **Nothing changes Italy's Camera or government in a run.**
  - Polling: `TryNextPollingDay(Italy)` is false at WorldClock.cs:580, so `PollingDayToday` is never set and `CheckElection` never runs. No campaign calendar, no desk "Polling day".
  - Confidence: `RulesOf` is Unsourced, so `MoveNoConfidence` refuses and the AI motion, Speaker round, Install, InstallSuccessor and ScheduleExtraElection paths are all unreachable.
  - Seats: `SeatTheRecordOnItsDate` and `DrawRecordRow` act only where `RecordSeatsChamber` holds (the USA). `SetSeatsFromElection` is called only from `CheckElection`.
  - Within that, the player can leave the cabinet or withdraw support, and an AI supporter can withdraw (SimulationManager.cs:2801, :2877, :2988). The government's membership can shrink, but it stays in office, so "holds" stays true.
- **The stand-in.** It is `GovernmentRecord.AtStart`'s provisional formation on the seated table (FdI+Lega in the picker's view). "A stand-in for Draghi's caretaker cabinet" is accurate.
- **"Caretaker" on 21 Jul 2022** is right: records_by_date.md:51 and :155, [CAM-729], "il Governo resta in carica per il disbrigo degli affari correnti". No Senato is modelled anywhere.
- **Checks' power.** C1–C6 all FAIL on 7c585d8f and PASS on head (C5 on base: mismatched=Italy).
- **The both-directions pin over all six countries:**

| Country | Polling day held from its opening | Says none |
|---|---|---|
| Sweden | yes | no |
| Germany | yes | no |
| Poland | yes | no |
| USA | no | yes |
| France | no | yes (governing mode) |
| Italy | no | yes |

  The changed skip in the `heldElsewhere` loop is no weakening, because `namedOnly` pins NoElectionYet's set exactly.
- **USA unchanged.** Every USA string is identical between base and head. The branches are equivalent, and the ParliamentRows edit is unreachable for any other country because `DrawRecordRow` returns early unless `RecordSeatsChamber` holds.
- **US-8 separability.** Italy's brief, ledger and reason are in branches keyed on Italy and in Italy's case of `NotHeldReason`. The folder line and mode line come from shared `NoElectionYet` branches that Italy keeps. US-8 would edit only the joint `namedOnly` expression and give the USA a polling day, which C5 then demands by name. No Italian word or Italian pin moves.
- **Germany** still reaches the snap branches, so they are not dead code.
- **No other check** pinned Italy's old strings.
- **Comments.** The new comments carry no transcribed counts, and their claims about NoElectionYet, TryNextPollingDay and the diagnostic hold.
- **Bars.** Not a money path, so no ledger row is owed. The tier is simulation plus UI, so it owes the cheap bar, the simulation bar, the dry film and one filmed width.
