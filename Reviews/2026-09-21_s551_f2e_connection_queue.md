# Review evidence — §551 (2026-09-21): P6-F2e, the connection queue's capacity

**What was reviewed.** The uncommitted change that became P6-F2e: `EnergyConnectionQueue.cs` (new - each operator's own published queue as a catalog, the refusal, the page's sentences), `EnergyFleet.cs` (`Place` refuses a build past the queue's capacity; `CannotPlaceWhy`), `AiEnergyMinistry.cs` (the held ministry orders what fits and records a deferral), with the page, the film driver and `EnergyMinistryDiagnostic.cs`. `EnergyFleet.cs` and `AiEnergyMinistry.cs` are money paths by the tier tool's pattern; `EnergyConnectionQueue.cs` was named into it on the first reader's finding. No baseline moves: no order exists in the no-policy world and the ministry is held.

**The form.** One independent read-only reader on the change, and a second independent read-only reader on the rework. Neither ran anything. Both read the primary files the three sourcing readers had saved (Svenska kraftnät's page, the Panorama, PSE's register and its project workbook, Terna's dataset, Berkeley Lab's report).

**What the first reader changed.** It found no defect of the several-worlds class, no new save state and nothing on the simulation path with no order placed. It found: (1) **France's wind line counted 5 850 MW of tenders still to come**, which the Panorama's own glossary does not call queued, under a page sentence that said otherwise - the line went from 22 814 to 16 934 MW, on the chapter's own cells; (2) **the ministry's clip and the diagnostic's excuse had never run** - a direct case now stages it, and with France's corrected figure the forty-year run clips France too; (3) the refusal sentence said *THE QUEUE IS FULL* of a queue that was not - it now says *THE ORDER IS PAST THE QUEUE'S ROOM* unless the line is full; (4) the new file sat outside the money-path audit - named in; (5) the excuse was undated and cumulative - dated by the lead time; (6) three weak assertions - a retirement is really placed in a full line, a technology is asserted to stand in one line only, the mid-state step is staged. Two things it found in the first cut had already been fixed by the dry film before it finished reading (a step larger than a line's whole figure; the sentence's length).

**What the second reader changed.** No runtime defect. Three definitions were not true of every megawatt under them: France's dropped the glossary's third limb (*retained in a tender* - the limb the offshore megawatts enter by), **Poland's offshore row counts PRELIMINARY connection conditions by PSE's own footnote** (some 6,8 GW of the wind line; disclosed on the page and in `Made`, the figure kept as the publisher prints it, flagged for Elias), and an edit of mine made while it read had put back *50 balancing areas* where the report says 47 of 50 had active requests. Also taken: the excuse's prefix and the staged test share one builder; the ministry branches on the queue's own rule instead of a second copy of its inequality; the HOLDS line is set in the face that fits its worst case; the step is printed the plate's one way; a staging failure says it is one.

**After the second reader, and read by no one but the author:** the strings and doc comments above, `GrainMw` (the whole-megawatt grain, named), the ministry's one-line branch change (the second reader's own fix), the HOLDS line's face, and the diagnostic's shared prefix. `EnergyFleet.cs` changed in one doc comment. The ledger's rows are for the files as committed.

---

## The first reader - on the change

*The reviewer's report, verbatim.*

# Adversarial review of P6-F2e, the connection queue's capacity

**The tree moved while I was reading.** At 16:58:41 a second patch landed. It added `EnergyConnectionQueue.FullText` and `StepUpMw`, put them on the page and in the film driver, and added two assertions to the diagnostic. This review covers that state; it was still current when I finished.

Two defects I found in the 16:50 state are fixed by the second patch, and I checked the fix:
- **USA coal and nuclear were unreachable.** `StepMw(USA)` is 12 400 MW, against line figures of 3 700 (coal) and 10 390 (nuclear), and the row said "THE QUEUE IS FULL" with 0 MW standing. `StepUpMw` now offers the line's room as the step.
- **The refusal sentence on the technology line was about 140 characters.** `FullText` replaces it with a short one.

I ran nothing in Unity (a batch was live, logs stamped 17:00–17:01) and edited nothing in the repository. My scratch scripts are under `...\scratchpad\review_f2e\`.

## Findings, most severe first

### 1. France's wind line counts 5 850 MW the publisher says is not in any queue, and the page describes it wrongly — CONFIRMED
- **Where:** `EnergyConnectionQueue.cs:77–79` (`Holds`, `CapMw = 22814`).
- **Defect:**
  - `panorama2025.txt:1154–1156` reads "Les projets en développement pour l'éolien offshore représentent 3 398 MW, et les appels d'offres à venir représentent 5 850 MW".
  - The headline 9 278 MW (l. 231) is that total, since 35 078 + 13 536 + 9 278 + 606 + 139 = 58 636.
  - Internally 3 398 + 5 850 = 9 248, which is 30 MW short of 9 278. That gap is not recorded anywhere.
  - The glossary (l. 2808–2813) defines "projets en développement" as an accepted queue-entry proposal or accepted PTF, retained in a tender, or a complete request. "À venir" tenders are none of these.
- **What the player sees:** the page prints `PROJECTS WITH AN ACCEPTED OFFER OR A COMPLETE REQUEST, ALL GRIDS`. That is false for 26 % of the wind line. The caveat exists only in `Made` and the class comment, and neither is drawn.
- **Scenario:** a French player can stand 22 814 MW of wind against a published queue of 16 934 MW (13 536 + 3 398).
- **Smallest fix:** either cap at 16 934 with `Made` naming both cells, or keep 22 814 and add "OFFSHORE COUNTS TENDERS TO COME" to `Holds`. Record the 30 MW gap either way. The choice between the two is Elias's.

### 2. The ministry's new clip branch and `AssertMandates`' excuse are never exercised — CONFIRMED by arithmetic replay, not by a run
- **Where:** `AiEnergyMinistry.cs:237–248`; `EnergyMinistryDiagnostic.cs:221`.
- **Evidence:** I replayed France's share rule against the caps (`review_f2e/fr.pl`).
  - 2026 asks 16 595 MW of wind and 27 642 MW of solar.
  - 2027 asks 2 339 and 5 439.
  - Peaks are 18 934 of 22 814 and 33 081 of 35 078, so no clip occurs.
  - Italy peaks near 15.8 of 168.9 GW (wind) and 49.8 of 140.3 GW (solar).
  - Germany is billed. Sweden only retires. Poland and the USA order nothing.
- **Consequence:**
  - The clip, the deferral sentence, the `MinOrderMw` exit and the excuse have no coverage.
  - A bug in any of them passes the diagnostic as CLEAN.
  - The class comment's motivating case is Germany's single-year order from §544. That order still stands at any size, because Germany is billed.
- **Smallest fix:** add a direct case to `QueueHoldsWhatIsPublished`.
  - Pre-fill France's wind line to `cap − 1000`, then call `AiEnergyMinistry.Decide(c, 2026, 0)`.
  - Assert one 1 000 MW wind order was placed.
  - Assert one deferral containing `THE CONNECTION QUEUE HAS ROOM FOR 1000 MW` and `: WIND +`.
  - Assert standing equals the cap.
  - State in the class comment that the German order is untouched.

### 3. `RefusalFor` says "THE QUEUE IS FULL" when the queue is not full — CONFIRMED
- **Where:** `EnergyConnectionQueue.cs:140–142`.
- **Defect:** the sentence is issued whenever the order does not fit, whatever is standing.
  - 12 400 MW into an empty 10 390 MW line gives `THE QUEUE IS FULL · NUCLEAR 0 MW STAND OF 10 390`.
- **Reach:** the page now uses `FullText`, but `CannotPlaceWhy` and the ministry's deferral still carry it. The deferral contradicts itself: `...THE CONNECTION QUEUE HAS ROOM FOR 14590 MW: THE QUEUE IS FULL · WIND 8 224 MW STAND OF 22 814...`.
- **Smallest fix:** branch the wording.
  - Room under 1 MW keeps the FULL sentence.
  - Otherwise print `THE ORDER IS PAST THE QUEUE'S ROOM · <line> <room> MW LEFT OF <cap>`.
  - The diagnostic's `StartsWith("THE QUEUE IS FULL")` is only asserted in the full case, so it still holds.

### 4. The new file sits outside the money-path audit — CONFIRMED
- **Where:** `Tools/bar_tier.ps1:37`.
- **Defect:** the `$money` pattern has no name that matches `EnergyConnectionQueue`.
  - Every refusal in `EnergyFleet.Place` is decided in that file.
  - §546's own reason for naming `EnergyFleet` is "the fleet decides what is dispatched and so what is billed".
- **Scenario:** a later edit to a cap or to `StandingMw`'s filter needs no review row, and `ReviewLedgerCheck` stays green. The check's own comment describes this gap.
- **Smallest fix:** add `EnergyConnectionQueue` to the pattern.
- **Also owed:** this change's states of `EnergyFleet.cs` and `AiEnergyMinistry.cs` each need a `reviewed` row.

### 5. `AssertMandates`' excuse has no year and is cumulative — PLAUSIBLE, latent (dead code today per finding 2)
- **Where:** `EnergyMinistryDiagnostic.cs:221`.
- **Defect:** `deferred[c.Id]` accumulates over forty years. One `: WIND +` queue deferral in any year `continue`s every later statute-year check for that label, including a fleet above its figure.
- **Today:** harmless for Italy (one statute year, three-year lead).
- **Scenario:** for a multi-point path such as Germany's, if it ever gets a published queue, a 2026 deferral would excuse 2045.
- **Smallest fix:** parse the deferral's year and accept only years within the label's lead time of the statute year, or compare the shortfall against the MW actually deferred.
- The text match itself is exact. The refusal tail cannot produce a false `: SOLAR +`.

### 6. Several assertions in `QueueHoldsWhatIsPublished` are weak or vacuous — CONFIRMED by reading
- **Retirement (l. 184):** only `CannotPlaceWhy(c, tech, -1.0)` is called, and that exits at `mw <= 0`. No retirement is ever placed in a full line and standing is never re-read. If `StandingMw` lost its `o.Mw > 0` filter, a retirement would give room and nothing would fail.
- **Shared room (l. 183):** the check is real only for Poland (coal, then gas). It is vacuous for Sweden, where only nuclear is orderable.
- **Line uniqueness:** nothing asserts that a technology stands in at most one line. `LineOf` takes the first match while `StandingMw` would count the order in both.
- **Mid-state:** the page is asserted only at full and at empty. The case `1 ≤ room < step` is untested. A fix is to place `cap − step/2`, expect `StepUpMw == floor(step/2)`, and expect it to be placeable and then full.
- **What does work:** the assertions for a forgotten refusal, for landed orders keeping their room, and for a queue shared between worlds would each fire.

### 7. Text lengths at 1280 — PLAUSIBLE, needs the film read per country
- **Where:** `GameController.Energy.cs:163–172, 182–184`.
- **Counts (`review_f2e/len.pl`):**
  - Source lines: USA 151, France 149, Poland 147 characters. The longest pre-existing line of that style and width is 136.
  - USA HOLDS (bold 9 px): 120 at rest, 126 as filmed, 147 with every line filled.
  - France's and Italy's gas and nuclear lines: 90 at rest and 93 or more once anything is queued. The author's own note says "some ninety characters".
- **Gap in the film:** it fills only solar, so a queued line that also carries the NoLineText sentence ("THE PUBLISHED QUEUE HAS NO LINE FOR IT · CAPACITY BILLED, ANY SIZE STANDS") is never filmed.
- **Check:** `MeasuredLabel` clips below 8 px and logs OVERFLOW. Read those lines for the USA, France and Poland.

### 8. Lower-grade items
- **Number formats — CONFIRMED.** `Mw()` prints `772 570` and `PlateFigure` prints `772570` on the same line. `Mw`'s comment says "as the page prints figures" and promises a thin space, but the character is U+0020.
- **Germany's sentence is hard-coded — latent.** `SourceText` (l. 174–178) returns Germany's TSO sentence for `p == null` and for any billed country. It is unreachable with six ids. It belongs on the German catalog entry.
- **Ministry epsilon.** `mw > room` is strict while `Place` allows +1e-6, so a null reason can print in that window. When `room < MinOrderMw` nothing is placed, yet the sentence defers only `mw − room`.
- **`CannotPlaceWhy`'s comment is wrong in two cases.** It says "null where Place would take it". That is false for a zero order and for a retirement with nothing to retire, where the minus button silently does nothing.
- **"50 BALANCING AREAS".** The LBNL report says 47 had active requests (`qu2026.txt:334, 351`).
- **Italy offshore.** The dataset has four statuses for offshore, not five.
- **Poland hybrids undisclosed.** The MIX+OSDn+SD column (20.8 GW injected) stands in no line, so PV and wind understate. The class comment does not say so.
- **`PoliSim.slnx`** is a Unity reorder and should stay out of the commit.
- **Per-event cost.** `CapacityText` allocates a list and about twelve strings per OnGUI event, and each technology line scans the orders about three times. CPU is trivial at 108 orders. The garbage is PF-class (§525's SourceText-cache precedent).

## Questions (not tied to a line)
- The page has no withdraw control, so one click on USA nuclear now takes the whole 10 390 MW line for six years. Should the page offer a withdraw?
- Svenska kraftnät also publishes reserved capacity. "Ansökt" is strictly the pre-reservation stock. Is the choice of "ansökt" ruled?
- Does RTE's own open data carry gas and nuclear queue lines that would un-bill France?
- The equivalence "the stock of requests equals what the grid is set to connect" is authored reasoning around sourced figures. It needs a ruling, not a code fix.
  - Read as throughput it is plausible: Sweden comes to about 2.4 GW a year of solar, 3 GW of wind and 0.7 GW of thermal.
  - The figures never move across a century of play.

## Checked and found sound
- **Several worlds.**
  - `Catalog` is read-only and nothing is written at run time or keyed by id.
  - `StandingMw`, `RoomMw`, `RefusalFor` and `FullText` read the passed country's own `FleetOrders`.
  - The page passes `_playerCountry` (`GameController.Energy.cs:394`).
  - The preview clone deep-copies the orders (`SimulationManager.cs:3546`) and only lands on its copy. Nothing places on it; the one `Decide` call site is in `AdvanceTurn` behind `Live`.
  - The shadow baseline forks by a save round trip.
- **Inertness and save.**
  - With `Live = false`, nothing on the simulation path reaches the new code.
  - `Advance` and the clearing are untouched.
  - `Country.cs` and `Order` are unchanged, so no new save state.
- **`Place`.**
  - Order of checks is sound, and the retirement clamp is unaffected.
  - An order of exactly the room is accepted, including the ministry's `mw = room`.
  - Many small orders, and withdraw followed by place, behave correctly.
- **Old save past a figure.** Room clamps at 0, the sentences print the standing figure against the cap truthfully, and nothing is evicted. `Sanitise` is unchanged.
- **Page and `Place` agree in every reachable state.** Rooms are whole numbers and `up = floor(room) ≥ 1` always fits.
- **Row height matches the draw.** An uncovered country returns before the plate.
- **Line mapping.**
  - Hydro and other are never orderable, so they never reach a line.
  - Labels outside 0..6 fall out at `CanOrder`.
  - The USA's five lines are disjoint.
- **Ministry.** Infinite room never enters the clip. Re-asking the gap each year is correct because `have` includes the queue, and it produces one deferral per label per year.
- **Film loop.**
  - It restores the queue exactly by reference, including when the first step is refused.
  - At most 108 steps (Italy).
  - Germany is skipped.
  - The preview cache is unaffected because solar's lead is two years.
- **Diagnostic.**
  - No statics leak; its worlds only reach record-scope reads, and `Seed(777)` follows.
  - Every line has an orderable technology. I checked `DependableSource` by hand for Poland gas and for USA coal and gas.
- **Figures against the saved primaries:**
  - Sweden: 9 004, 4 844, 4 348 MW; the offshore exclusion; 1 September 2026.
  - Italy: decoded from `gridq/r2.json` as 140.312, 104.657 and 64.215 GW; MaxDate is 31 August 2026.
  - Poland: 34, 7.5 and 27.1 GW; the file dates. Tabela 4's 17.1 is the Suma row (0 + 8.7 + 8.4).
  - LBNL: 773, 220 and 253 GW rounded.
  - Germany: 717 applications, 270 GW, 211 GW of batteries.
  - All four sums hold.
  - Not recomputed: LBNL nuclear 10.39 and coal 3.70 (the data file is 15 MB).
- **Mapping Övrig elproduktion and inne MWE to coal, gas and nuclear.** It is defensible as each publisher's residual line. It silently includes hydro, CHP and bio, and in Poland biogas, so the caps are lenient, never strict.
- **Mojibake.** The new file shows no mojibake patterns.

## Files read
- **Repository:**
  - `EnergyConnectionQueue.cs` (both states) and its `.meta`
  - `EnergyFleet.cs`
  - `AiEnergyMinistry.cs`
  - `EnergyMinistryDiagnostic.cs`
  - Parts of `GameController.Energy.cs`, `UiScreenshotDriver.cs`, `SimulationManager.cs`, `Country.cs`, `CountryId.cs`, `EnergyMarket.cs`, `EnergyLayerData.cs`, `PoliSimWidgets.cs`, `GameController.Desk.cs`, `GameController.Health.cs`, `ShadowBaseline.cs`, `SaveGameService.cs`, `ConstantProvenanceCheck.cs`, `ReviewLedgerCheck.cs`
  - `Tools/bar_tier.ps1`, `COMPLETED.md`, `PoliSim.slnx`
- **Research:** `gq/svk_p1.txt`, `panorama2025.txt`, `gridq/pse_wzip.txt`, `gridq/r1.json`, `r2.json`, `q1.json`, `q2.json`, `dsr.pl`, `gq/qu2026.txt`, `hz_reife.txt`, `uenb_pos.txt`.

---

## The second reader - on the rework

*The reviewer's report, verbatim.*

# Second read of P6-F2e (the connection queue's capacity), after the rework

All eight reworks are in place and I found no runtime defect in the rule. I found three sourcing-wording problems (one is a regression from a 17:26 edit), one still-untested excuse path, and one plausible text-length overflow on the page.

`EnergyConnectionQueue.cs` was rewritten on disk at 17:26:15 while I was reading; every other changed file is stamped 17:12 or earlier. My line references are to the 17:26 state, in which the USA's `Whose` and `Holds` strings were shortened. `Tools/review_ledger.tsv` still holds only the s544 rows, so any digest taken before 17:26 is stale.

## Findings, most severe first

### 1. France's `Holds` omits the glossary limb that the offshore MW enter by. CONFIRMED
- **Where:** `EnergyConnectionQueue.cs:78` (`Holds`), class doc lines 23–24, and the `Made` string on line 80.
- **Defect:** The Panorama glossary (`panorama2025.txt` 2808–2813) defines *Projets en développement* on RTE's grid with three limbs:
  - an accepted *proposition d'entrée en file d'attente*,
  - an accepted *proposition technique et financière*,
  - **"ou qui ont été retenus dans le cadre d'un appel d'offres"** (retained in a tender).
  
  For Enedis and the ELDs the test is a request qualified as complete.
- The page says "PROJECTS WITH AN ACCEPTED OFFER OR A COMPLETE REQUEST". The third limb is missing.
- The 3 398 MW of offshore wind are tender-awarded farms (my reading, not the Panorama's text), so they most likely enter by that limb, and the same may hold for part of the solar line.
- The rework's own reason for excluding the 5 850 MW of tenders still to come rests on this same glossary sentence. As the class doc paraphrases the definition, neither offshore figure obviously qualifies.
- So `Holds` is not true of every MW in the wind line, and the doc is internally inconsistent.
- **Fix:** Reword within the present length. France's second line is already about 151 characters, and the USA's was cut from about 165 to about 149 at 17:26. For example: `"OFFER ACCEPTED, TENDER WON OR REQUEST COMPLETE, ALL GRIDS · RENEWABLES ONLY"` (76 characters against 83 today). Make the same change in the class doc.
- The arithmetic checks: 13 536 + 3 398 = 16 934, and 3 398 + 5 850 = 9 248, which is 30 MW short of the 9 278 headline.
- Minor: `Whose` says "(RTE · ENEDIS)", but 740 MW of the wind line are the ELDs' and EDF-SEI's.

### 2. Poland's wind line counts *preliminary* connection conditions and does not say so. Figures CONFIRMED; whether they count as "queued" is a ruling
- **Where:** `EnergyConnectionQueue.cs:88–90`.
- **Defect:** Tabela 4 (`gridq/pse_wzip.txt` 297–308) prints 0 / 8,7 / 8,4 / 17,1. pdftotext shifts the values one row, so they read:
  - applications 0,
  - conditions stage 8,7,
  - agreements 8,4,
  - **Suma 17,1**.
- The conditions row carries footnote 11: *"w tym wstępne warunki przyłączenia"* (including preliminary conditions).
- PSE's own project register of the same date (`gridq/pse_art7.xlsx`, which I parsed) lists offshore farms with 8 420 MW under agreements in force. That matches the 8,4. It lists only 1 875 MW with conditions issued (Baltic East 900, Baltica 9 975).
- So about 6,8 GW of the 24 600 MW line are preliminary conditions (the 8,7 GW conditions row less the 1 875 MW the register lists). They are issued ahead of the offshore auction and appear in no project row.
- This is the same class as the first review's France finding. The page says "CONDITIONS ISSUED PLUS AGREEMENTS IN FORCE", and `Made` gives only "17,1 GW (Tabela 4)".
- **Fix:** Disclosure at the least: `Made = "FW 7,5 GW (Tabela 3) + morskie farmy wiatrowe 17,1 GW (Tabela 4: 8,7 GW at the conditions stage, which PSE's footnote 11 says includes PRELIMINARY conditions, + 8,4 GW of agreements)"`, plus a clause in the class doc. Whether the line should drop to about 17 800 MW is Elias's ruling.

### 3. The 17:26 edit reintroduced the USA imprecision the rework had fixed. CONFIRMED
- **Where:** `EnergyConnectionQueue.cs:94`. `Holds` now reads "ACTIVE PROJECTS AT 7 ISO/RTOs AND 50 BALANCING AREAS".
- **Defect:** The report (`gq/qu2026.txt` 334 and 351) says the data "includes data from 7 ISO/RTOs and **47** non-ISO balancing areas", that data were sought from 50, and that "3 BAs did not have any active requests".
- The class doc (line 29) is right; the page string now contradicts it.
- **Fix:** Write "AND 47 OF 50 BALANCING AREAS" (+6 characters), or "DATA FROM 7 ISO/RTOs AND 50 BALANCING AREAS".
- The rest of the USA entry checks against the report:
  - 7 + 50 and about 98 % coverage (lines 163–165);
  - gas 253, solar 773 and wind 220 GW (line 1217), which match 252.83, 772.57 and 196.32 + 23.43.

### 4. The dated excuse still never executes, and no run today can reach it. CONFIRMED by reading
- **Where:** `EnergyMinistryDiagnostic.cs:267–270` and the summary comment at 227–231.
- **Defect:** `AssertMandates` asserts only capacity-path and fossil-free mandates. France is a renewable-share mandate, so its 2027 and 2028 clips never touch the excuse.
- A capacity-path clip needs target − record − landed > the line's figure. The standing orders cancel, because they count in both the fleet-with-queue and the room.
- Germany is billed, so its room is infinite. Italy's lines are 168 872 and 140 312 MW against statute targets of 28.1 and 79.2 GW. The excuse is unreachable for every country.
- The prefix format is therefore verified only by my reading. It does match the deferral: `"{Id} {year}: {LABEL} +"`.
- **Your question: can it fail to excuse a legitimately clipped path?** No.
  - The last decision that serves statute year Y is the one at Y − lead. `Build` reads the path at `year + lead`, and orders land at the boundary that makes `CurrentTurn = T + lead`. `SimulationManager.cs:2702` passes `CurrentTurn + 1` to `Advance`.
  - If that decision is clipped, the deferral is stamped Y − lead, which is what the excuse looks for. If it is not clipped, the fleet reaches the target.
  - An earlier clip with a different stamp is simply re-asked at Y − lead.
- The reverse looseness does exist. A 60 MW deferral stamped Y − lead excuses a shortfall of any size in year Y.
- The summary comment at 228–229 is now false in the present tense. It says no queue binds and "France peaks at 18.9 of its wind line", but at 16 934 MW the line binds in 2027 and 2028. It also implies the excuse has now run, and it has not.
- **Fix:** Extract the prefix builder (`ExcusePrefix(id, placedYear, label)`) and use it both in `AssertMandates` and in `MinistryOrdersWhatFits`' `StartsWith`, so the one format that is exercised is the excuse's. Reword the summary.

### 5. The USA's HOLDS line can grow about 17 % past anything the film measured. PLAUSIBLE
- **Where:** `GameController.Energy.cs:182` and `CapacityText` in `EnergyConnectionQueue.cs`.
- **Defect:** The line is drawn in the bold 9-point `label` style. For the USA it is 120 characters at rest and 126 in the solar-full frame. Those are the only states filmed.
- With one step placed in each of the five lines it is 144 characters, and with all five lines full it is 147.
- The regular-weight `small` second lines run to about 140–151 characters. The USA's was cut from about 165 to 149 at 17:26.
- Bold is wider than regular at the 8 px floor, per your layout notes. `MeasuredLabel` shrinks text to the floor and then `UiOverflowGuard` fires.
- I could not measure this. One dry-film frame at 1280 for the USA, with one step in each line, would settle it.
- **Fix if it overflows:** Draw that line in `small`, or wrap it after the third part.

### 6. One way of printing a MW figure is not yet everywhere. CONFIRMED, low
- **Where:** `GameController.Energy.cs:115` still uses `PlateFigure((float)step, 0)`.
- **Defect:** The USA reads "STEP 12400 MW" on the same plate that prints "+12 400 MW QUEUED".
- **Fix:** Use `EnergyConnectionQueue.Mw(step)` there.

### 7. `MinistryOrdersWhatFits` staging cannot pass for the wrong reason, but it can fail with a misleading message. CONFIRMED, low
- **Where:** `EnergyMinistryDiagnostic.cs:238–247`.
- **Defect:** The test needs France's 2026 wind ask to exceed 1 000 MW with 15 934 MW already queued. Those queued megawatts count toward the fleet-with-queue and so shrink the ask. The ask is 9 862 MW today.
- If the ask were 1 000 MW or less there would be no clip. The placed order would not be 1 000 MW, the deferral would be null and the line would not be full.
- That is three loud failures, all blaming the rule rather than the staging.
- **Fix:** When the deferral is null and the placed wind is under 1 000 MW, report "the staged room exceeds the ask - the clip was not exercised".
- The second call is robust. The deferred wind share keeps the gap positive, the wind ask is far above 50 MW, the room is exactly 0 (15934 + 1000 equals 16934 exactly) and the sentence reads FULL.

### 8. The ministry re-derives `Place`'s inequality. PLAUSIBLE, negligible
- **Where:** `AiEnergyMinistry.cs:241`.
- **Defect:** The ministry tests `mw > room + 1e-6`. `Place` tests `standing + mw <= cap + 1e-6`. An order within one unit of floating-point rounding of the boundary could pass the first test and fail the second.
- `Place` would then return null with no order and no deferral.
- **Fix:** Branch on `EnergyConnectionQueue.RefusalFor(country, label, mw) != null`, so both use the one rule.

### 9. Nits
- The doc comments on `RefusalFor` and `CannotPlaceWhy` say "for the row". No row draws them; the page uses `FullText`.
- The whole-megawatt grain (`room < 1.0`, `Math.Floor`) is an untagged CONVENTION.
- The class doc's line 25 was left unwrapped.
- The no-line loop (diagnostic, line 216) covers only labels 0, 1 and 2. Every catalog has wind and solar lines today.
- The diagnostic's line 209 prints "stood whole… fills it" even when that block recorded failures.
- A full line with a retirement queued reads "+27 000 MW QUEUED · THE QUEUE IS FULL · … 27 100 OF 27 100". That is a net figure next to a standing one. It is explicable but odd.
- The page has no withdraw, which predates this change. With a capacity, a filled line now stays full for the whole lead time.
- The `PoliSim.slnx` reorder is Unity noise.

## Refuted on inspection
- **Poland's "inne MWE 27,1 GW" double-counting offshore wind.** The aggregated register has no offshore column, and the project register books the farms' agreement megawatts under "inne MWE". My check:
  - The register's non-offshore "other" types at 400 kV sum to about 20.0 GW of injection (gas units 2.0 and 9.7, nuclear 5.05, other generating units 2.47, the SG type 0.77).
  - Tabela 3's 400 kV cell is 18,1 GW. With offshore included the sum would be 30.3 GW.
  - The FW cell at 400 kV is 0,6 GW, which matches the FW rows' 567.6 MW.
- So offshore wind is outside Tabela 3 and Tabela 4 is additive. The same parse shows "inne MWE" is essentially gas 15.3 GW, nuclear 5.0 GW and other generating units 2.5 GW, so mapping it to coal, gas and nuclear is well founded.
- **"17,1 GW is the agreement stage only."** It is the sum; the agreements are 8 420 MW.

## Checked and found sound

**A — the numbered reworks**
- **Item 1:** No "22 814", "22814" or "tenders not yet awarded" remains anywhere in the repo. Both occurrences of "9 278" are the disclosure itself.
- **Item 3:** `RefusalFor` is called from `EnergyFleet.Place` (a null check), from `CannotPlaceWhy` (the diagnostic, and a null check in the film driver), and from the ministry's deferral. FULL is asserted only at diagnostic line 183 and at line 247, and both lines are full at that point. The two FULL tests agree (`room < 1.0` in `RefusalFor`, `RoomMw >= 1.0` returning null in `FullText`).
- **Item 4:** `bar_tier.ps1` stays ASCII. `ReviewLedgerCheck`'s regex stops at the first closing quote, so the comment's apostrophes are harmless. The new file sits under the money roots and its name matches.
- **Item 6:** Every assertion is present and none is vacuous.
- **Item 7:** `NoLineText` is 65 characters, so the worst technology line is about 88. `FullText`'s worst is about 76, and the next-step text about 65.
- **Item 8:** Every sub-item is done except the `:115` remnant in finding 6.

**B — the diagnostic's order of operations**
- `Advance(c, whole.OnlineTurn, …)` also lands earlier lines' mid-state orders that fall due. That is harmless because lines are disjoint, and the diagnostic now asserts that.
- The shared-room check is vacuous for Sweden, where coal and gas are refused. Poland exercises it:
  - gas 4 269 × 0.85 = 3 629, above its 2 107 peak, so it is orderable from the record;
  - coal 22 965, above 15 376.
- For the USA's coal line (3 700 MW against a 12 400 MW step): half = 1 850, `StepUpMw` returns 1 850, an order of 1 851 says ROOM, and the last step fills the line. Nuclear (10 390 MW) behaves the same, with half = 5 195.
- I recomputed the country steps: 12 400, 500, 2 600, 1 500, 1 300 and 600 MW.
- The 100 MW retirement always places, because `CanOrder` implies a record above zero, and it is withdrawn before the landing.
- The new functions leave no static state: no `EnergyMarket.Clear` call, only pure `CanOrder` reads under `RecordOnly`, no fleet scope left open, and `SimulationRandom.Seed(777)` comes after them.

**C — the page against `Place`**
- With infinite room, `∞ < step` is false, so the step is offered whole, and the ministry's `mw > ∞` is false.
- With room from 1 MW up to a step, the page offers `floor(room)`, which is at least 1 and no more than the room, so `Place` takes it.
- With room under 1 MW, the step up is disabled; the only order left would be 0, which `Place` refuses as a zero order anyway.
- Each row computes `full` and `up` immediately before its own button, so shared lines stay consistent within one IMGUI event.
- A line whose figure is not a whole number would leave a sub-megawatt remainder that reads as FULL. It would break nothing.

**D — what the rework could have broken**
- **Compile:** Every `using` is present (`System` in all four files, `PoliSim.Simulation` in the page and the driver). There are no local-name collisions for `step`, `steps`, `t`, `line`, or the lambda parameters `d`, `o` and `x`. `List.Find` and `List.Exists` are fine, and `F`'s argument counts match. The `.meta` file matches the project's two-line form and its GUID is unique.
- **Film driver:** The loop terminates; Italy takes about 108 orders, the USA 63, and the guard is 4 000. Every step is withdrawn after the frame. The earlier scroll leaves the technology lines in view.
- **ROOM sentence:** It is drawn nowhere on the page. `Decision.Deferred` has no UI consumer, and the live call site discards `Decide`'s result.
- **Other callers:** `EnergyFleet.Place` has no caller that the new capacity could break. The only large order is `FleetIsTheCountrys`' 10 000 MW of wind, and it is Germany's, which is billed.
- **Saves:** Nothing new is persisted and the save version is unchanged.

**E — what I could not verify**
- The Svenska kraftnät figures (9 004, 4 844 and 4 348) are not in any scratchpad file I could find. The offshore-is-outside statement is confirmed (`gq/svk_5000.txt:27`).
- I did not open Terna's dataset.

## Files read
- `EnergyConnectionQueue.cs`, whole, in both the 17:12 and 17:26 states
- `EnergyFleet.cs`, `AiEnergyMinistry.cs` and `EnergyMinistryDiagnostic.cs`, whole
- `GameController.Energy.cs`, lines 100–220
- `UiScreenshotDriver.cs`, lines 470–560
- `bar_tier.ps1`
- `ReviewLedgerCheck.cs`, the `$money` parse
- `PoliSimWidgets.cs`, `MeasuredLabel`
- `GameController.Health.cs`, `PlateFigure`
- `EnergyMarket.cs`, lines 138–200 and 452–494
- `SimulationManager.cs`, the three fleet call sites
- `EnergyLayerData.cs`, the capacity, availability and dispatch tables
- `review_ledger.tsv`
- Sources: `panorama2025.txt`, `gq/qu2026.txt`, `gq/svk_5000.txt`, `gridq/pse_wzip.txt`, `gridq/pse_art7.txt`, `gridq/pse_art7.xlsx`

I changed nothing in the repository. My two parse scripts and the unpacked workbook are in `C:\Users\elias\AppData\Local\Temp\claude\C--Users-elias\e2343ca0-830e-4b66-89ed-89bd33443c7e\scratchpad\review2_tmp\` (`x.pl`, `y.pl`).
