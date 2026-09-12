# The backlog plan — four items and one Design ask, sequenced (2026-09-12; ruled §474)

**Status: RULED 2026-09-12 (Elias, `COMPLETED.md` §474) - the ten decision sheets below carry his words; the sequence runs as written.** Written in plan mode on the measured tree (HEAD `2e87c38`, residue 0) and installed here so the sessions that execute it read the same order. Rows opened from it live in `POLISIM_FEATURE_LIST.md` (F4-1..F4-4, PN-1..PN-3, EN-6, EN-7, CL-1..CL-4, OP-1, D20); records land in `COMPLETED.md`; this document is re-read, not re-typed - a session that lands a row marks it here in the same commit.

The items: individual tax rates by bracket; pension age and average pension payment; the energy tab; the election-and-campaign prototype; and the one Design ask they share (D20). Two BASELINE families never land in one pass; each item states what is sourced, what is authored and what is blocked with its trigger.

---

## 1. The measured state (2026-09-12)

### Item 1 - Individual tax rates by bracket

| | finding | evidence |
|---|---|---|
| **Ruled** | D-3 (2026-08-31): (c) now, (b) pluggable schedules when it runs, never (a)'s bracket tables - three of six are not bracket tables (Germany §32a EStG formula, France quotient familial, Italy three layers) | `POLISIM_TAX_SPECLET.md` head, §3 |
| **Trigger** | the cohort substrate carries an income dimension - unfired (the substrate is sex-blind and income-blind) | the spec-let's head |
| **S3, the arithmetic blocker** | a schedule on one average income is a flat rate at that average's bracket; the tax spec-let is downstream of the substrate | spec-let §4 |
| **S4 / S5 / S6** | the player moves a bracket's rate, not its threshold; revenue stays one figure per `TaxType`; Sweden's blended 52 → kommunal + statlig is BASELINE; S5 and S6 collided for Sweden until DS-2d | spec-let §4 |
| **S7** | struck (DS-1): its premise - a zero multiplier - died at C-N4 (§126), §345 and FT-5 (§372, §396) | feature list FT-1, FT-5 |
| **The substrate** | 21 age bands, counts only; seeds SOURCED (Eurostat `demo_pjan`, US Census 2024), projections GENERATED with six digests; readers of the bands: the spending drivers, the participation table, the unemployment-by-age table (FT-8), the wage-bill base, the voter groups | `Assets/Scripts/Data/PopulationCohorts.cs`, `PopulationPyramids.cs`, `Generated/PopulationProjections.cs` |
| **Income series in the repo** | `EconomyState.Gini` (a scalar, `ilc_di12`), `EconomyState.RealWageIndex` (one average), and Sweden's SCB `SamForvInk1` income-class × age × valkrets table - unwired, Sweden only | `ElectionsData/sweden/valkrets_income_by_age_class_2024.csv` |
| **Sources for six** | Eurostat `ilc_di03` (mean and median equivalised income by age band, EU-SILC - the five; mean ÷ median pins a log-normal's σ per band, a DERIVED shape); US Census CPS ASEC PINC-01 / HINC-02 (full income classes by age); SCB's table validates the shape for Sweden | web, 2026-09-12 |
| **Tax side** | `TaxLine` = (Type, Rate, IsImplemented, RateCeiling): no bracket, no schedule; revenue = rate × sourced base (`TaxBaseTable`, D-16, USA excluded); the household burden is one average; Italy's instrument BILLED (E-6) | `TaxLine.cs`, `TaxBases.cs`, `WorldFactory.cs` |

**Verdict.** SOURCED: pyramids, projections, four of five real instruments, Sweden's income-by-age table. AUTHORED: the flat seeds, `TaxTypeBaseShares`, the burden's MPC. BLOCKED by D-3's trigger only - and the trigger is a pass this plan schedules (F4-1).

### Item 2 - Pension age and average pension payment

| | finding | evidence |
|---|---|---|
| **Neither exists** | no field, lever, data folder or feature row; two exclusions only - `LawCatalog` "retirement age belongs to the pension system's own machinery"; the AI-ministry premise "moves no retirement age" | grep, whole repo |
| **The reach of 65 today** | the Pensions driver (`SpendingDriver.Elderly65Plus`: "the pension line IS a headcount times a benefit" - the payment is implicit); the old-age dependency ratio → `EconomyState.DependencyRatio` → participation sensitivity; the caseload indexation `factor = prices × driverRatio × wages` (RF-2, Socialförsäkringsbalken 58/62 kap.); participation by band (sourced 65–69, 70–74); FT-7's supply shock, FT-7's labour input = employment, FT-8's natural rate; potential's labour window is 20–64 (a seam) | `SpendingDrivers.cs`, `PopulationCohorts.cs`, `SimulationManager.cs` (the line indexation), `ParticipationRateTable.cs`, `MacroSystem.cs` |
| **Must not move with an age** | the health age-cost index's 65/80 boundaries (biological) | `SpendingDrivers.cs` |
| **The payment needs no lever** | the line already is headcount × benefit and indexes by prices × driver × real wage; a payment lever would be a second indexation. The player's lever is the line's slider; the payment is a DERIVED readout (line ÷ headcount) with a sourced seed gate | the line indexation |
| **Statutory-vs-set precedent** | `CarbonRateStatute` (EN-4e, §471): the statute moves the figure at the boundary before any override; a passed bill's figure wins; the UI says "Between decisions the rate moves as the law moves it"; the turn-start figure is held so a statutory move is not a draft | `Assets/Scripts/Data/CarbonRateStatute.cs`, `PolicyWebRenderer.cs`, `GameController.cs` |
| **Grammar a new lever carries** | a dial row with named Min/Max (`DialLabelCheck`), ten `RangeCaptions` bands whose signs match the model's re-derived effect (the coupling must exist first), the D13 slider row, a 9c plate row for a stat, a diagnostic, its own family | `DialLabelCheck.cs`, `RangeCaptionCheck.cs`, `LedgerRow.cs` |

**Verdict.** SOURCED: the bands, participation by band, the line's seeds, the inkomstindex rule. AUTHORED: the age-cost weights, the participation sensitivities. BLOCKED by no ruling - gated by cost: a BASELINE family of the largest class (budget driver + participation + natural rate + potential + tax base).

### Item 3 - The energy tab

| | finding | evidence |
|---|---|---|
| **Ruled** | S14 as it stood: "The screen after stages 2–5, on Design's board, once the redirect is recorded. Until then a diagnostic dump is the only presentation." S1–S15 ruled as written (§454) - **S14 amended by DS-4 (§474)** | `POLISIM_ENERGY_SPECLET.md` S14 |
| **Stage table** | stage 5 = the five dials mapped onto the instruments + a law category (2 sessions); stage 6 = the screen (1–2) | spec-let §7 |
| **Landed / not** | stages 2–4 with EN-3b, EN-4c/4d/4e, EN-5 (§457–§471); stage 5 NOT | |
| **The redirect** | boards 12a–12f are "a redirect no record holds" (§449) - **ruled DS-5: they are Design's answer to D19, read the D11 way; a board asserting an instruction the record does not hold stops that board, not the set; no instruction is recorded as Elias's** | §449, §473, §474 |
| **The prototype** | Design's Swedish map recorded as evidence; its foot "deferred until D12 row 1's map lands"; "instrument-first stands - one country of six with a map is a Sweden view" | §452 |
| **Finding** | D12 row 1 (the stylized six-country map) is BUILT (§268–§274): the prototype's condition is satisfied or misnamed (a Sweden zone map is D8-3's kind). Instrument-first stands either way; the return says so (DS-5) | `CLAUDE_DESIGN_ASSET_REQUEST.md` |
| **What the layer holds** | `EnergyLayer` (fleet by technology, three-block loads, SE1–SE4, six links, reservoirs), `EnergyMarket.Result` (prices, dispatch, must-run, scarcity, marginal costs per zone and block; links' flow, binding, rent; water values; CO₂; hydro shift; ETS rise), `EnergyLedger.Book` (the retail stack per class; the two ledgers with `Gap` asserted; congestion rent; levy scale), the state's energy fields, the pass-through weights | `EnergyLayer.cs`, `EnergyMarket.cs`, `EnergyLedger.cs`, `EconomyState.cs` |
| **Absent, drawn as absent** | no investment or retirement; the load does not grow; reservoir seed fill unsourced; the ETS price exogenous with no path; neighbours outside the six exogenous; the USA's ETS row 0; Germany has no energy line (KTF off-budget); FR/PL/US hydro-shift folds BILLED; the three exchange folds not fetched (T-8); Italy's seven zones and the USA's three not modelled; no quantity daily (S13) | `ENERGY_LAYER_PREMISE.md` §7 |
| **Current surface** | the Environment plate: two price rows, the mix row, a foot; nothing draws a zone, block, link, water value, merit order or ledger | `GameController.Environment.cs` |
| **Grammars to inherit** | the plate rows (9c), the distribution row, the rule waterfall (10c), the plate grid and the sparkline (small multiples), the quarterly histories, the Riksbank page (board 5f: the path with the rule on it), the election-night bridge (§450) - typed on its vote source: a generic bridge is generalised beneath it, never a forked painter | `GameController.Health.cs`, `PlateGrid.cs`, `AttributionBridge.cs` |
| **A new tab's cost** | a new `UiPalette.SystemArea` needs an RGBA32 area icon or `AreaIconCoverageCheck` fails (a real asset - a D20 row); a page under `Sectors` costs no icon; every new stat node needs a stat icon; `MetaTextCheck` forbids "§N" / "section N" on a surface | `GameController.cs`, `AreaIconCoverageCheck.cs` |

**Verdict.** SOURCED and LANDED: everything the instruments would draw. Stage 5 proceeds on §378's ruling (the one law category as its own item); the page ships structurally (DS-4) on the Sectors page (DS-4b); the board is asked in D20 on the built page.

### Item 4 - The election and campaign prototype (what a player reaches)

| station | state | evidence |
|---|---|---|
| Campaign HQ from the rail; the opening interrupt; the chips; polling; the debate (days 20/41, unannounced); election night (calls, 4a cartogram, swing, who governs, the attribution bridge) | reachable, Sweden only | `GameController.cs`, `GameController.Campaign.cs`, `CampaignRun.cs`, §205–§207, §437–§440, §450 |
| **The 26-week pre-campaign and its eight verbs** | built but unreachable - `SimulationManager.AdvanceCampaign` returns before the campaign start; only the final 8 weeks run. The largest unlisted gap; constant 5 (`CampaignCalendar.DefaultPreCampaignWeeks` 26) and constants 14–15 (offices, staff) are unjudgeable because of it | `CampaignClock.cs`, `SimulationManager.cs` |
| The 13 September calendar | films only; the played game's calendar is the turn boundary - D-23 (a), self-taken (§209); **DS-7 ruled (b) after the first play** | `SimulationManager.cs`, §209 |
| A region picker; scandals in the live run | absent (local acts go to the strongest region; an empty scandal array is staged) - **DS-10 ruled: build** | `GameController.Campaign.cs`, `SimulationManager.cs` |
| A win rule | lose-only: out of cabinet ends the game; in office continues with an ordinary morning; the verdict prints as a sentence (D-8.5's stamp undelivered) - **DS-6 ruled: keep D-5 (a); "win" is the stamp's text; the opposition question a sheet (OP-1)** | `GameController.cs` |
| Who is the player, in party terms | a stand-in seats the largest seeded party, "the PICKER is billed, not built (§119)" - **DS-6 ruled: the picker ships** | `GameController.cs` |
| A second country | Sweden the only campaignable; Germany half - a real elected chamber and verdict, no campaign, night or map; FR/IT/PL/US not implemented; D-22 (Germany's cast) after Sweden is played (DS-9) | `LiveCampaignSetup.cs`, `NationalElection.cs` |
| The twenty play-calibration constants (E-5..E-24) | never judged; every shelf verifies the values unmoved; §346 lists "one thing to look for in play" beside each | §189, §346, §380, §469 |
| D19 | item 2 BUILT (§450); item 1 STOPPED (§449) - **DS-8 ruled: keep B8's banner; a text chip from primitives now, D-8.5's art later** | §449, §450 |
| Stale register text | F-1 "CampaignRun still UNWIRED, held by C-R4b" - a gloss in two RECORDS, not in the register row; wired since §205–§207 (the trigger "a campaign the player actually runs" is satisfiable, unobserved; the row carries the note since §475). The shelf's "(Rosatellum.cs, TacticalVoting.cs)" gloss - ⚠ this plan's draft claimed `TacticalVoting` reached and `ElectoralCollege.cs` the second unwired file; **read off the check's own print at S1 (§475), the gloss was right and the draft conflated two classes**: UNWIRED 2 is `Rosatellum.cs` entire and `TacticalVoting.cs` as a wired type with its two entry points uncalled; UNREACHABLE 2 is `ElectoralCollege.cs` and `Rosatellum.cs`. Corrected §475 | `UnwiredSubsystemCheck.cs`, `NationalElection.cs` |

**Verdict.** Sweden is the only campaignable country, the twenty constants are unjudged - and half the loop that would let them be judged never runs. Closing the loop is wiring and a protocol, not mechanics.

### Item 5 - The Design ask, D20

| | finding | evidence |
|---|---|---|
| The request's form | ⭐ STATUS heads (newest first); per ask a Reconciliation ("Nothing is asked twice"), rows n of N, "What is NOT asked - generated", "Carried - NOT re-asked", constraints, "The inventory - the checks' own print" | `CLAUDE_DESIGN_ASSET_REQUEST.md` |
| The generator | `D18Inventory` runs the coverage checks in-process and writes a GENERATED block (`Tools/d18_inventory.sh`; `D18InventoryCheck` fails the bar if block and repo disagree); `DesignNotificationCheck` guards the TO DESIGN heading; `SEND_PACKAGE.md` cut by hand from on-disk digests, the LAST write before the commit (§435) | `Assets/Editor/D18Inventory.cs` |
| The rules | "generated from the coverage checks rather than recalled"; "a batch that unblocks nothing is not requested"; §427 binaries uploaded, not described (`Tools/pngcheck.pl`); sending is Elias's (E2) | the request's head, `ERRANDS.md` |
| Today's gaps | every coverage check prints zero (bar372). D20's generated rows can only come from nodes the items declare; a declared node with no asset fails the bar unless a ratchet ceiling admits it (D18's UNCONSUMED idiom) | `bar372_shelf.log` |
| Open Design rows | D-7 (board 2b, the one drawing open); D8-2 (colours, a ruling); D8-3 (valkrets map); D8-4/D8-5 (election night's paper, the stamp - behind K-1); D8-6; D-E4 (in E-3's return); D12 all built; D13 built (§267); D17/D18/D19 closed | the request's carried table, §473 |
| The paste | E-32: ONE paste carrying the D19 ask and the D17/D18 return - re-cut twice, never pasted; E-3 the return paste | `ERRANDS.md` |

---

## 2. The decision sheets - RULED (Elias, 2026-09-12; the words are his, `COMPLETED.md` §474)

| DS | question | ruled | gates |
|---|---|---|---|
| **DS-1** | Strike S7 - its premise (a zero multiplier) is stale since C-N4 / §345 / FT-5 | **strike.** "Its premise died at C-N4/§345/FT-5; a stale ruling that blocks live work is worse than none." | F4-2 |
| **DS-2** | The income dimension's form | **(c).** "Log-normal per band from ilc_di03's mean ÷ median, with national deciles printed as a cross-check, never fitted to them." | F4-1 |
| **DS-2b** | Does `Gini` become DERIVED at F4-1 | **the gate.** "Gini stays a calibrated gate with its writer unchanged; S2 must land byte-identical or it is not a readout. Gini becoming derived is its own family, later." | F4-1 |
| **DS-2c** | What the one `IncomeTax` dial does under a schedule | **uniform shift.** "One dial shifts every rate; per-bracket rows come with the schedule row's grammar, not before." | F4-2 |
| **DS-2d** | S5 vs S6 for Sweden | **one line, two layers.** "S5's constraint holds (one revenue figure per TaxType, so the decompositions don't change shape) and S6's intent holds (kommunal + statlig are two real instruments). Both, not either." | F4-3 |
| **DS-3** | Pension age as statutory-vs-set | **statute + dial**, "on CarbonRateStatute's precedent: one rule per country with its paragraph, stepped at the boundary, a passed bill's figure wins." | PN-1 |
| **DS-3b** | An age inside a five-year band | **the fraction, stated.** "Snapping to the band would make a two-year reform invisible, which is the opposite of what the lever is for." | PN-1 |
| **DS-3c** | Potential's 20–64 window under a moved age | **state the seam now.** "Potential's 20–64 window is printed as a deviation; re-forming labour input to Σ(band × rate) is its own family after B3, because it re-solves six seed potentials (§394's precedent) and must not ride inside a pension pass." | PN-1, PN-3 |
| **DS-3d** | Where the age and the payment live | **the Budget group.** "The pension line is a budget line; a seventh society family would split one fact across two homes." | PN-1, PN-2 |
| **DS-4** | S14 - hold or amend | **amend S14.** "Structural ship first in the v3 grammar, the board second, read the D11 way against a built page. The D19 way worked: real parts make a better ask than a description does. I ruled S14 and I am amending it." | EN-6 |
| **DS-4b** | The page's home | "the Sectors page now, the rail cell asked in D20 and taken only if the page proves it needs one." | EN-6, D20 |
| **DS-5** | Record the 12a–12f instructions; open the energy ask | **half taken.** "Open the energy ask on our side: yes. Recording 12a–12f's four revisions as my instructions: no — I did not give them, and I do not record instructions I did not give (the §6.3 precedent). The boards are Design's answer to our D19 and are legitimate as such: read them the D11 way, and any board asserting an instruction the record does not hold stops that board, not the set — exactly as 10d did. Note in the return that D12 row 1 is built, so the prototype's stated condition is satisfied or misnamed." | D20 |
| **DS-6** | Who is the player; what a win is | **keep D-5 (a) lose-only, build the picker.** "The player is a party leader with a seated party (R-CL1); the picker ships. 'Win' is the stamp's text — seat change plus in-government. Open a sheet, not a build: whether losing office should continue play in opposition rather than end the run — a real design question, answerable only after a campaign is played." | CL-2 (the picker), OP-1 (the sheet) |
| **DS-7** | D-23 (b) | **"(b), confirmed, after S-C3."** | CL-4 |
| **DS-8** | D19 item 1 | **"keep B8's banner; a text chip from primitives now, D-8.5's art later."** | 13a, carried in D20 |
| **DS-9** | D-22 Germany's cast | **"confirmed, after Sweden is played."** | D-22 |
| **DS-10** | Scandals in the live run | **"build, [AUTHORED-DRAFT] rate, every party equal, the seven responses as HQ chips."** | CL-2 |
| **§378** | Stage 5's dependency | **"build the law category stage 5 needs, cited, as its own item; the wider dials-only set follows P4-C3's one-category-per-session pattern and does not block stage 5."** | EN-7 |

---

## 3. The sequence

Legend: **B#** a BASELINE family (dumped `traj_<label>_s{777,424242}_t{100,500,1000}`, diffed against the previous label, explained per country) - one per session, never two. **RO** a readout pass, dumped and proven byte-identical. **UI** films only. The chains (tax, pension, energy, campaign) run beside each other; only the one-family rule serialises them. *"S1 and S-C1 proceed immediately — neither waits on anything above. One family per pass, one commit per item, one green bar, R-SP1 push, the standing list after each landed pass."*

| # | row | session (one chain of bars + a record) | depends on | family | unblocks |
|---|---|---|---|---|---|
| **S1** ✅ §475 | (sourcing) | `ilc_di03` (five, by API), CPS ASEC PINC-01 / HINC-02, SCB HE0110 already on disk → `Tools/income_prep.pl`, a data folder with a README and digests; pensions: the six statutes with paragraphs, OECD *Pensions at a Glance* (statutory and normal ages, legislated paths, replacement rates), Eurostat `spr_exp_pens` / SCB's average pension → `Tools/pension_prep.pl`; D-22's data half (the 16 Länder's votes as a catalog CSV on the Sweden generator's shape); the spec-let's stale trigger sentence, F-1's register row and the shelf's TacticalVoting gloss corrected. Nothing reads any of it. **LANDED 2026-09-12 (§475)**: `ElectionsData/income/` (three files) and `ElectionsData/pensions/` (four, the statutes extracted by pattern with their sentences), READMEs with the raw files' digests; D-22's file verified already on the generator's shape; the gloss correction landed on this plan's own line (item 4's table) | nothing - landed | none | F4-1, PN-1, D-22 |
| **S2** ✅ §479 | **F4-1** | **LANDED 2026-09-12 (§479).** the income dimension as a readout: a generator on the projections generator's pattern (digests; refuse to emit on a failed reconciliation); per band a log-normal pinned by mean ÷ median (σ² = 2 ln(mean ÷ median)), DERIVED; gates: the band mixture reproduces the national mean; its Gini reproduces `EconomyState.Gini` within a stated tolerance or prints DIVERGENT; the national deciles printed as a cross-check, never fitted; SCB's class table validates Sweden's shape (printed, not fitted); a cohort-income diagnostic; the distribution on `PopulationCohorts` (index-aligned with the counts), nothing on `EconomyState` | S1 | **RO** (`traj_f2i` vs `traj_en4e`: every field byte-identical) | F4-2; D-3's trigger fires |
| **S-E1** ✅ §481 | **EN-6** | **LANDED 2026-09-12 (§481; the stat nodes deferred to S-D20 with the inventory's regeneration - the D18 check holds the block to the repo).** the energy page, structural, in the v3 grammar (§4) on the Sectors page; the generic bridge beneath the election-night one; quarterly histories for the energy figures; new stat nodes declared under a ratchet ceiling; the ABSENT rows; films 1280/2560 × six | nothing but S1's turn | **UI** | S-D20 (real parts, generated rows) |
| **S-D20** ✅ §483 | **D20** | **ASSEMBLED 2026-09-12 (§483; batch 4 dissolved by the checks' reading - see the ask's reconciliation; the paste E-35).** D20 assembled and cut (§5): the nodes' gaps emitted, the head, the batches, `SEND_PACKAGE.md` as ONE paste, the `ERRANDS.md` row, digests last | S-E1 | - | Design's round-trip overlaps the families |
| **EN-8** ✅ §484 | **EN-8** | **LANDED 2026-09-13 (§484; rode after S-D20 by the ruling).** the carbon row's step in its own grain: `TaxLine.DialGrain` (the ceiling's hundredth rounded to ten, never under ten), `LedgerRow.Draw`'s grain and unit, the reach guard in grains, the step printed under the name; Sweden 0,735 grains per pixel at 1280 and 0,250 at 2560 against the bound of 1 (was 22 054 and 7 494 units); every film's Budget verdict under the bound | S-D20's turn (opened §478 by CL-1's film) | **UI** | every sweep film's exit code; the interrupt film's 2560 exit 0 |
| **S3a** | **F4-2** | S2(b) pluggable schedules + S1 + S5: a schedule on `TaxLine` - flat (today, the default), a bracket table (PL, US, SE statlig), §32a's formula (DE, verbatim), the barème per part (FR), three layers (IT - flat + BILLED until E-6). Revenue = Σ bands headcount × E[schedule(y)] under the band's distribution × the wage index; the coverage bridge re-solved so revenue at seed = today's to the cent (EN-4c's "re-solved to hold the anchors"); the one dial a uniform shift (DS-2c); the household burden the average effective rate over the distribution (the same figure at seed); FT-5 keeps the average rate (the marginal reading sheeted for a re-read at the source). One or two sessions | S2 | **B1** (`traj_f4s2` vs `traj_f2i`) | F4-4, F4-3 |
| **S5** | **PN-1** | the pension age: a pension statute on `CarbonRateStatute`'s pattern (one rule per country with its paragraph; stepped at the boundary before any override; a bill's figure wins; the UI caption inherited). Reach: the Pensions driver = the cohort at or above the age (the fraction inside a band, stated); the participation rates of the straddled bands move by a response BILLED to named evaluations else `[AUTHORED-DRAFT]` with the reason; FT-7/FT-8 downstream by construction. Does NOT touch the age-cost 65/80, the dependency ratio's definition, or potential's 20–64 (the seam printed as a deviation). A dial row + ten captions after the coupling exists; a pension-age diagnostic (the statutes at no policy for ten years, the carbon diagnostic's form). In the Budget's Pensions group | S1 | **B2** (`traj_pens1`) - independent of the tax chain; only the one-family rule orders them | PN-2; the AI ministry's "no retirement age" premise re-read |
| **S4** | **F4-3** | S6: Sweden's 52 retired into kommunal + statlig as one line with a two-layer schedule (DS-2d); Sweden's bridge re-solved | S3a | **B3** (`traj_f4s6`; five countries byte-identical, Sweden explained) | the Swedish schedule row |
| **S6** | **PN-2** | the pension payment as a readout: average benefit = the Pensions line ÷ pensioner headcount; the replacement rate = benefit ÷ the wage index's level (by band once F4-1 exists); a seed gate against OECD's replacement rate / the average pension (within tolerance or DIVERGENT, the definition named); a plate row on 9c in the Pensions group's projection caption | S5 | **RO** (`traj_pens2`) | - |
| **S3b** | **F4-4** | the schedule row on the screen: per-bracket rates as rows of the D13 slider family; the formula, the barème and the three layers drawn beside a bracket table on D20's grammar (structurally if D20 is unanswered) | S3a (+ S4 for Sweden); D20 | **UI** | F4 closes |
| **S-C1** ✅ §477 | **CL-1** | **LANDED 2026-09-12 (§477).** the pre-campaign made reachable: the campaign window opens at the pre-campaign start; the run steps pre-campaign days; the eight verbs priced from the constants that exist; AI parties' pre-campaign reproduces their day-0 staging - asserted: the setup at the campaign start digest-identical to the staged one, so every harness digest holds; the record's start date the pre-campaign's; save replay over the longer window | nothing - landed | none (no player in the dump; the trajectory suite byte-identical by digest) | CL-2, CL-3; entries 5, 14, 15 judgeable |
| **S-C2** | **CL-2** | the loop's closures: a region picker (a cartogram tile sets the next local act's region; the queue already carries it); the debate announced on the HQ from the calendar; live scandals (DS-10); the party picker at country selection (DS-6, R-CL1) | S-C1 | none | entries 11, 12 judgeable |
| **S-C3** | **CL-3** | the play protocol: a staged save at the pre-campaign's first day; the play sheet = §346's "one thing to look for" beside each of the twenty constants; the persisted queue as the record of the play; the two felt-verdict saves re-cut; the seed stated - K-1 (13 September) refreshes the 2022 prior. Then Elias plays; E-2..E-4 and E-5..E-24 are his | S-C1, S-C2; K-1 | - | F-1's trigger fires; CL-4, D-22 |
| **S-C4** | **CL-4** | D-23 (b): polling day on the calendar's date inside the election turn; AI countries stay on the turn, stated. One to two sessions | S-C3 played once | none | K-1's refresh lands on its date |
| **S-E2** | **EN-7** | energy stage 5: the five dials onto the four instruments (S9), the one law category cited (§378's ruling) | S-E1 | a family only if a seeded dial position is not neutral - measured by the dump | the page's instrument strip |
| **S-C5** | **D-22** | Germany's campaign on the Länder catalog and the cast (DS-9) | S-C3 played | none | a second country |
| (13a) | | D19 item 1 on DS-8: the text chip, B8's banner kept | - | UI | - |
| **PN-3** | | potential's labour input re-formed to Σ(band × rate), six seed potentials re-solved (§394's precedent) - its own family after PN-1 | S5 | **B4** | - |
| **Shelf** | | the standing list after each landed pass | - | - | - |

---

## 4. The energy page against what the layer holds (EN-6)

Instruments first, as ruled (§452): one page per country under the Sectors area (DS-4b); Sweden's zones a section on it, not a map; the Riksbank page's idiom (the path with the rule on it, the inputs as instruments); every figure the state's own.

| row | draws | grammar | source | absent, stated |
|---|---|---|---|---|
| 1 | the price with its decomposition, households and non-households: wholesale · margin · network · policy · env. tax · VAT; the bill and its GDP share | the distribution stacked bar; a sparkline from a new quarterly series | the ledger's classes | Germany's absent energy line; the levy scale where the AI moved it |
| 2 | the rule on the price: fuel ÷ efficiency + ETS × factor + O&M + adder = the block's marginal cost; scarcity where it bites | the rule waterfall (10c) per block | the result's marginal costs and adders | "the fleet and the load are static until dispatch" |
| 3 | the fleet as capacity by technology, utilisation beside; the dispatched mix against the seed's | the existing mix distribution row, doubled | the layer's capacities | no investment or retirement; the DIVERGENT thermal definition |
| 4 | SE1–SE4 as a small multiple: per zone the three block prices and loads; the six links' flows against the NTCs, BINDS where a block binds (Caution only where a cut is full - the prototype's invariant, kept); the other five one cell, "ONE ZONE · THE REAL MARKET HAS SEVEN" for Italy | the plate grid + the sparkline | the result's zones and links | the neighbours exogenous; no quantity daily (S13) |
| 5 | water value and the reservoirs (Sweden): the value per block, the balance against the store | an open band; a sparkline | the result's water values, the reservoir balance | the seed fill unsourced, the slope authored |
| 6 | the two ledgers as bridges: system cost (fuel + ETS + adders → the outlay, the inframarginal rent the gap) and incidence (households, non-households, taxpayers → generators, suppliers, networks, support, the state), the close = the book's gap, refused above 1e-9 | the generic bridge beneath the election-night one | the book's totals | - (the thesis; no grammar yet - the board's job) |
| 7 | the instruments: the ETS price ("NO PATH · 2023 MEAN CARRIED BY THE PRICE LEVEL"); the energy line's levy (a reached-by chip to Budget); the carbon tax "REACHES TRANSPORT, NOT THE FLEET"; the five sector dials "DESCRIPTIVE UNTIL STAGE 5" | reached-by chips, honesty tags | the pass-through, the levy scale | the USA's ETS row 0 |
| 8 | ABSENT rows, one each with its reason | the absent band | `ENERGY_LAYER_PREMISE.md` §7 | - |

Not drawn: a zone map (a Sweden zone map is D8-3's kind and a later ask). `MetaTextCheck`: no "§N" or "section N" on the surface.

---

## 5. Closing the loop for a player (CL-1..CL-4)

Not mechanics - wiring and a protocol: the pre-campaign runs (constants 5, 14, 15 judgeable; the AI's staging at the campaign start asserted digest-identical); a region picker; the debate announced; scandals in the live run (DS-10); the party picker (DS-6); the calendar (DS-7) after the first play; the win rule stays D-5 (a) with "win" as the stamp's text and OP-1 Elias's sheet; the protocol - a staged save, the §346 sheet, the queue as the play's record, the seed stated; register hygiene (F-1's row, the shelf's gloss); D-22 after the play.

---

## 6. D20 - one ask, its structure

**The head - the reconciliation, generated where it can be.** D12 row 1 is built (§268–§274): the energy prototype's foot condition is satisfied or misnamed - said in the return (DS-5); instrument-first stands and the prototype stays evidence (§452) - no Sweden map is asked. D13's slider family (§267) is inherited by the pension dial row and the schedule row - no new row form. D19 item 2 built (§450); item 1 on DS-8 (a text chip now; B8 kept) - carried with its three citations. Boards 12a–12f are Design's answer to D19, read the D11 way; a board asserting an instruction the record does not hold stops that board, not the set (DS-5). D-7 (board 2b) the one drawing open - carried. D8-4/D8-5 behind K-1 - carried. D17/D18 closed. "Nothing is asked twice."

| batch | what is asked | grammar it inherits | genuinely new | unblocks |
|---|---|---|---|---|
| **1 - the energy page** | §4's eight rows on the built page; above all row 6 - the two ledgers as the page's own device; the rail cell asked, taken only if the page proves it needs one (DS-4b) | 9c/10a plates, the distribution row, the 10c waterfall, the plate grid, the 5f graph-with-rule, the 13b bridge | **the two-ledger page** - the thesis with no form; the largest board | EN-6's second pass |
| **2 - the schedule row** | a new instrument class: how a schedule reads as a row; how a player moves a bracket's rate and not its threshold (S4); how a formula (DE), a barème per part (FR) and three layers (IT) draw honestly beside three bracket tables - one grammar, six honest shapes; the average effective rate as the row's figure. Asked on the SOURCED SHAPES (the README's verbatim tariffs) as the annex | the tax ledger row, the D13 slider, the 10c waterfall for the formula | **the schedule row** - no existing grammar | F4-4 |
| **3 - the pension family's one distinction** | a lever the law also moves: the law's path beside the player's figure, which stands this year (a glyph for MOVES BY STATUTE is a costed follow-up, not an asset gap) | the D13 dial row, the carbon caption's sentence, 9c's plate | only the statutory mark | PN-1's screen |
| **4 - the generated asset rows** | the checks' own print after EN-6 declares its nodes: the energy figures' stat icons (`StatIconCoverageCheck` under a ceiling lowered as delivered - the party-mark idiom); the area icon only if the rail cell is taken | the D18 inventory's emit | assets, not grammar | the ceilings back to 0 |
| **carried, not asked** | D-7, D8-2..D8-6, D-E4 (in E-3's return), D19 item 1 (DS-8), the elections (nothing new) | | | |

**Rules honoured.** Every gap row is generated (`Tools/d18_inventory.sh`; `D18InventoryCheck` holds the block); "a batch that unblocks nothing is not requested"; binaries uploaded, not described (§427; `pngcheck.pl` on receipt); the package (`SEND_PACKAGE.md`) re-cut as ONE paste carrying E-32's unpasted content (the D19 ask, the D17/D18 return, 13a's three citations) beneath D20 - the D12-under-D13 precedent (E-11) - artefacts numbered n of N with on-disk digests, the LAST write before the commit; E-32 re-cut in `ERRANDS.md` with what it unblocks; `DesignNotificationCheck` on the TO DESIGN heading; sending stays Elias's (E2).

---

## 7. Verification, per session

| session | bars | dumps | must stay green | read the result as |
|---|---|---|---|---|
| S1 | cheap 38 (`GeneratedCatalogCheck` digests, `DocumentClaimCheck`, `ResidueCheck`) | none | `ConstantProvenanceCheck` | the documents true to the tree |
| S2 | cheap 38 + sim 47 (+ the cohort-income diagnostic); `AggregationEquivalenceCheck` | `traj_f2i` vs `traj_en4e` | every field byte-identical | "N of N identical" or it is not a readout |
| S-E1 | cheap + the film bar: the overflow guard at 0, `MetaTextCheck`, `StatIconCoverageCheck` under its ceiling, `AreaIconCoverageCheck` | none | `LeverLivenessCheck` unchanged | films at 1280/2560 × six |
| S-D20 | `D18InventoryCheck`, `DesignNotificationCheck`, `DocumentClaimCheck` | - | the manifest's digests = disk | the glance prints n of N |
| S3a, S4 | sim 47; the save round trip (a format bump); the preview parity; `LeverLivenessCheck`; `RatchetSlackCheck` twelve tight; the impact ratchet 0.5074 held | `traj_f4s2` vs `traj_f2i`; `traj_f4s6` | revenue at seed identical to the cent; Italy (flat) byte-identical; S4: five byte-identical | fiscal drag per country, signed; the six diffs explained |
| S5, S6 | as S3a + `DialLabelCheck`, `RangeCaptionCheck` (signs from the model) | `traj_pens1`; `traj_pens2` (byte-identical) | Poland (fixed age) byte-identical; the health index untouched | the statutes at no policy for ten years, per country; the seed gate within tolerance or DIVERGENT |
| S-C1, S-C2 | cheap + the campaign harnesses' digests + the films | none | every harness digest identical (the staging assertion); `PlayerReachabilityCheck` 0; `UnwiredSubsystemCheck` at 2 | the pre-campaign changes nothing the harness measured |
| S-C3 | the staged saves load | - | the save version unchanged | Elias's sheet: twenty lines, one each |
| S-E2, S-C4, S-C5 | cheap + sim; `EnergyLayerCheck` gates 6–10; Germany's night filmed | as measured | `PartyMarkCoverageCheck`, `D18MarkAssignment` | - |

Each session: one commit, one green bar, the R-SP1 push, one record; the standing list after each landed pass.
