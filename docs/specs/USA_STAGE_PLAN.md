# PoliSim — PS-6, the USA: the plan (spec + work plan for Code and Design)

**Status: PLAN, put to Elias 2026-10-04; R-US1 to R-US13 RULED 2026-10-05 (Elias's ruling F8: as recommended, R-US6 and R-US9 as he amended them).** Elias's instruction: *"Next: PS-6 (the USA), planned before any code, as you propose."* The rulings in §4.1 are ruled; those in §4.2 are asked later, each with the measurement that informs it. Every date, rule and figure is sourced by Code before use: where this plan names a fact it names what to look up, and a fact marked *from memory* is unverified and is read before anything depends on it. A bare § number is a `COMPLETED.md` record; a spec's own section is named with its document. The code facts in §2 are a reading of HEAD `5edca04` on 2026-10-04 (TRACKING: they go stale as PS-6 lands); this plan carries pointers, never line numbers or counts of code.

**Built on:** `POLITICAL_SYSTEM_SPEC.md` (§1, §4, §6, §7, §9 stage 6, §10, §11, §12); `START_POINTS_AND_PARTY_CREATION_SPEC.md` (§1.1, §2.6, §4 S6, §7 decision 10, §8); the PS-6 + SP-6 and D-US rows of `POLISIM_FEATURE_LIST.md`; ruling B7 (§759); §618's ruling 4; and Elias's rulings E1–E4 of 2026-10-04, applied to the USA here — E1's per-candidate factor, E2's "only spending stays in the budget act" and "follow the most recent practice", E3's nights asked together, E4's backtest of a veto rule against the real record.

**Precondition, met.** E1–E4 landed before this plan and are committed with it (§772–§776): they extend files PS-6 extends (`PresidentialElection.cs`, `PresidencyOfRecord.cs`, `PresidentialReferenceWorld.cs`, `SimulationManager.cs`, `ParliamentSystem.cs`, `BudgetBill.cs`, `PartySystem.cs`, `WorldClock.cs`, the `GameController` partials); their reviews are §772's (E1) and §773's (E2). By E2, Poland's 2023 coalition declarations are sourced by read and the formation reads them (§776). This plan is PS-6's first record (§777, US-0); its code starts on that committed tree with green bars.

---

## 1. What PS-6 delivers

### 1.1 The row

`POLISIM_FEATURE_LIST.md`, **PS-6 + SP-6** (Code; needs PS-5; done when *"playable as either party's nominee"*):

> "**The USA**: the state-by-state campaign, the presidency, the Senate, midterms, term limits; your own nominee or a third-party run; **the federal debt limit as a dated statute** (Elias's ruling B7, 2026-10-02, `COMPLETED.md` §759; read in `ElectionsData/usa/debt_limit.md`): **$41,103,995,660,231** since P.L. 119-21 § 72001 (approved 4 July 2025, *"is increased by $5,000,000,000,000"* on the $36,103,995,660,231 reinstated 2 Jan 2025 …); at the limit extraordinary measures buy *"a few weeks to several months"* (CRS IF10292 v22, 21 Sep 2026); then Congress raises or suspends it (the CRS also names abolition), or **the game forces spending down to revenue** - the game's own rule, ruled; the CRS reports outside projections (June 2026) of debt **reaching** the limit in early to mid 2027, a **binding** limit several months later"

**D-US** (Design; needs PS-6; done when "answered → built"): *"The Electoral College map, the veto, a Congress view."*

### 1.2 The specs

`POLITICAL_SYSTEM_SPEC.md`:

- §1: *"In the United States the player is the party's presidential nominee, and the office is the presidency."*
- §4, the USA's row: *"president via the Electoral College"*; *"House, 435; **Senate, 100** (new)"*; *"5 Nov 2024, scheduled"*; *"standard run-up"*; seats *"exact (EC, with ME/NE)"*; campaign *"states and cast owed"*. The government of record at the start, *"Biden's administration and the 118th Congress"*, is *"verified and dated by Code from primary sources"*.
- §6, whole:
  - *"**The player is a party's presidential nominee** — Democratic or Republican. Third parties are not playable: there is no Electoral College path to show them."*
  - *"**The campaign is fought state by state** on the Electoral College allocator that already reproduces 2024 exactly, including Maine's and Nebraska's district methods. The House is elected the same day (the chamber exists) and a third of the Senate with it."*
  - *"**The Senate is new and required.** US legislation lives or dies in it, and a House-only Congress would misrepresent the country. 100 seats in three classes, sourced by state; the cloture threshold on legislation and the budget-reconciliation exception, sourced from the Senate's own rules."*
  - *"**Winning makes the player president.** Presidential powers are those the model can honestly hold: proposing the budget (Congress passes it, through both chambers); signing or vetoing bills, with the two-thirds override in both chambers; appointments (the Fed-chair machinery exists). Divided government is the mechanic: a hostile House or Senate is what makes the presidency hard."*
  - *"**Midterms every two years** — the whole House and a Senate class; the player's party fights them."*
  - *"**Losing does not end the run.** A defeated president may run again four years later; the two-term limit binds the person, not the run: the player picks the party's next nominee - the Vice President by default - who campaigns on the outgoing record."* (amended by F8, R-US6 (b); until then: *"the constitutional two-term limit ends the run when it binds"*)
- §7: *"A run ends when the player's party falls out of parliament, when the player ends it, or — in the USA — when the player declines to run again; the term limit binds the person, not the run."* (amended by F8, R-US6 (b)) After the first election night the game shows *"the real result and the government that actually formed"* beside the player's.
- §9: *"**Stage 6 — the USA.** §6 whole: the state-by-state campaign, the presidency, the Senate, midterms, term limits."* — *"Elias may move the USA forward."*
- §10: decision 3 (*"The five other countries hold their real calendars and are simulated from the start date"*), decision 5 (AI casts derived from CHES by a stated rule), decision 6 (*"The US player is the party's nominee of record; third parties are not playable."*), decision 8 (*"The Senate is required for the USA and Italy to be playable"*).
- §11: *"the US Senate's composition by class and its cloture and reconciliation rules; the US veto override and term limit … every item fetched, verified and dated, or billed."*
- §12: *"the US presidency (the Electoral College map, the veto, a Congress view) … Asked against built pages, the D19 way."*

`START_POINTS_AND_PARTY_CREATION_SPEC.md`:

- §1.1: *"USA | presidential, November 2024 | a presidential nominee (or your own, §2.6) | its stage (6)"*; *"A later option, not planned here: the US midterms as their own start point, with the player as a congressional party's leader."*
- §2.6: *"… the USA offers two forms: **your own nominee** — a leader the player creates, at the head of the Democratic or Republican ticket, with positions inside that party's range; or **a third-party run** — the Grassroots origin in the hardest form, where the Electoral College shows honestly how far a third party is from a single electoral vote."*
- §4: *"S6 — the USA's two forms (§2.6), with the USA's stage."* §7 decision 10: *"The USA offers your own nominee or a third-party run."* §8: *"the US nominee and third-party ballot-access rules by state."*

### 1.3 Standing rulings PS-6 honours

- **§618 ruling 4:** *"Every election on a country's calendar inside a run is simulated once that country's model exists; until then its chamber and head of state hold as of record, and its view says so."*
- **§627 ruling 1:** the USA's presidential card draws the House — *"divided government being the presidency's central constraint"*.
- **§629:** a presidential system's government of record is the president and their party; its stated risk, *"a presidency wearing a cabinet's words"*, is owed to stage 6.
- **B7 (§759):** *"Yes, in PS-6, as a dated statute. The limit is $41.1 trillion since P.L. 119-21 (July 2025, +$5.0 trillion). At the limit, extraordinary measures buy a few months; after that Congress raises or suspends the limit, or the game forces spending down to revenue. CRS IF10292 (21 Sep 2026) projects the limit binding in early to mid 2027."* — with §759's two corrections (the $41.1 trillion lives in the statutes' notes; the CRS distinguishes *reaching* from *binding*).
- **E1:** a candidate's first-round share = the party's support in the game's own poll × a candidate factor, *[FITTED]* once to the record against the game's poll on that date, *"so a world that follows history reproduces"* the result; candidates with no party row *"carry their fitted share as their base"*; *"Future fields start at factor 1.0"*; *"per-candidate data, not a second free parameter"*; the alternative of seating the real winner rejected because it *"would take the 2025 race away from the player"*.
- **E2:** *"only spending stays in the budget act"*; *"follow the most recent practice"*; a rate moves only through its own act, *"whoever proposes it"*.
- **E3:** Poland's presidential night folded into D-PL's ask (D-PL's seventh question, §771) — a country's nights asked together.
- **E4:** *"A veto on all 43 of 43 statutes needs checking against the real record."* E4's form — the confusion matrix, the hit rate, the precision, every miss — is `docs/generated/VETO_B1_BACKTEST.md` (§775).
- **F3:** a statute is at risk where the president's backing party did not vote for it, and a seeded draw at the president's own rate on the record decides; *"The slip shows the veto risk as a percentage."* - the form US-10 measures the US veto against before anything goes live.
- **F4:** a fit is held on *"the world that follows history"*; a played game's count moves with its play (risk 17).
- **F8** (2026-10-05): R-US1 to R-US13 as recommended, with R-US6 (b) and R-US9's shutdown as Elias amended them (§4.1).

### 1.4 Two conflicts, settled by F8

1. **Third parties.** Decision 6 (*"third parties are not playable"*) against §2.6, decision 10 and the row (*"your own nominee or a third-party run"*); the row's done-when names neither form. → **R-US4, RULED (a)**: PS-6 builds the major-party nominee and, late, your own nominee (US-33); the third-party run is its own later row, **SP-6b**. Both specs say so.
2. **Every country's calendar.** Decision 3 and §618's ruling 4 owe every modelled country's elections in every run; the build holds only the player's country's — Germany's and Poland's included. → **R-US5, RULED (a)**: the US count is a pure function of a world's state here (US-8); every country on its own calendar is one later cross-country row, **PS-8**.

### 1.5 What "playable" means in this plan

- **Checkpoint 1 (US-8):** a US game holds its 2024 presidential election, state by state through the Electoral College, and the winner takes the oath; Congress is seated as of record, by date.
- **Checkpoint 2 (US-16):** the House and a Senate class are elected on the same day; the Senate exists (decision 8).
- **Checkpoint 3 (US-25), the row's done-when:** *playable as either party's nominee* — bills pass or die in both chambers, the veto and its override work, the budget goes through Congress, the debt limit binds on its date.
- **The close (US-36):** midterms, the term limit, the Fed chair, the campaign on the states, your own nominee, the night and the boards, filmed real.

---

## 2. What the repo already has for the USA

### 2.1 Built and wired

| piece | the USA at HEAD | where it lives |
|---|---|---|
| Roster | REP and DEM generated from GPS 2019 (`UsGps2019`; the file out of tree by G7, US-32 §837): V4/V6 economic and social, people-versus-elite on V8 (a reading); no general left–right or EU item; V9's salience HELD (G7) | the USA's roster in `Assets/Scripts/Data/PartySystem.cs`; `ElectionsData/positions/party_positions.md` |
| Casts | DEM grassroots, REP professional, by §695's rule (V9 onto the salience would cast REP populist - held, G7, §837) | `CampaignCasts.Of` |
| The House's seats | the 2020, 2022 and 2024 elections' tables — election-day figures `[HH-DIV]`, not sworn counts | `PartySystems.SeatsAt` (vintages `Usa2020`, `Usa2022`, `Usa2024`) |
| Chambers of record | the House by Congress, 117th–119th, dated by the 20th Amendment; **no Senate anywhere in code** | `WorldClock.Chambers` |
| Executive of record | Biden (DEM) to noon 20 Jan 2025, Trump (REP) from it, each a presidency whose cabinet is its party; installed at the start | `WorldClock.Governments`; `GovernmentRecord.AtStart` (§629) |
| The start | one playable card, PRESIDENTIAL ELECTION, opening at the standard run-up before 5 Nov 2024 (the 26 + 8-week window is `[AUTHORED-DRAFT]`); the derived brief names the president and the House majority and promises the polling day; the card draws the House (§627) | `StartPoints.For`, `WorldClock.StartDate`, `WorldClock.StartLine`, `StartBrief`, `CountrySelectorScreen` |
| The economy | the seed vintage's, its offset to a 2024 start stated; the debt seed's perimeter unstated | `WorldClock.SeedVintageLine`; `WorldFactory` |
| The Fed | fictional chairs, the Taylor rule plus the chair's bias; the chair appointed by the governing party on the turn cadence (CL-4's stated exception) | `FederalReserveSystem`; the chair selection in `GameController` |
| The AI's fiscal rule | the FRA 2023 caps and a sequester, triggered by the debt ratio's trend — "the debt-limit logic", not the dollar limit; the sequester's size `[AUTHORED-DRAFT]`, borrowed from the EU's | `AiFinanceMinistry` (`AiFinanceMinistry.UsSequesterPercentOfGdp`) |
| The economic vote | a presidency's coefficients, unified and divided, "divided" read from the House; the congressional figures sourced but not in code | `EconomicVote`; `docs/reference/ECONOMIC_VOTE.md` (Table 9.1) |
| The pre-start record | from the 2020 election (BLS U-3, CPI-U); "never judged" — no US election on the calendar (§709) | `Assets/Scripts/Elections/Generated/PreStartRecord.cs` |
| Statutes in force | the federal tax schedule, the minimum wage, the pension age (Social Security Act §216(l)), the fiscal year from 1 October (no page behind it); the slip's note naming 31 U.S.C. 3101(b) | `TaxSchedule`, `MinimumWageRates`, `PensionAgeStatute`, `FiscalYearData`, `FiscalRules.NationalNote` |
| The budget path | unsourced: the government's bill voted alone in the one chamber; where the AI governs the player's country its budget is tabled as the government's bill (§632) | `WorldClock.BudgetProcedureOf`; `SimulationManager.TableGovernmentBudget` |
| Salience | Gallup's most-important-problem row | `ElectionsData/salience/issue_salience.md` |

### 2.2 Built, wired to nothing — or built for another country and reusable

- **The Electoral College allocator** (`Assets/Scripts/Elections/ElectoralCollege.cs`, R-EL8, §49): 538 electors, 270 to elect, winner-take-all, Maine's and Nebraska's district method, from the statutes; its own header says *"PURE FUNCTIONS, WIRED TO NOTHING"*; it deliberately models no faithless electors, no contingent election, no NPVIC and no ranked-choice count. Its only caller is the Editor harness `SeatAllocationBacktest`, which reproduces 312–226 from arrays typed from `state_ev_2024.csv` (§419 owes a generated table). `UnwiredSubsystemCheck` counts it as unreachable.
- **Poland's presidential machinery:** the contest store on the Country and its save; `PresidencyOfRecord` and `TwoRoundElection` (Poland's only); `PresidentialElection` (the day hook, the player's country only); `PresidentialVeto` (the Sejm's constants; `PresidentialVeto.Applies` is Poland's, **and Poland's statute-act split keyed on it until US-20 (§798), which moved the split onto `WorldClock.StatutePartsAreActs`; the veto gate and `ChamberVerdicts.Veto` stay on `Applies`** — in the simulation, on the Budget page, in the controller, in `ChamberVerdicts.Veto` and in the film driver's staging); E1's eve-built `PresidentialReferenceWorld` (§772); the model card's generated block (`PresidentialVoteBacktest.WriteReadings`); E4's `Tools/veto_b1_backtest.pl` (§775).
- **Germany's regional machinery:** §689's normalised uniform swing (`RegionalVoteModel.RegionalSharesByUniformSwing`); the Länder reader (`GermanRegions`), its generated catalog and its re-read (`GeneratedCatalogCheck`); the staged campaign (`LiveCampaignSetup.TryFor`); candidacy as reach (§699); the night's tile view (`LaenderTileView`).
- **W-D1's per-region count** (`ElectionDay`, over a campaign's regional mobilization): pure and wired to nothing; only Editor harnesses call it.

### 2.3 Sourced on disk, not built (`ElectionsData/usa/`)

- **`records_by_date.md`**, raw pages under `raw/records/` and `raw/executive/` with SHA256SUMS (re-verified 2026-10-04): the House and the Senate by Congress; the Senate's three classes (Wayback captures of senate.gov); presidents and vice presidents by date; party chairs and congressional leaders; its §4 quotes Art. I §7 (the veto, the two-thirds override, the ten days), the 20th and 22nd Amendments, 2 U.S.C. §§1 and 7, Senate Rule XXII ¶2 (three fifths of the senators *"duly chosen and sworn"*), 2 U.S.C. §641 and §644. The saved §641 page also carries §641(a)(3) — *"the statutory limit on the public debt"* may be changed by reconciliation — and §641(g), which bars reconciliation from Social Security title II; §644(b)(1)(F) makes such a provision extraneous. Gaps G1–G12 are listed there (G7: no per-state Senate table; G10: the Byrd waiver unquoted).
- **`debt_limit.md`** with `raw/debt_limit/` (B7): P.L. 119-21 §72001 — *"An Act To provide for reconciliation pursuant to title II of H. Con. Res. 14"* — 31 U.S.C. 3101, Treasury's MSPD table 2, CRS IF10292 v21 and v22.
- **`returns_2024.md`, `state_ev_2024.csv`, `district_method_2024.md`** — filed 2026-08-28/29 from an agent's return with **no raw bytes**: the FEC workbook, the Clerk's statistics, the Maine and Nebraska canvasses and statute pages are cited by URL only; 12 states carry winner shares; no jurisdiction-by-candidate counts for all 51.
- **In the saved NARA transcripts, not extracted:** the 12th, 17th, 23rd and 25th Amendments; Art. I §3 cl. 4 (the Vice President's vote), Art. I §7 cl. 1 (revenue bills originate in the House), Art. I §9 cl. 7, Art. II §1 cl. 2 and cl. 5, Art. II §2 cl. 2 (advice and consent).
- **Elsewhere:** Statutory PAYGO (`ElectionsData/rules/raw/`); the FRA 2023 caps, out of tree, from a secondary publisher.

### 2.4 Missing — the switches PS-6 turns

- No polling day: `WorldClock.TryNextPollingDay` gives the USA none and `WorldClock.PollingDayBasis` has no US statute; `PollingDayDiagnostic` asserts it.
- No vote model: `PartySystems.TryElectorate` and `PartySystems.TryRealHistory` have no USA case, so `NationalElection.TryPredictShares` fails and the count records NotImplemented, its reason (`NationalElection.NotHeldReason`) naming the House alone.
- No state layer, no state catalog, no US branch in `NationalElection.DeriveRegional`; no staged campaign; no night (`ElectionNightFromModel.Available`).
- No confidence rule (`ConfidenceProcedure.RulesOf` is Unsourced): a formation runs over the House on polling day.
- No Senate, cloture, reconciliation, US veto, US budget procedure or dollar debt limit.
- No midterm, no term limit (Trump's 2017–2021 term is in no table), no statutory Fed-chair term.
- Party creation is Sweden's only (`PartyCreationFlow.Offered`); no state geometry (`MapRenderer` holds one US centroid).
- No AI country votes in any run: `SimulationManager.PollingDayToday` is the player's polling day, and Poland's presidential round runs only in a Polish game.

### 2.5 Wrong or misleading today

1. **A US game advertises an election it never holds:** the card is playable and the brief says *"Polling day is 5 November 2024"*, while Poland's and France's presidential cards are locked.
2. **Biden and the 118th House govern for the whole run:** nothing seats the 119th Congress on 3 Jan 2025 or the president of record on 20 Jan 2025.
3. **A presidency wears a cabinet's words:** the president's party is "the government" and every other party an opposition with nothing to introduce, so a REP player at the start — the House's majority — reads IN OPPOSITION (§629).
4. **Bills and budgets pass by the House alone:** no Senate, no filibuster, no veto.
5. **The AI's "debt-limit logic" is a ratio trend**, no dollar limit exists, and the debt seed's perimeter is unstated — the mapping read it at about $4.0 trillion below Treasury's debt subject to limit at the seed's vintage (DERIVED; bridged per PN-4 before anything compares them).

---

## 3. The items in order

**The grain, every item** — PS-5's: source, prove on the record, rule, then wire.

- **Sourced by read** into `ElectionsData/usa/`: raw bytes with SHA256SUMS; a large set goes out of tree under `PoliSim-captures/sources/` with its extracts in the repo (§757's B3 precedent). A refused host is BILLED with what is missing, never estimated.
- **Figures generated, never typed** (§419): a committed `Tools/` script writes them and a check re-reads them.
- **Premises named:** DECLARED or ruled; an authored value is `[AUTHORED-DRAFT]` with its line.
- **Measured before live:** an Editor instrument pinned in the cheap bar while not live asks the ruling it informs, with its table. Nothing measured goes live unruled; a premise that fails is STOPPED, with the measurement that killed it.
- **Inertness proved, not read** (§400): Sweden, Germany and Poland byte-inert by the dump diff. The trajectory dump opens on Sweden's start with no player country (`TrajectoryBaselineDump`), so a player-only US item should leave the sentinel alone — proved by the diff, not assumed. An item that moves it is its own BASELINE family, explained per country (`Tools/traj_diff.pl`); two families never share a pass.
- **Reviews:** a commit the tier tool marks as touching a money path (`Tools/bar_tier.ps1`: SimulationManager and BudgetBill among them; ParliamentSystem is not one) gets its `Reviews/` report and its `Tools/review_row.ps1` row before the bar.
- **Tiers:** a commit touching `ElectionsData/` or a `Tools/*_prep.pl` is simulation tier and owes the simulation bar even when nothing reads it — batch the sourcing commits.
- **Films:** dry-film sessions declared in `Tools/film_scope.tsv` before they run; a Canvas surface an item touches (`CountrySelectorScreen`, `SigningScreen`, `ElectionNightScreen`) is filmed REAL (§578).
- **Shared checks re-pinned in the item that changes what they assert:** `PollingDayDiagnostic`, `StartBriefDiagnostic`, `PlayerRoleDiagnostic`, `ConfidenceDiagnostic`, `GovernmentBudgetBillDiagnostic`, `FormationSweepDiagnostic`, `SaveMigrationCheck`, `PreviewParityDiagnostic`, and `UnwiredSubsystemCheck`'s ratchets (lowered to what the bar measures, never to what was hoped). `StartPointsDiagnostic`'s one-US-contest assertion stands: the midterm start is out of scope.
- **One record per item, short form;** one Unity job at a time (§648); sourcing agents run ahead of the code.

**The order at a glance.**

| phase | items | what it gives |
|---|---|---|
| A — the plan, and an honest start | US-0 – US-2 | the plan in the repo; the US card says what it holds; the record's dates inside a US run |
| B — the presidency's proofs | US-3 – US-5 | the returns by state, read; the Electoral College exact on four elections; the state and national vote measured |
| C — the presidency held | US-6 – US-8 | E1's factors for the USA; the oath; **checkpoint 1** |
| D — the other proofs | US-9 – US-14 | campaign geography, the veto record, the House count, the Senate by date, the midterm record, the debt limit — each asks its ruling; any order within their needs |
| E — Congress seated and elected | US-15 – US-17 | the Senate; the three contests on one day (**checkpoint 2**); D-US part one asked |
| F — Congress at work | US-18 – US-25 | both chambers, the roles, Poland re-keyed, the veto, the budget, the president's pen, B7 (**checkpoint 3**); D-US part two asked |
| G — later cycles, the campaign, the nominee | US-26 – US-33 | history rolled forward, midterms, the term limit, the Fed chair, the campaign on the states, GPS read, your own nominee |
| H — the night, the boards, the close | US-34 – US-36 | D-US built; PS-6 closed on real films |

**Size:** 37 records, against PS-5's roughly sixteen (§720–§771). Phase D's sourcing runs beside Phase C; Elias may move any phase.

### Phase A — the plan in the repo, and an honest start

**US-0 — PS-6 planned: this plan into the repo (records only). Size S.**
This document installed under `docs/specs/` (suggested `USA_STAGE_PLAN.md`); the PS-6 + SP-6 row points to it and lists US-0 to US-36 with their needs; R-US1 to R-US21 listed under it with the item each blocks; the DECLARED premises named. `POLITICAL_SYSTEM_SPEC.md` §6 and `START_POINTS_AND_PARTY_CREATION_SPEC.md` §2.6 point to the plan, their third-party clauses marked in conflict until R-US4 is ruled (§643: a design a later session depends on lives in the repo the day it is made).
*Needs:* E1–E4's records committed with green bars.
*Done when:* one § carries the plan, its deferrals and its ruling list; the row carries references only — no line numbers, no counts, and no backticked `Type.Member` for a member not yet built (`DocumentClaimCheck` reads the root documents); the document batch is green; the reply to Elias puts R-US1 to R-US13.

**US-1 — The US start says what it holds. Size S.**
Until US-8, the folder card's line (`WorldClock.StartLine`), the start card's mode line (`StartPoints.ModeLine`), its brief (`StartBrief`'s presidency branch) and `NationalElection.NotHeldReason` say that no US election is modelled yet and what holds instead — the president and the House seated at the start, no Senate modelled, until US-2 seats the record by its dates. The card stays playable (it is Ruled, §618); the words change, not its state. **Built: `COMPLETED.md` §782.**
*Needs:* US-0; E2's Polish declarations read.
*Done when:* `StartBriefDiagnostic` pins the new clauses and drops the polling-day clause; the cheap and simulation bars are green (Elections tier); a REAL film of the start card at 1280 (Canvas text drawn by `CountrySelectorScreen`).

**US-2 — The record by date inside a US game. Size M.** *(R-US1)*
In a US game, whatever the game does not yet elect is seated on its record's date: the 119th House at noon on 3 January 2025 and, until US-8 lands, the president of record at noon on 20 January 2025 (20th Amendment §1, on disk). A chamber of record not yet sourced (the 120th) is not seated: the 119th stands, and the view says so. Built as one dated transition on the day loop — a sibling of `ParliamentSystem.SetSeatsFromElection` that seats a record, and a government change read from `WorldClock.Governments`. US-7 reuses it for the oath, US-15 for the Senate of record, and US-16 retires it chamber by chamber as the game elects them. The player's country only; R-US5 owns the rest. **Built: `COMPLETED.md` §783** - held as state, not as a transition: an old save is put right as it loads, and no save-format step is owed; past the record the last of record stands - the House past the 119th's term, the president past the term of record - and the Parliament page says so.
*Needs:* US-1; R-US1.
*Done when:* a Congress-of-record diagnostic steps a fresh US world across both dates — the 118th and Biden before, the 119th and Trump after; a save straddling each date loads into the game, not only the manager (§557); the dump diff shows no mover (proved, not read); review row (SimulationManager); cheap and simulation bars; USA@1280x720 declared and dry-filmed; US-1's words updated.

### Phase B — the presidency's proofs on the record

**US-3 — The presidential returns by state, read; the catalog; the Electoral College proven exact. Size L.**
Fetch and save:
- the FEC's *Official 2024 Presidential General Election Results* (2024presgeresults.xlsx) and *Federal Elections* 2012, 2016 and 2020 — the president by state and candidate;
- Maine's and Nebraska's canvasses by congressional district for the same years, and their statute pages;
- NARA's 2012 and 2016 results pages (2020 and 2024 are on disk);
- the Census Bureau's 2010 and 2020 apportionment tables; 2 U.S.C. 2a and 2c; 13 U.S.C. 141(b); 3 U.S.C. 3;
- 3 U.S.C. as the Electoral Count Reform Act of 2022 left it — §§1, 5, 7, 15 and 21; the presidential day is now §21(1), so "3 U.S.C. 1" alone is the wrong cite.

Bring `returns_2024.md`, `state_ev_2024.csv` and `district_method_2024.md` to the raw-page standard. A committed `Tools/us_returns_prep.pl` (perl; the xlsx unzipped and its XML read) generates the catalog under `Assets/Scripts/Elections/Generated/`: votes by jurisdiction, candidate and year; the ME/NE districts; the electors in force each year, derived from the apportionment (Senators plus Representatives; DC by the 23rd Amendment), not typed. The 2024 workbook carries no party labels: the candidate-to-party key is DERIVED from NARA's certificates and stated. `ElectoralCollege` reads the catalog, and `SeatAllocationBacktest`'s typed arrays retire. **Built: `COMPLETED.md` §786** - the catalog `UsPresidentialReturns` and its CSVs (by year, by jurisdiction, by district), the record `ElectionsData/usa/president_returns.md`; `ElectoralCollege.FromCatalog` feeds the allocator, which `GeneratedCatalogCheck` runs over every year of the catalog in the cheap bar. Read as built: the 2024 workbook does carry the party, on its electoral-vote columns though not its vote columns, so the ruling's premise did not hold - its derivation stands: the tool finds the nominees' columns by the surnames of NARA's nominees, and the run dies unless the workbook's own labels name the same party; 2016's seven votes for other persons are named by state and recipient - the certificates of vote record ballots - and three of the electors by the Supreme Court's *Chiafalo v. Washington* (saved), the other four by no saved official record: the done-when's "electors ... named" is met for three of seven, **put to Elias, pending** (the record's reading 3 gives the options); Maine's 2012 and 2016 districts are READ from the Governor's certificates (no district table exists). Not reached, BILLED: elections.alaska.gov (405), nebraskalegislature.gov (no connection, curl status 000; Internet Archive captures stand in), NARA's 2010-census allocation capture (429).
*Needs:* US-0.
*Done when:*
- every new raw page passes `sha256sum -c` and is registered with its publisher, date and basis;
- the tool fails on any mismatch and prints none: each year's jurisdictions sum to the FEC's national totals, the electors to 538, and the record's own statewide and district winners give NARA's split exactly — 2024 312–226 and 2020 306–232 (on disk); 2012 332–206 and 2016 306–232 pledged against 304–227 cast, the electors who broke their pledge named and not modelled (2012's and 2016's figures from memory until NARA's pages are read);
- `GeneratedCatalogCheck` re-reads the catalog; `UnwiredSubsystemCheck` unchanged (nothing in play reaches the allocator yet);
- the cheap and simulation bars green; a refused host BILLED.

**US-4 — The state-swing proof. Size M.** *(asks R-US14)*
An Editor instrument derives every jurisdiction and the five ME/NE districts from the previous presidential election at the TRUE national shares of record, by three methods — (i) §689's normalised uniform swing (`RegionalVoteModel.RegionalSharesByUniformSwing`, unchanged), (ii) proportional swing, (iii) plain additive swing (Poland's okręg form) — for 2012→2016, 2016→2020 and 2020→2024, then counts the electors on the catalog. DECLARED: the ME/NE districts swing with their state; minor candidates pooled per state; faithless electors and Maine's ranked-choice count not modelled. Nothing is fitted. It opens `docs/reference/US_ELECTIONS.md`, the US model card, writing its GENERATED block on `PresidentialVoteBacktest.WriteReadings`'s pattern: every state's miss, every state called wrong, the electors. **Built: `COMPLETED.md` §787** - `UsStateSwingCheck` (the cheap bar; pinned; a stale block fails it) and the card, whose readings carry the three methods on each consecutive pair of elections the catalog holds and the rule's recommendation; R-US14 asked with that table. Read as built (DECLARED in the card): a state weighs what it cast in the previous election; the districts swing with their state in two-party terms, a district's previous row standing in for the next election's district of the same number across the change of lines between 2020 and 2024; a tie that leaves (a) out fails the instrument, to be asked.
*Needs:* US-3.
*Done when:* pinned in the cheap bar, not live, and a stale block fails the bar; the measurement — not an assumption — says whether 2020's states give 2024's 312–226 under each method, with 2012→2016 reported as the stress case; R-US14 asked with the table.

**US-5 — The national-vote proof: can the vote model hold two parties? Size M.** *(asks R-US15)*
Source the national House vote by party, 2016–2024 — the FEC's *Federal Elections* volumes where they serve, the Clerk's *Statistics of the Presidential and Congressional Election* summary table for 2024 — beside US-3's presidential votes. National totals only: the 435 districts are US-11's, so the presidency never waits on the district PDFs. In an Editor instrument and in `GateReRun` (a USA case in the elections-backtests launch), in the backtest direction — the predicted election is never an input to its own prediction:
- fit the electorate of `PartySystems.TryElectorate` on the GPS rows two ways: free, and with its spread parameters held at the four fitted countries' (DECLARED);
- derive loyalty from each candidate history — House 2022 against 2020, presidential 2020 against 2016;
- predict 2024 from a fresh world on the eve, its economic vote read through §709's US pre-start record.

It prints the national miss per history; the degeneracy (how far the fit slides without changing the two-party split); and the E1 factors that would close the 2024 gap — Trump and Harris, with Stein, Kennedy, Oliver and the rest on their record shares as base.
*Needs:* US-3.
*Done when:* the miss table, the fitted parameters and the degeneracy are in the record; the `elec<N>` diff against the previous run moves no other country's line; R-US15 asked with them; nothing live.

**Built: `COMPLETED.md` §788** (the House vote by state, 2016-2024, by the Clerk's recapitulation, the FEC's tables the cross-check; 2024's candidates; both in the generated catalog) **and §789** - `UsNationalVoteCheck` (the cheap bar and the documents bar; 2024's own-contest misses and the economic vote pinned; R-US15's rule run and its verdict printed; a stale block fails it), the US card's second block of readings, and GateReRun's US section after its verdict, outside the gate. With two parties the vote model gives the prior back, checked on every fit, so a history's national miss is its last election's distance from the one it predicts, plus the economic vote. The `elec<N>` diff inserts the US section and moves no other line; nothing live; R-US15 asked with the card.

*Design, DECLARED before the build (§788):*
- *What the fit reads.* Each history's electorate is fitted to that history's last election before the predicted one (T−1) - the election a game started on the eve holds as its prior - and never to the predicted election. With two parties a fit pins one number, the two-party split; and since the compatibility scale hands the spatial shares back unchanged, an electorate fitted to T−1 returns T−1's split through `PreferenceModel.Preference` whatever the loyalty - exactly for a held fit, which is solved, and within its grid's residual for the free fit. The instrument checks that on every fit rather than presenting it as a model effect: a history's national miss is then its no-change miss plus the economic vote.
- *Free.* `VoteModel.Calibrate` on T−1's two-party split as it stands - its grid, its tie-break.
- *Held.* Four held fits, one per country `PartySystems.TryElectorate` holds, its σ and τ read there at run time (not typed). The mean sits on the segment from DEM's point to REP's, at the place that reproduces T−1's split - solved continuously, not on a grid. Added with the measurement (§789): a fifth, held at the four's median (σ and τ each the median of the four) - a candidate for R-US15 that favours no one country, since each of the four sits on a bound of `VoteModel.Calibrate`'s grid in at least one parameter (`PartySystems.TryElectorate`'s values against the grid). In every fit wEcon is the USA's declared weight (`VoteShareBacktest.UsEconomicWeight`, from Gallup's table of July 2026 - after every election used; inert for the two-party split at the fitted points; it sets where a held fit's mean sits and what happens away from them). The positions are `PartySystems.RealRoster`'s.
- *The degeneracy.* How far the split moves when the mean slides one and two units either way along the line that divides the two parties, from each fit; the fits' different spreads holding the same split; and what the spread decides away from the fitted points - added with the measurement (§789), the split's answer when DEM's point moves one unit toward REP's (a nominee off the party's point), and the share each fit gives a unit placed midway between the parties.
- *Loyalty.* Each party's share of all votes cast - the four countries' basis as near as the sources allow: the presidential history's share of the FEC's total, the House history's of the Clerk's Total, which carries the non-votes and Maine's re-counted ballots (below); with two parties the loyalty cannot move the split (the identity), so the denominator moves no miss. The prior is normalised over the roster by `Preference` itself.
- *The House basis.* The Clerk of the House's election statistics, 2016–2024 (*Statistics of the Presidential and Congressional Election* in a presidential year, *of the Congressional Election* in a midterm), their "Recapitulation of Votes Cast for United States Representatives": 50 states, each party's own ballot line, one publication for every year. The Clerk prints each race as its own statistics give it - in Louisiana's runoff districts the finalists' December runoff beside the others' November vote, Maine's finalists at their last ranked-choice round (in 2022 beside an eliminated candidate's first-round vote), Alaska's first choices, nothing for North Carolina's 9th district in 2018, whose election was not certified (corrected at US-11, §790, which read every race) - and its Total also counts the non-votes some states report and Maine's ranked-choice lines, which count some ballots again. The FEC's House-by-party tables, 2016–2022, are read beside it as the cross-check, every state where the two differ printed by the tool: read side by side, the FEC adds a district's vote for an unexpired term to its full-term vote and Louisiana's December runoff to November, and prints Maine's first round (reading 13 of `ElectionsData/usa/president_returns.md`, each difference the saved pages explain). This departs from the item's text, which named the FEC where it serves and the Clerk for 2024: a history drawn from both would mix their conventions, between two of its elections. Put to Elias as a reading (§788).
- *The presidential basis.* US-3's catalog summed, 51 jurisdictions.
- *The economic vote.* The code's as built - Table 9.1's presidential row on the president's party (`EconomicVote.Magnitudes`) - for both histories, read on a world built on each eve as a new game builds it (the epoch set before the world is created). Printed with and without, so the history's miss and the economic vote's are never read as one; the congressional row stays out of code (US-13's).
- *The cases.* For 2024, on the eve world (4 Nov 2024, stepped to 5 Nov): (a) House 2022 against 2020; (b) presidential 2020 against 2016. Out of sample:
  - 2022's House, on its own eve world (7 Nov 2022, stepped to 8 Nov): from House 2020 against 2018, and from (b)'s presidential 2020;
  - 2020, with no world: (b) presidential 2016 against 2012 - the case US-6 cites as "2020 from 2016" - and (a) House 2018 against 2016. No world can be built before the USA's first government of record (`WorldClock.Governments`): `GovernmentRecord.AtStart` throws; nor does the US pre-start record (`PreStartRecord`) reach back to the start of the term 2020 would judge.

  Each case is scored on its own contest and on the other, two-party and signed: REP's predicted share less the record's.
- *E1's factors.* On the 2024 eve world's poll, per history: Trump's factor is his share of the FEC's total ÷ REP's poll, and Harris's likewise. Stein, Kennedy, Oliver and the rest stand at their shares of record; the rest is every other column of the FEC's workbook, None of These Candidates and the scattered write-ins among them. Each factor is also printed divided by the two nominees' combined share, with the field's sum beside it - in-sample by construction.
- *Where it prints.* An Editor instrument, with its own batch and its GENERATED block in `docs/reference/US_ELECTIONS.md`; a stale block fails the cheap bar. `GateReRun` gains a US section after its VERDICT, in the R-EL13 form: no world, no economic vote, outside the gate. The four cases, the verdict and `BuildCases` are untouched.

### Phase C — the presidency held

**US-6 — The US prediction, and E1's factors. Size M.** *(R-US2, R-US15)*
`PartySystems.TryElectorate` and `PartySystems.TryRealHistory` gain the USA in R-US15's unit, each vintage's history pair sourced, and `PartySystems.HistoryNote` states the basis. `PresidencyOfRecord` gains the 2024 field per R-US2: Trump (REP), Harris (DEM), and the minor candidates standing only where the FEC shows them on the ballot, carrying their fitted share as their base (E1). Each candidate sits at their party's position. The presidents' dates stay in `WorldClock.Governments` — no second copy.

A candidate's national share is the party's support in the game's poll × the candidate's factor, *[FITTED]* once to the FEC's national result against the game's poll on 5 Nov 2024, on the reference world built on the eve: `PresidentialReferenceWorld` generalised, the epoch set to 4 Nov 2024 before the world is created (the s772 review's fix — never a start world with its clock moved). Later fields start at 1.0. Wired to the prediction only; no polling day yet.
*Needs:* US-5; R-US2; R-US15; E1 committed.
*Done when:*
- an instrument in the cheap bar refits the factors each run and fails where a stored factor is no longer the fit;
- the card says in words that the factors reproduce the 2024 national shares on the reference world **by construction** (the s772 review's second finding, carried), so the acceptance rests on the states (US-4's layer on 2020's states) and on the out-of-sample case (US-5's 2020 from 2016);
- the card carries E1's premise for the USA: a game played from 12 Mar 2024 reaches 5 Nov on its own economy and its own seed vintage (`WorldClock.SeedVintageLine`), so its count is not the record's;
- the sentinel unmoved, or its family explained; the cheap and simulation bars green; any surface that reads the US poll seen on the USA dry film.
**Built: `COMPLETED.md` §844** - R-US15's poll live for the USA (`PartySystems.TryHistory`/`TryElectorate`: the House pair from the catalog, the electorate held at the four's median, solved per seated vintage - a game from the start holds 2022's place for its whole run, DECLARED and held by the check; the compass captions it "held, not fitted"); the 2024 field of record (`PresidencyOfRecord`: every FEC column; the nominees' factors [FITTED] on `PresidentialReferenceWorld` generalised by country - Trump 1.0183908, Harris 0.9456195, patched from the log by `Tools/us_factor_patch.pl`; the minor columns standing nationally - a DEPARTURE put to Elias, the per-state table US-8's); the naming by date (from the DNC's 6 Aug 2024; the RNC's date BILLED, G13 - the asymmetry put to Elias); `UsPresidentialFactorCheck` (the cheap and documents bars) refits each run and writes the card's US-6 block; Poland's presidential checks byte-identical; the `elec<N>` diff moves GateReRun's US header and LoyaltyHarness's US/France block.

**RULED (Elias's ruling J3, 2026-10-19, on I16's item 11; installed `COMPLETED.md` §870; verbatim in `docs/RULINGS_2026-10-19.md`):** the departure is reversed. A minor candidate stands only where on the ballot of record (the FEC's states), as US-30 already requires. The build is the feature list's US-6c.
**Built: `COMPLETED.md` §885 (US-6c)** - the ballot of record read off the FEC workbook's fonts by its own note (`UsPresidentialReturns.CandidateStates`, `PresidencyOfRecord.StandingOfRecord`); the count's states stand each minor column only where listed (Stein 37, Kennedy 31, Oliver 47), in the record's pattern there, renormalised, the nominees swung on their two-party split over the rest; the national field and E1's factors unchanged; no call moves.

**US-7 — The oath: a presidency, not a formation. Size M.**
`ConfidenceProcedure.RulesOf` gains a presidential rule for the USA in place of Unsourced: a fixed term, no investiture and no confidence vote (Art. II; impeachment not modelled, stated). No formation and no Speaker's round run for the USA — the verdict path bypasses the formation for a presidency — which pays §629's debt of *"a presidency wearing a cabinet's words"*.

A contest's winner becomes president-elect and takes office at noon on 20 January (20th Amendment §1) through US-2's dated transition: a government of the presidency kind, its cabinet the president's party, Gamson's allocation not applied. The outgoing president governs until then; the Fed-chair appointment passes with the governing party, as built. The Desk chip and the role gate's reasons read PRESIDENT and PRESIDENT-ELECT — words only; D-US composes them. Tested on planted outcomes; US-8 supplies the real one. Its code lives in files a game path already reaches (`UnwiredSubsystemCheck` admits no game file nothing reaches, §646).
*Needs:* US-2, US-6.
*Done when:* a presidency diagnostic forces each outcome and steps to 21 Jan 2025 — each world seats its winner on the oath's date, and a DEM-player world whose candidate wins reads PRESIDENT; `PlayerRoleDiagnostic` and `ConfidenceDiagnostic` carry the US rows; `FormationSweepDiagnostic` re-pinned if its sweep reaches the USA; a save straddling the oath loads into the game; review row; bars; one filmed width shows the chip after the oath.
**Built: `COMPLETED.md` §845** - the presidential rule (a fixed term, no investiture, no confidence vote: `ConfidenceProcedure.Rules.Presidential`), the verdict path skipping the formation, the contest record saved (`UsPresidentialElection`; format 39, the step intended once for US-7 and US-8; per-state results not carried), the game's president sworn at noon on 20 January on US-2's dated transition with the record yielding from the game's polling day, the open arrival window carried by the save, PRESIDENT and PRESIDENT-ELECT on the chip and the gate; proved on planted outcomes (`PresidencyDiagnostic`). Who governs when the House cannot choose is ASKED (R-US22). §629's remaining words (the picker's IN THE CABINET, the compass legend) are carried to D-US.

**US-8 — The 2024 presidential election held. Size L.** *(R-US3, R-US14; built so R-US5's row is cheap)* — **checkpoint 1**
- `WorldClock.TryNextPollingDay` returns the US presidential days under 3 U.S.C. §21(1) — 5 Nov 2024, then every fourth year (midterm days join at US-27) — and `WorldClock.PollingDayBasis` cites the statute read at US-3.
- The run-up and the campaign window open with no staged campaign (`LiveCampaignSetup.TryFor`'s stated reason stands, as Poland's does).
- On polling day the day hook holds the count: the prediction × the factors (US-6) → the states by R-US14 from the previous presidential election's state shares → the electors by `ElectoralCollege` on the catalog; with no one at 270, R-US3. The count is a pure function of the world's state, callable whether or not the USA is the player's country — R-US5's row then needs only its gate.
- The verdict names the winner, the electors and the oath's date; the electors' meeting and the count of 6 January are dates (3 U.S.C. §§7 and 15). The president-elect takes office through US-7. Congress stays as of record by date (US-2), and `NationalElection.NotHeldReason` says so in place of US-1's line.
- The contest is stored on the Country beside Poland's and saved (a save-format step with its migration). The Parliament page's president row serves the USA; the reference view (`WorldClock.TryReference`) shows the record's 312–226 and the government after it. §709's US pre-start record is judged for the first time.
- No night until D-US is built: the verdict lands at once, as Poland's does until D-PL is built.

*Needs:* US-4, US-6, US-7; R-US3; R-US14.
*Done when:*
- E1's acceptance for the USA: a fresh world on the eve, stepped through 5 Nov 2024, elects Trump — its electors set against 312–226 and every state called wrong written into the card;
- a fresh US world stepped from 12 Mar 2024 past 5 Nov holds the count once, its log line naming the winner and the electors; a save taken across the count and across the oath loads into the game;
- `PollingDayDiagnostic`'s USA assertion inverted, as §697 did for Germany; `StartBriefDiagnostic` and US-1's words now name a polling day that is held (the USA alone leaves `WorldClock.NoElectionYet` - Italy stays in it, PS-7-interim §841 - and `StartBriefDiagnostic`'s two-way pin then holds the USA's polling day by name); `PreStartRecordDiagnostic`'s US row judged; `UnwiredSubsystemCheck`'s unreachable ceiling lowered to what the bar measures, the allocator now wired; `SaveMigrationCheck` and `PreviewParityDiagnostic` green; `elec<N>` diffed;
- the default-USA paths measured — the controller's pre-pick fallback to the USA, the film driver's USA default, the warm-up's `HoldElectionWithoutTheNight` — so no default world holds a US election by accident;
- `Reviews/` report and review row before the bar (SimulationManager); cheap and simulation bars; the USA dry film; a REAL film of the start card.

**Built: `COMPLETED.md` §847 (checkpoint 1)** - the count on the day loop (`UsPresidentialElection.Count`, pure: the poll × E1's factors; the states from the previous presidential election of record by R-US14 (a) in runtime, `ElectoralCollege.FromNationalShares`, with US-4's instrument reading it; the electors in force, labelled on screen when carried forward); R-US3 (a)'s House choice on 6 January by the House of record's delegations, a majority of all the states; the president-elect sworn through US-7; Congress by date; no format step (the states re-derived from each contest's national shares); the USA out of `NoElectionYet`; the Parliament page's president row; the reference at the record's electors. The count's naming of the nominees (R-US23), the 2028 state prior (R-US24) and the forks' own counts (R-US25) are asked; the per-state minor-candidate table stays US-6's ask.

**RULED (Elias's ruling J4, 2026-10-19; installed `COMPLETED.md` §870):** the declared readings stand, except reading 4 (*"an exact state tie goes to DEM"*). An exact state tie is broken by a seeded draw, DECLARED, never in one party's favour. The district tie, which breaks to REP by the same inheritance from US-4 (`Reviews/2026-10-17_s847_us8_count.md`, reading 4), is read by Code as falling under the same ruling, reversible by Elias's word. The build is US-8c.

### Phase D — the other proofs: Congress, the campaign, the limit

Each asks its ruling with its table. Their sourcing runs ahead from US-0; their order among themselves is free within their needs.

**US-9 — The campaign-geography proof. Size M.** *(asks R-US16)*
In an Editor harness only, stage a US campaign on the 51 jurisdictions — audience 2020's valid votes (US-3), the vote model of US-6, casts by `CampaignCasts.Of` — idle and with each AI personality, and measure three things:
- (a) where the AI's local acts land: its candidate regions are the largest audiences (`CampaignAi.LocalCandidateRegions`, `[AUTHORED-DRAFT]`) — the share in the closest 2020 states against the largest;
- (b) that a local act moves a state's derived share only through the national total: the count reads the campaign's national final shares, and `NationalElection.DeriveRegional` spreads them afterwards — *"a readout of the national result, never an input to it"*;
- (c) how far W-D1's per-region count (`ElectionDay` over the campaign's regional mobilization — pure, wired to nothing) would move one state under a maximal ground game, against the 2024 battleground margins (US-3).

*Needs:* US-3, US-4, US-6.
*Done when:* the three measurements in the record; R-US16 asked with them; nothing live; cheap bar.
**Built: `COMPLETED.md` §846** - `UsCampaignGeographyCheck` (the cheap and documents bars; an Editor harness, nothing live; the mirror of `DeriveRegional` proved equal on every staged country's campaign, no seam) and the card's US-9 block. (a) and (b) hold by construction and more: at the US audience every run closes both parties past the compatibility ceiling, so no US campaign moves the national vote (GA-5-1's candidate mechanism). (c): the mobilisation ceiling moves a state 2.90-9.00 points and could flip 13 calls (139 electors); R-US16 (c) sized over Sweden and Germany. R-US16 asked with the card.

**US-10 — The veto and the filibuster on the record (E4's form, before any wiring). Size L.** *(asks R-US17)*
Read, raw out of tree with extracts in the repo:
- the House Historian's *Presidential Vetoes* for 2017–2026 — Trump's first term, Biden, Trump's second to date — with every override;
- each vetoed measure's kind: a bill, a joint resolution, or a Congressional Review Act disapproval, whose Senate path is 5 U.S.C. 802's (read; from memory it needs no cloture);
- each one's passage and override roll calls — the Clerk's XML for the House; for the Senate, govinfo's Congressional Record or a Wayback capture, BILLED by name where neither serves;
- the final passage of every public law of the 118th Congress (the divided one, for the precision), others as read;
- an interpretive source for the override's base — two thirds of those present and voting, a quorum present — such as a CRS report through congress.gov's crs_external_products path.

A committed `Tools/us_veto_backtest.pl` writes `docs/generated/US_VETO_BACKTEST.md` in `VETO_B1_BACKTEST.md`'s form: per president, each candidate rule's base (F3's at-risk set transposed; either chamber; both chambers), the vetoes inside and outside it and the rate a draw would take - and B1's confusion matrix, hit rate and precision beside it. It also counts how many vetoed measures of record a party-bloc Congress under Rule XXII could have sent to the president at all. **Built: `COMPLETED.md` §802** - `Tools/us_veto_fetch.pl`, `Tools/us_veto_prep.pl` (the extract `ElectionsData/usa/us_veto_measures.csv`, its record `veto_record.md`, its mutation suite `Tools/us_veto_mutations.sh`), `Tools/us_veto_backtest.pl` and the document, held current by `UsVetoBacktestCheck` in the cheap bar. Read as built: every public law and every vetoed measure of the 115th-119th Congresses (1,564 measures - the 118th's 274 laws among them, the other Congresses read so that each president has a base), from the GPO's BILLSTATUS records out of tree; the Senate's roll calls through the Internet Archive (none BILLED); the expedited Senate procedures read for every kind a veto fell on (5 U.S.C. 802, P.L. 94-329 §601(b) for War Powers and arms sales, 50 U.S.C. 1622, D.C. Code §1-206.04), not the CRA's alone; the rules read each chamber's final agreement (every wording the record uses, held by a guard that the agreement acting last is never one "with an amendment", and since §831 by a second, H3's superseded-vote guard, that no final agreement stands before the other chamber's later amendment), and one by voice vote, unanimous consent or a rule's adoption names nothing; "far more" read, before the first run, as a pooled precision below B1's on Poland (PROVISIONAL, G5). The 25 vetoes equal the House Historian's counts and the Senate's lists override by override; every override is two thirds of those voting.
*Needs:* US-0.
*Done when:* the document is current, and a check fails the cheap bar when it goes stale; the tool's own check passes (party sums from member votes equal each roll call's totals; each override's requirement reproduces two thirds of those present and voting); the rates go into the reply to Elias before anything goes live (E4); a rule that names far more bills than the record vetoed — or a bloc Congress that sends none — is STOPPED and re-asked.

**US-11 — The House count proof. Size L.** *(asks R-US18)*
The Clerk's *Statistics* PDFs for 2016–2024 (all five saved, §786 and §788; their text is `pdftotext -raw`'s - the fonts are Type1, so the CID trap first feared here does not arise), the FEC's *Federal Elections* xlsx (2016–2022) as the cross-check of the winners, the ranked-choice counts and Alaska's 2022 rounds. A committed `Tools/us_house_prep.pl` generates the 435 districts per cycle with the exceptions as data: same-party generals (California's and Washington's top two, Louisiana's open primary), the ranked-choice races as the Clerk prints them (Maine's finalists at their last round, Alaska's first choices - its 2022 last round, the FEC's, the deciding count), Louisiana's December runoffs, fusion lines summed, the unopposed seats printed without votes flagged, North Carolina's uncertified 9th of 2018, Georgia's majority rule (no House runoff is printed in 2016–2024), the races one major party left uncontested readable from the candidates printed, and the maps in force each cycle dated, redraws named. **Built, the data half: `COMPLETED.md` §790** - the catalog (`ElectionsData/usa/house_districts.csv`, `house_maps.csv`, `house_years.csv`; `UsPresidentialReturns`' other part, `UsHouseDistricts.cs`), its record (`house_districts.md`), its mutation suite, and its re-read in `GeneratedCatalogCheck`; the districts give `[HH-DIV]` in every year but its two footnoted vacancies. **The lines in force and their instruments: §799** - `house_maps.csv` gains `in_force` (Missouri 2026 held by the 120th tab's note) and `instrument` (printed only), as the *Design* below asks before the instrument's first run.

Plurality is then counted per district on the record's own votes, and three seat methods are measured at the true national House vote: (a) district uniform swing on the map in force; (b) state delegations by a state swing; (c) a national seats-votes rule with one fitted parameter. The cycles are 2018→2020, 2020→2022 (the redraw: (b) and (c) only) and 2022→2024, the redrawn states named. Delegations are printed by state, which the 12th Amendment needs.
*Needs:* US-0; for its instrument, R-US18's seven choices answered blind, or Elias's word to run on the defaults, which rules none of them (§791).
*Done when:* the record's own district votes give the 2022 and 2024 House exactly against `[HH-DIV]` (REP 222 / DEM 213; REP 220 / DEM 215), every exception named; each method's seat error per cycle in the card's GENERATED block; `GeneratedCatalogCheck` re-reads; R-US18 asked.

*Design, DECLARED before the instrument's first run (§791).* A design panel specified it blind to rough scratch estimates of the three methods that the session had made first; the session, which had seen them, condensed the specification into this list and R-US18's block and corrected both where its review found them wrong (R-US18's entry says what the panel's brief held; `COMPLETED.md` §791 says what the panel saw and lists each correction). The arithmetic the rule runs on, the scoring and the decision are R-US18's preregistered block; this list holds everything else.
- *What the game needs from a count.* Control (bills pass by party blocs, so it matters most), all 50 delegations (R-US3's contingent election; the night's board) and 435 seats. (a) and (b) give all three; (c) gives control only, so it stands as the national benchmark and is never recommended alone. The game feeds the count one number, its poll: every method moves by the poll's own change and is never re-solved - the district and state methods plain additive on shares read by candidate, (c) through the change in the poll's logit (a re-solve, as §689's does, would carry the gap between the two vote bases into every unit). (a) with F-old, (b), (b') and (c) are anchored on the previous House - in play the game's own (R-US11 (a)) - so their level never jumps at the first game election, and step 2 compares responses, not proportionality's level. F-prop is not anchored: a redrawn state it counts moves to proportionality's level and stays there (S5's option); step 1 measures that level.
- *Inputs* - generated, re-read by `GeneratedCatalogCheck`, never typed. By `Tools/us_house_prep.pl`: `house_districts.csv`, each race's deciding count `final_r`/`final_d` - Louisiana's runoff finalists alone, Alaska 2022 at the FEC's last round, Maine at its last round, otherwise the general as printed; Alaska 2024 stands at first choices, its rounds BILLED - a base in no cycle of D and in no election of play; C5, once measured, reads it at first choices unless its rounds are read first (a correction of the catalog, re-pinned or asked again as R-US18's block says). Each candidate's party is the first R or D label in the record's closed label table (`house_districts.md` reading 3: Washington's 2018 "GOP" Republican, Vermont 2024's joint line Republican, the write-ins other). `house_maps.csv` for held and redrawn. By `Tools/us_returns_prep.pl`: `house_by_state.csv`, US-5's catalog, whose own-line columns give V(E), the districts' own lines asserted to sum to them. The House tool gains two generated columns: `in_force` - `lines_changed`, but where a saved page says a new plan was not used (Missouri 2026, by the 120th tab's note that its plan "cannot be used") - which the rule's held and redrawn read and which sets play's redrawn states; and `instrument`, printed only - the redraw's instrument as `house_districts.md` reading 8 names it (a court-ordered plan, an act; Minnesota's 2016 and 2018 changes "cosmetic", as the 115th and 116th tabs call both), "-" where no saved page says.
- *(a), asserted.* No row other than the no-winner and unopposed ones without a deciding count; North Carolina 2018's base without NC-9; WA-8 2018 and Vermont 2024 contested on the deciding count; the identity - every base share above one half exactly where its winner is REP and below where DEM (`GeneratedCatalogCheck` already holds every winner leading its deciding count, with no tie for first), a failure being a reading defect, nothing substituted; with M = n and no element at exactly one half, F-old's quantile form returning the plain count; M never zero. The clamp to [0, 1] is for printing only - it cannot change a call. In play a held state whose prior is an F-old set is counted as the set, each element's half going to its stored call. The output: the national seats, the 50 delegations with their classes, every held district's swung share and call, and each redrawn state with the fallback that counted it and its instrument.
- *(b), asserted.* c_T never negative - REP at least U_R and DEM at least U_D in every state. It reads no map, so a redraw changes nothing in it; its anchor is the base delegation, whatever lines it was won on, and U seats stay their party's as (a)'s ones and zeros do. At zero swing with c_T = c_B every state returns its base delegation, as (a) returns its base and (c) its base's seats - the three differ only in their response; at one seat it is identical to (a) in a held state, checked. Beside it, never deciding: (b0) - also F-prop - which at zero swing against the base delegations prints proportionality's level error; (b U-blind), a sensitivity line; (b'), which can only stop the rule. The plan reserves its one fitted parameter for (c): (b') has two, so adopting it is an amendment, which says whether they are refit on the game's previous House at each election or held from the record.
- *(c), printed beside its verdict-line comparison:* rho with its window; every (x, y); the in-sample fit; the leave-one-out fits and errors for every cycle, labelled "fitted on later elections" where they are; the fit with every transition (C3 too); the fit on held states only - the states held across each transition, their own-line votes and decided seats - showing how far the redraws inside its window move rho, the reason that leaves C3 out applied to them; a rho at or below zero marked unplayable. Checked: at V(T) = V(B), (c) returns its base - REP the nearest integer to 435 × S(B), a half to B's majority - and Shat lies within 1e-12 of S(B). In play, if ever ruled, its window is C1, C2 and C4 and its anchor the game's previous House. If Elias rules its total in, the step-2 method's delegations are reconciled to it: the difference moved, a seat at a time, toward the party it favours, nearest to flipping first among the other party's calls by vote share (a held district by its swung share's distance from one half; an F-old element likewise; a (b) or F-prop state by its distance from the next rounding threshold in the favoured direction, a state at its clamp excluded), ties by postal code and then district; the result asserted to every state's seats, to every bound (a state's REP within [0, n], a (b) or F-prop state's within [U_R, n_T − U_D]) and to 435.
- *The truth* is the record's own district plurality on the Clerk's counts in every cycle - the quantity every method predicts, from the publication the base comes from. It is a data gate, not a ruling: it must give `[HH-DIV]` every year but NY-22 2020, which goes to the winner on the Clerk's count (certified on 8 February 2021, as the FEC's 2020 sheet notes; the game counts all 435 seats by plurality and models no certification - `[HH-DIV]`'s 2020 is S4's reading (i)), and NC-9 2018, undecided (no votes printed; the 2019 special is another election). The game's 117th of record (`PartySystems.SeatsAt` and `PartySystems.ChamberSizeAt` at `Usa2020` - `[HH-DIV]`'s election-day figures, NY-22 unassigned) is untouched. Alaska 2022's truth is its winner; as a base it is read at the FEC's last round. The delegations of record and the contingent call of record (26 delegations to elect; a tied delegation casts no vote, R-US3) are derived by the instrument and pinned per year - the class counts and the call, their figures in the code and `COMPLETED.md` §791, never in this plan.
- *The cycles.* C1 to C4 (C1 2016→2018, C2 2018→2020, C3 2020→2022, C4 2022→2024) are measured; D = {C1, C2, C4}. C4's swing is near zero (§791), so every anchored method returns its base except where that swing carries a district across a tie ((a)) or a seat count across a rounding ((b), (b'), (c)). C4 tests structure and step 1 - its redrawn states, at near-zero swing, show whether a new map looked like the old one or like proportional - and its own conditions can only veto a switch; while S4's reading (vii) stands, where its figures in step 2's and step 5's sums decide, (vii) asks (a blind answer to choice 7, either way, makes (vii) printed only). C3, the census redistricting, is printed in R-US14's stress-case form, (a) running through F-old in every multi-seat state - the item's "(b) and (c) only" honoured for deciding, since C3 never decides: play meets no census redistricting before the 2030 apportionment is sourced (R-US13 (a)), and its first redraws are mid-decade ones, the kind C1, C2 and C4 hold. Step 1 is also printed over every redrawn state-cycle, C3's included, never deciding. This departs from the item's text, which lists 2018→2020, 2020→2022 and 2022→2024: C1 joins D as the record's largest swing and its only midterm on mostly held lines - the kind of election a 2024-start game meets on 3 Nov 2026 - and while S4's reading (ii) stands, C1 can turn a verdict into an ask but never decide against the item's list (a blind answer to choice 1, either way, makes (ii) printed only). Asked blind (R-US18's entry). C5 (2024→2026) is pre-declared: BILLED until the Clerk's 2026 statistics are saved (due 2027), never estimated, its truth gate then generated the same way, against `[HH-DIV]`'s 120th row and its footnoted vacancies (read into `house_years.csv`'s `vacant`, as NC-9's and NY-22's are).
- *The measures,* per cycle, for (a) with F-old, (a) with F-prop, (b), (b0), (b U-blind), (b'), (c) and no change:
  - REP seats against the truth, N and |N|, and each row's zero-swing count beside it - for an anchored method its base, so that N is its response error;
  - control - the majority (218 of 435) called right or wrong, with its margin predicted and of record;
  - W per state, G, H and G − H ((c) has none); G counts each misplaced seat once, bounds |N| and gives no credit for cancellations;
  - every delegation beside the record's ((c) has none); K, the wrong ones named; the tied delegations against the record's; the class counts; the contingent outcome against the record's - REP, DEM or "no state majority" (neither party at 26, R-US3's unmodelled branch);
  - for (a) in held states, the districts called wrong, named with their margins and split into "the swing flipped it, the record did not" and the reverse - NC-9 2018 never among them: (a)'s call on it (C1) is printed, not listed as wrong, since it has no winner of record, and counts in (a)'s North Carolina total, which W, N and K score by interval; the mean absolute two-party miss over districts contested at both elections; the base's one-party seats that changed party, named;
  - the per-state table: seats in force, held or redrawn with its source and instrument, the record's REP−DEM, each row's, a mark where a class is wrong;
  - the identities, asserted; (c)'s fits;
  - the exceptions, named: fusion, same-party, one-party, unopposed, Louisiana's runoffs, Maine's and Alaska's ranked-choice counts, NC-9, NY-22, the labels of WA-8 2018 and Vermont 2024, every state counted by F-old with M ≠ n, and every tie-break that fired - a share or q(k) at exactly one half (to its base winner, or by the half rule), a rounding half (the half rule), (c)'s half (to B's majority), and equal sums in steps 1 and 2 (the TIE-BREAKS);
  - the decision table over D: step 1's W by redrawn state-cycle under each fallback, with each map's instrument and each fallback's delegation class against the record's, and step 1's sums; H, G, |N|, N and K per method and cycle and summed; (c)'s and (b')'s comparisons; each figure marked better, equal or worse; the outcome under the declared readings and under each of S4's seven; the verdict line;
  - adequacy, per cycle: the kept fallback's misplaced seats per 100 redrawn seats against (a)'s per 100 held seats, printed with the sourcing question;
  - tilt: a deciding method whose signed N has one sign in every cycle of D is flagged - a partisan tilt the player would feel - never a stop;
  - the game's range, where no truth exists - 5 Nov 2024 and 3 Nov 2026, the 2026 base the chain a world that follows history runs (the method's own 2024 count at 2024's V, on the 2026 lines in force, Missouri under both readings): REP seats at V from 40 to 60 % in one-point steps; the control point, the smallest V on a 0.01-point grid that gives REP 218; the districts the fallback counts and, for F-prop, the states it would hold for good;
  - pins: every measured cycle and row - REP seats, N, G, H, K and the contingent outcome; for (c) REP seats and N; for the record each year, its delegation class counts and contingent call. A moved pin or an unpinned measured row fails by name. A moved pin is re-pinned in the commit that moves it, naming its cause - but a pin whose figure is in the decisive digest only as R-US18's block allows, and a record pin only by the commit that corrects the catalog, naming the correction.

  Only the figures the rule names decide; everything else informs Elias.
- *Where it prints.* `UsHouseCountCheck`, `Run` and `WriteReadings` on `UsStateSwingCheck`'s pattern, in the cheap bar and the documents bar; the card's third GENERATED block, stamped with the catalog's digest, the rule's, the decisive table's and the ruling record's state. A stale block fails both bars, and so does a ruling-record line after R-US18's block that differs from the instrument's record. An ask is registered, not failed (R-US18's block): the grain's "asks the ruling it informs, with its table" and "nothing measured goes live unruled" meet CLAUDE.md's one green bar per commit, and an unregistered stop still fails, so the ask cannot be skipped. `UsStateSwingCheck` and `UsNationalVoteCheck` keep their own form unless Elias extends this to them.
- *Data failures are defects, not asks* - each fails the instrument until fixed: the truth gate; the record's delegation class counts or contingent call against their pins; districts or seats against the apportionment, or a total not 435; a state-cycle with no held or redrawn class; `in_force` differing from `lines_changed` in 2016-2024; the own-line V(E) read from `house_by_state.csv` not US-5's catalog; any row's output, no change included, outside its bounds or not summing to its states' seats and to 435; c_T below zero; a winner outside REP and DEM in 2016-2024 (the truth gate); an Alaska 2022 row without its last round, or a Louisiana runoff row without its finalists; a row other than the no-winner and unopposed ones without a deciding count; North Carolina 2018's base not its decided seats; WA-8 2018 or Vermont 2024 not contested on the deciding count; a zero-swing identity failing; M = 0; sum x² = 0 in a (c) window; a rule digest not on the instrument's list; a pin moved, a measured row unpinned, or a stale block; `GeneratedCatalogCheck`'s re-read failing.
- *In play* (US-16 on the presidential day; US-27 every even year). V(T) is the game's poll on polling day, REP/(REP+DEM), before E1's factors (R-US15 (a), asked). B is the previous House election - the record's 2022 for 5 Nov 2024, then the game's own (US-26, R-US11 (a)) - and V(B) the V that count used. Stored per state in the save, unclamped (a format step at US-16): the labelled district shares where the state was held; the F-old set with each element's call where F-old counted, the state's next prior; and always the state's share u, the delegation the count gave and its unopposed seats by party, which (b) and F-prop read after the game's own election, since no district votes are drawn (the board shows seats and delegations, no House vote breakdown). Every share is its base plus the summed swings, so rolling forward is exact. A state counted by F-prop - only under a ruling - stays on F-prop at every later election, its footprint printed in the game's range. The redrawn states are `house_maps.csv`'s `in_force` reading - Missouri held on 3 Nov 2026 by its saved note, `in_force` equal to `lines_changed` again if a later read restores the plan - and past the rows read the last map read stays in force. The chamber is set by `ParliamentSystem.SetSeatsFromElection` with REP the count and DEM 435 less it; the delegations are stored with the contest and saved, for R-US3 on 6 January and for the night's board. The count is a pure function with no draw: 435 seats in 50 states at the apportionment in force (2 U.S.C. 2a; R-US13 (a) carries 2020's forward), asserted on every call. Not modelled, stated: third-party winners; runoffs, ranked-choice transfers and Georgia's majority rule (with two parties the plurality is a majority); specials; party switches; incumbency.
- *What the record cannot decide, flagged:*
  - (b)'s seat rule - the plan names none (asked blind, choice 5);
  - a prior on new lines - published results by new district for each enacted plan would retire the blind fallback (asked with R-US18, S5's third option);
  - the kind of the record's redraws against play's - mixed (a court-ordered plan, enacted acts, some named only, a change the Bureau calls cosmetic) and partly unread (`house_districts.md` reading 8), Colorado 2018's undescribed, Pennsylvania 2018's taken as a redraw on the Bureau's list (DECLARED; its instrument BILLED), 2026's not read; which is why S5 asks rather than switches;
  - whose geography rolls forward - US-26 as ruled, the game's own count (the alternative, the record's newest returns on the lines in force, would shorten the fallback's reach; Elias's reading of R-US11);
  - whether C1 belongs in D (asked blind, choice 1);
  - the noise floor - two swing cycles and a level cycle cannot separate methods a few seats apart, so the rule asks for consistency and asks wherever a declared reading decides; a one-seat difference can still move the outcome between (a) and S1, or S1 and (b) - no integer rule is free of an edge, but no declared one-seat convention decides silently, since S4 runs each;
  - NY-22 2020 (asked blind, choice 2; S4 (i) guards it) and NC-9 2018 as conventions argued from the game's model;
  - unopposed seats against an imputed share - the record cannot choose; they part only at swings far beyond the record's;
  - Minnesota's and Colorado's 2018 changes (asked blind, choice 3);
  - the swing's basis (asked blind, choice 4; S4 (vi));
  - C4's place in step 2's and step 5's sums - the panel's arithmetic counts it, its account of C4 says it only vetoes (asked blind, choice 7; S4 (vii));
  - (c)'s rho on one or two transitions - S3 cannot fire on the record on disk; the Clerk's statistics before 2016 would make it live (asked blind, choice 6);
  - R-US15 - RULED (a) (G2, §797): V in play is the own-line House vote;
  - US-5's House basis (§788) - RULED (a), the Clerk's figures throughout (G4, §797): `house_by_state.csv` stands as built;
  - contestation moving V - the districts a party leaves uncontested change from one election to the next; every method reads the same swing, and the effect cannot be measured without modelling uncontested votes;
  - Missouri's 2026 plan - read as held by the saved note; the referendum's result and the record's 2026 returns BILLED;
  - Alaska's sources - the FEC's 2022 round departs, for one district, from "the Clerk throughout"; 2024's rounds BILLED;
  - a census redistricting in play after 2030 - C3's case, printed;
  - swings beyond the record - one-party seats keep their one or zero until half the vote has swung;
  - R-US3's unmodelled branch - R-US3 is ruled, so "no state majority" (neither party at 26) prints as its own outcome in every cycle and method, flagged before US-16 (§791 says how near 2018 and 2022 came);
  - C5's role - whether 2026 joins D is an amendment, once it is read.

**US-12 — The Senate by state and by date, and the race proof. Size M.** *(asks R-US19; R-US10 seats the independents)*
- Extract the per-state, per-class table from the three saved class pages (G7; no fetch) with a committed `Tools/us_senate_prep.pl`.
- Date every seat change of the 118th and 119th Senates — appointments, resignations, deaths, re-registrations — from GPO's Congressional Directory on govinfo or dated Wayback captures. From memory, unverified: one senator re-registered from Democrat to independent in mid-2024, which would make the division on 12 March 2024 differ from `[SEN-DIV]`'s undated line.
- Read each independent's caucus from the Senate's own record (the saved page states it for the 117th only), and name the Senate on 12 Mar 2024 and on 3 Jan 2025.
- Read the Class I returns of 2018 and 2024 and the 2024 specials (FEC; the Clerk's statistics2024.pdf), and measure 2024's Class I at the true national vote two ways: (a) the state's presidential-level derived share that day; (b) the seat's 2018 result swung nationally — each counted against the 119th of record.

*Needs:* US-0 (the race half: US-3, US-4). The measurement - `UsSenateRaceCheck`, R-US19's instrument - waits on Elias's answers to R-US19's eight choices asked blind (§794) or his word to run on the defaults; the catalog step before it does not.
*Done when:* a roster diagnostic finds 100 seats in three classes by state on both dates, reproducing `[SEN-DIV]`'s division per Congress or naming each dated difference; the caucus sourced or BILLED; the race table in the record; R-US19 asked.

**Built, the roster half: `COMPLETED.md` §792** - `Tools/us_senate_prep.pl`, the catalog (`ElectionsData/usa/senate_seats.csv`, `senate_changes.csv`, `senate_on.csv`, `senate_division.csv`; `UsPresidentialReturns`' part `UsSenateRecord.cs`), its record (`ElectionsData/usa/senate_record.md`), its mutation suite (`Tools/us_senate_mutations.sh`), and `GeneratedCatalogCheck.CheckUsSenate`, the roster diagnostic: 100 seats in three classes by state on every named day, the US start (`WorldClock.StartDate`) and the 119th's opening among them, the roster derived again from the seat rows; `[SEN-DIV]`'s undated lines dated by the stretches of days each holds; the caucus sourced. It departs from the first line above: the base is the Senate's 50 "States in the Senate" pages (saved at §792, `raw/senate/`), each seat's holders with their days, and the three class pages are anchors - each must equal the roster on its own page date - since a class page is one day's Senate and names no change. The from-memory line was right: the re-registration was Manchin's, on 5 Jun 2024, by the Senate's own page. The race half (the Class I returns, R-US19) is part two. **Its catalog: §793** - `Tools/us_senate_races_prep.pl` reads every Senate race of 2018 and 2024 (the Class I seats and the specials beside them) and Nebraska's Class II race of 2020 from the Clerk's statistics, twice, held to each state's recapitulation, to the FEC's 2018 sheet and every winner to the roster above (`senate_races.csv`, `UsSenateRaces.cs`; `senate_record.md`'s race half; `Tools/us_senate_races_mutations.sh`; `GeneratedCatalogCheck.CheckUsSenateRaces`). It measures nothing: R-US19's details are declared before any measurement, as R-US18's were (§791). **The roster back to 3 Jan 2017: §795** - R-US19's catalog step, its first part (US-12's *Design*): `Tools/us_senate_prep.pl` reads every holder whose service ended from 4 Jan 2017 to the window on his state page's own days (no oath before the window is on a saved page), held to the Died in Office and Appointed Senators pages for those years and to `[SEN-DIV]`'s 117th line and note, one slip DECLARED (`senate_record.md` reading 9). **The races of 2016-2024 and the counts R-US19 reads: §796** - R-US19's catalog step, its second part: `Tools/us_senate_races_prep.pl` reads the Senate races of 2016, 2020 and 2022 beside 2018's and 2024's (175 races; the five runoffs' marks held to their footnotes, Alaska 2022's ranked rounds from the FEC's sheet, the FEC's 2016-2022 sheets the cross-check with their differences DECLARED) and writes each race's counts by side - the general's ballot, the base count and which it is, each side's strongest candidate and the strongest of no side (`senate_record.md`'s race half, readings 11-12).

*Design, DECLARED (§792; `senate_record.md`, its readings):*
- *A seat is held from the oath* - the New Senators page's date of swearing, not the appointment or the term's first day (the state pages mix the two); between a departure and the next oath the seat is vacant.
- *The roster on a day is the Senate at that day's end*: a senator counts from his oath's day and not on his last.
- *A senator-elect not yet sworn holds no seat*; `[SEN-DIV]`'s 119th line, which counts one, is a named and dated difference.
- *Party* by the state's page; *a change* by the Senate's Changed Parties page, held to its words; *an independent's caucus* by the Senate Democrats' own list on its capture day, or by the Senate's own words where the list is silent, applied over his whole service in the window - Sinema's a reading put to Elias (R-US10).
- *Which source decides a day*: the Senate's own pages; the Congressional Directory's change days printed where they differ, its oaths and counts held equal.
- *The record's reach*: the New Senators page's capture - a row beginning after it is set aside and a service ending after it written as serving, both printed; the named days and `[SEN-DIV]`'s stretches end at it.

*Design of the race half, DECLARED before the instrument's first run (§794; R-US19's block holds the rule, and the panel's specification, verbatim in `Reviews/2026-10-06_s794_us12_blind_panel.md`, the rest - where the two differ, this plan governs):*
- *The catalog step*, after the preregistering commit and before the first run, measuring nothing: `Tools/us_senate_races_prep.pl` reads the Senate listings of the saved Clerk statistics of 2016, 2020 (whole: Class II and every special) and 2022, held to their recapitulations, the FEC's 2016-2022 sheets and the roster as §793 reads 2018 and 2024; `senate_seats.csv`'s holder rows are extended back to 3 Jan 2017 from the saved state pages, a gap where a page does not reach declared and printed; the caucus column gains a sourced "neither" (N, its `caucus_by` citing the Senate's words), an independent with a blank source still refused; and the race catalog gains the generated columns the block names (`gen_r`, `gen_dc`, `final_r`, `final_dc`, `final_r_top`, `final_dc_top`, `final_none_top`, `base_count` with its page), each with mutation cases and a re-read in `GeneratedCatalogCheck.CheckUsSenateRaces`. Per read year the check asserts completeness: every seat of the year's class has its regular race, every special the roster names has a row.
- *The truth's data gate* (the instrument fails - a defect for code or sourcing, never a ruling): a target winner who is not his seat's next holder (K3's held to the roster of 3 Jan 2023); an independent winner with a blank caucus source (a sourced N is not a failure: it puts the seat in O and fires S7); a tie.
- *The instrument fails the cheap bar also on:* a catalog re-read failing or a page off its digest; the generated columns absent or not re-read; an independent's votes unseparable; K1's default count not 33 one per Class I state, K3's not 34; a seat of D without a base, or a base other than its latest race of record; a base without a share (a missing `house_by_state.csv` row for a fallback included), other than one registered under S9; a base winner not the seat's next holder where the extended roster reaches; the identity failing on a base not registered under S8; a zero two-party total; an R-US14 residual at or above 1e-9; (a) at a midterm not returning P's calls at V(T) = V(P), or reading (x) not returning a_s(P) there, or pi* outside (0, 1); California's two contests called differently, or two same-day races of one seat with different sides or winners; holdovers plus the class not 100; a pin moved without its re-pin, a measured row unpinned, a stale card block; the ruling-record line differing from the instrument's; V(E) read other than through the one reader R-US18's instrument shares; the block's digest not the last on the instrument's list.
- *(a) in play:* on a presidential day each seat's share is its state's share from the presidential count that night, after E1's factors (US-8) - the night's map, so (a) cannot split a ticket on that day, and the card says so; at a midterm (US-27) it is the share US-16 stored on the last presidential day, unclamped, with that day's V, moved by V's change (B8's default).
- *(b) in play:* record bases until the game elects each seat - 2018 Class I for 5 Nov 2024, 2020 Class II for 3 Nov 2026, 2022 Class III for 7 Nov 2028, each special's seat's previous race of record, the reads scheduled by the ruling and an unread base BILLED (G6) - a play base whose runoff is on no saved page among them, printed, outside the identity until read; then each seat's share is stored unclamped with its V and the next election starts from it (R-US11 (a), US-26), telescoping exactly. Game results are two-sided, so F-H fires only at a seat's first game election. After every election the instrument asserts 100 seats in classes of 33, 33 and 34.
- *(c), if ruled:* fitted at E1's point - US-6's reference world on the eve of 5 Nov 2024 - against its poll V for parent (b) or the presidential count's state shares for parent (a); a cheap-bar instrument refits each run and fails on a stale factor or a factor other than 1.0 on a later field. Under parent (a) it equals its parent on every later race (asserted); under parent (b) the ruling names which share US-26 stores - the factored 2024 share or the parent's - and that is asserted.
- *Premises, each DECLARED:* two sides, an independent with his conference and a winner of no side never assigned (R-US10 (a)); one poll, V before E1's factors, plus the presidential count's state shares on its day - no Senate or state poll (R-US15 (a), asked); (a)'s state shares by R-US14's method, its (a) while R-US14 is asked; anchored, plain additive, never re-solved, every share stored unclamped with its day's V; plurality decides in play (runoffs and ranked rounds not modelled - the record's deciding count names the winner, its last two-sided count the share); a caucus read in 2023-2026 carried back to the same person's earlier races (no saved page dates an earlier one); no candidate effect outside (c) - (b) carries a seat's previous candidate forward, (a) carries none; every winner seated at noon on 3 January, no delayed oath (R-US18's NY-22 convention); both methods scored on the same seats, truth and cascade, neither given the target's state-level result ((a-record) printed only); the count a pure function of the world's state; the ruling holding only under R-US15 (a).
- *The departures from US-12's item text:* the truth is each target race's winner by side, which equals "the 119th of record" on every published figure (West Virginia's late oath the one named difference, printed); and the 2016→2022 Class III cycle joins under B1's default.
- *The card:* `UsSenateRaceCheck` writes its own GENERATED block on `docs/reference/US_ELECTIONS.md`, stamped with the catalog, rule and decisive digests and the ruling state. The ask prints what each option costs to build, never deciding: (a) one stored share per state per presidential day and no Senate base; (b) every seat's previous result read and stored, with the 2020 and 2022 reads (Georgia's runoffs and Alaska's rounds among them); each hybrid both; (c) a refit instrument and, under (b), the roll-forward choice.
- *Printed beside the block's printed list:* the labels-only, the non-Republican-summed and the deciding-count bases; (a) at a midterm from P's record rows ("(a-mid, record P)"); a tilt flag where N has one sign in every cycle; (c)'s f_R, f_D, s and p per race; both Vice Presidents for K1's control; the seats where a side is absent from the deciding count; the game's range at a 0.01 grid around 51 and 50 and the cloture point.

**US-13 — The midterm record, and the model's midterm. Size M.** *(asks R-US20)*
A committed `Tools/us_midterm_record.pl` reads the saved `[HH-DIV]` divisions and NARA's results pages for each president's party, writing `docs/generated/US_MIDTERM_RECORD.md`: the president's party's House seat change at every midterm since 1946, and in presidential years. An Editor instrument runs the model from the eve of 5 Nov 2024 to 3 Nov 2026 on US-5's staging, with the congressional economic-vote figures (`docs/reference/ECONOMIC_VOTE.md`, Table 9.1's US legislative row — sourced, not in code), and prints the swing the model produces.
*Needs:* US-5, US-11.
*Done when:* the record and the model side by side; R-US20 asked.
**Built: `COMPLETED.md` §840** - `Tools/us_midterm_record.pl` ([HH-DIV]'s seats, NARA's presidents, the Clerk's vote 1998-2024 - its 1944-1996 statistics page images, BILLED - Vital Statistics' Table 2-2 printed beside as SECONDARY) writing `ElectionsData/usa/house_midterms.csv` and `docs/generated/US_MIDTERM_RECORD.md`; `UsMidtermCheck` (the cheap and documents bars, 3.4 s), the US card's midterm block: the record held to its pages, the model's 2026 midterm from the eve of 5 Nov 2024 (no economic vote / the presidential row / Table 9.1's legislative cell [draft], in the instrument only), R-US20's preregistered rule and verdict - (a), m -0.21 inside [-8.97, +2.26] - with the rule's reach (|m| at most 1.60, so (b) unreachable under the rule), the ruling record ASKED. Nothing live.

**US-14 — The debt limit measured before it runs; the budget statutes read. Size M.** *(asks R-US21)*
- **The bridge first** (PN-4: same year, same perimeter): Treasury's MSPD tables at the seed's vintage (table 2 on disk; tables 1 and 3 from api.fiscaldata.treasury.gov) set against the seed's gross federal debt, whose perimeter is unstated (`WorldFactory`); the residual stated.
- **The statute as dated data:** suspended to 1 Jan 2025; reinstated on 2 Jan 2025 by P.L. 118-5 §401(b)'s formula — the record's $36,103,995,660,231 reproduced from the record's own debt; +$5,000,000,000,000 on 4 Jul 2025 (P.L. 119-21 §72001).
- **The dates:** an Editor diagnostic reads the model's debt through the bridge on each start's no-policy path (`NoPolicyCentury.For`) and prints the day the limit is reached and the day it binds under each option of R-US21, beside CRS IF10292's *"early to mid 2027"*. It measures the AI ministry's ratio-trend "debt-limit logic", and its borrowed sequester, against the dollar limit.
- **Also read for US-22:** 31 U.S.C. 1102, 1105(a) and 1341–1342; 2 U.S.C. 631, 632 and 681 ff.; the Budget Act §904(c)–(d), the note to 2 U.S.C. 621 — the Byrd rule's three-fifths waiver, correcting G10's "§644(e)" pointer; and Treasury's letters on the 2025 extraordinary-measures episode, for R-US21's span. **For R-US9 as ruled (F8):** 31 U.S.C. 1341–1342 (the Antideficiency Act); the lapse of record - P.L. 119-4 (the full-year continuing appropriations for the fiscal year ending 30 September 2025) and H.R. 5371's actions to P.L. 119-37 (12 November 2025), both captured 2026-10-05 from govinfo's own pages and held out of tree until this item brings them in with their digests; and the excepted share of discretionary spending in a lapse (OMB's and the agencies' lapse plans), BILLED until read.

*Needs:* US-0.
*Done when:* the dates are printed per option and per start in the cheap bar, nothing live; `debt_limit.md` carries the bridge; R-US21 asked with the dates.
**Built: `COMPLETED.md` §838** - `Tools/us_debt_limit_prep.pl` and `docs/generated/US_DEBT_LIMIT.md` (the limit by date from the acts and Treasury's tables, the 2025 reinstatement reproduced from the record's own debt to the DTS's million, the record's last measures, Treasury's perimeters); the bridge to the seed (the document's engine block, `UsDebtLimitCheck.WriteBridge`: ratio 1.092538); `UsDebtLimitCheck` (the cheap and documents bars, 12.3 s: the record recomputed, the bridge pinned, the reach and bind dates per start and per R-US21 option beside CRS IF10292, the cut, the ministry's trigger); `debt_limit.md` §6-§11. The excepted share BILLED; nothing live; R-US21 asked with the dates. The review’s fixes, verified twice: §842 (the check holds the document to Treasury’s files, not to itself; a body digest; the derived dates derived).

### Phase E — Congress seated and elected

**US-15 — The Senate as a chamber. Size L.** *(R-US10)*
A second chamber on the Country wherever the game seats one, built generic so Italy's Senato (PS-7, confidence in both chambers) reuses it:
- seats by party, the independents per R-US10, seated by date from US-12's table — the 118th at the US start, the 119th from 3 Jan 2025 — through US-2's transition, `WorldClock` gaining the Senate's chambers of record;
- the Vice President's tie-break (Art. I §3 cl. 4, extracted) deciding an equal division;
- the save carrying it — a format step, its migration, and the field in `ClonePreviewCountry`'s hand-list;
- the brief and the start card naming the Senate majority — the card keeps §627's House bar until D-US says otherwise — and Politics › Parliament drawing the Senate as a second chamber card beside the House, structurally;
- `EconomicVote`'s "divided government" re-read against its source's coding, reading both chambers if the source codes it so (to be read; today the House's).

*Needs:* US-8, US-12; R-US10.
*Done when:* a Senate diagnostic finds 100 seats in three classes by state, the 118th at the US start with the ruled caucus and the 119th after 3 Jan 2025; `SaveMigrationCheck` and `PreviewParityDiagnostic` green; `StartBriefDiagnostic` carries a Senate clause; Sweden, Germany and Poland byte-inert by the dump diff; bars; the dry film; REAL films of the start card at 1280 and 2560.
**Built: `COMPLETED.md` §849** - the Senate of record seated senator by senator on its dates in the player's country (`UsSenate.On` over US-12's table, R-US10 (a): each independent with his caucus; nothing seated before the record's first day; past the record the last of record stands), held as state on US-2's transition, at load and at selection; a generic `Country.SecondChamberSeats` (save format 40, the clone's hand-list); Art. I §3 cl. 4 (`UsSenate.MajorityOf`/`Carries`) with the vice president read through US-28's one derivation (`UsTermLimit.TryVicePresidentOn`); the brief's Senate clause and row, the start card's caption (the House bar kept), the Parliament page's Senate card; EconomicVote's divided government read over both chambers (DECLARED, [BK-D] p. 245), which agrees with the House-only reading in every state reachable before US-16; R-US20's term asked.

**US-16 — The House and a Senate class elected on the same day. Size L.** *(R-US18, R-US19)* — **checkpoint 2**
On the presidential polling day the game also elects the House, by R-US18's method on US-11's districts, and the Senate class whose terms expire, by R-US19's rule (2 U.S.C. §1; the specials where sourced, else BILLED — G6). Both are seated at noon on 3 January. The 12th Amendment (R-US3) reads the House-elect's delegations. The reference shows the record's 220/215 and the 119th Senate of record. "Divided" is read from the game's own chambers. US-2's Congress of record retires for each chamber the game now elects, so under §618's ruling 4 a 2024-start game elects its own 119th Congress.
*Needs:* US-8, US-11, US-12, US-15; R-US18; R-US19.
*Done when:* a Congress-election diagnostic — a fresh world on the eve of 5 Nov 2024 seats a 119th whose misses against the record are written into the card; 435 and 100 sum after every election; a save across the count loads into the game; review row (SimulationManager); bars; the dry film.
**Built: `COMPLETED.md` §883 (checkpoint 2, 2026-10-20)** - the House by R-US18 (b) and the Senate class (and the day's specials of record) by R-US19 (c) over (a) on the presidential polling day (`UsCongressElection`), seated on 3 January; the record retires chamber by chamber; the 12th Amendment reads the game's House; R-US22's order built (an acting president no incumbent, the player's where of the player's party); US-8c's seeded ties; format 49; `CongressElectionDiagnostic` writes the 119th's misses into the card (the eve world: the House REP 216 / DEM 219 against the record's 220/215; the Senate class 15/19, none called wrong).

**US-17 — D-US asked, part one: the night. Size S.**
The main session sends D-US part one in the standard form (§5) against the built count. It goes to `uploads/D-US_ask/`: one zip with its `MANIFEST.sha256` (`Tools/ask_zip.pl`) and the frames loose beside it; the state table generated by a new `Tools/us_states_table.pl` (on `Tools/pl_districts_table.pl`'s pattern); the German night as built (and D-PL's boards, if answered by then); the US Parliament tab. Design is told that the three contests are asked together, as E3 folded Poland's presidential night into D-PL, and that this is the Electoral College's night, not D-PR's run-off.
*Needs:* US-16.
*Done when:* the upload is listed back whole; the ask stands at the head of `CLAUDE_DESIGN_ASSET_REQUEST.md`, its local copy under `PoliSim-captures/design/D-US_ask/`; the D-US row reads ASKED.

### Phase F — Congress at work

**US-18 — Bills through both chambers. Size XL.** *(R-US9 for what rides reconciliation)*
- A US bill passes the House by a majority of those voting, then the Senate at cloture — three fifths of the senators duly chosen and sworn, computed from the sworn count on the day (Rule XXII ¶2), never fixed at 60 — then by a majority, the Vice President breaking a tie on passage. A reconciliation bill takes the simple-majority path (2 U.S.C. §641(e)'s twenty hours).
- The bloc premise is stated: a party votes as one in each chamber.
- Each chamber records its own division, the Senate's carrying its cloture line as Required. `ParliamentSystem.WouldBillPass` and `ChamberVerdicts` read both chambers for the USA through one projection; the Laws page names the chamber that stops a bill; the signing plate shows both divisions. Every other country's single chamber is untouched.
- A pass-rate sweep over the game's bill kinds in the 118th and 119th of record is reported, not tuned.

*Needs:* US-15; R-US9.
*Done when:* a Congress diagnostic shows, in the 119th of record, a party-line REP ordinary bill passing the House and failing cloture, a party-line REP reconciliation passing both chambers without cloture, and an uncontested bill passing both; and, in the 118th of record, a party-line DEM reconciliation passing the Senate with its caucus (R-US10) and dying in the REP House, while a party-line REP ordinary bill passes the House and dies at cloture. `ChamberVerdictCacheCheck` green; Sweden, Germany and Poland byte-inert by the dump diff; `Reviews/` report and review row (SimulationManager); bars; REAL films of the signing plate with two chambers at 1280 and 2560.
**Built: `COMPLETED.md` §851** - `Congress` (the House by a majority of those voting, a tie failing; the Senate at cloture - three fifths of the senators sworn on the day - or by reconciliation, the Vice President deciding an equal division; one stance a party in both chambers), each chamber's division its own record with its origin (US-21 reads `DivisionRecord.Origin`), the one projection behind one predicate (`Congress.VotesBothChambers`: the gate, `ParliamentSystem.WouldBillPass` dated, `ChamberVerdicts`), the Laws page and the signing plate naming both chambers; reconciliation DECLARED for the tax and welfare programme bills and a budget moving no spending line nor the pension age (Trade, Sector credits, SwfDrawdown and the unmodelled yearly limit ASKED); the AI president's budget stays in the House until US-22 (DECLARED - US-22 closes it); `UsCongressDiagnostic` with the pass-rate sweep reported.

**US-19 — Roles in a presidential system. Size L.** *(R-US7, R-US8)*
- Per R-US7: the president's party introduces the administration's bills and proposes the budget; a party out of the White House introduces ordinary bills in any chamber it controls, and revenue bills only where it holds the House (Art. I §7 cl. 1, extracted); a party holding neither votes and introduces nothing.
- Per R-US8: the president's levers.
- `SimulationManager.PlayerMayIntroduce` and `GovernmentRecord.RoleOf` read the presidency and the chambers. The Desk chip reads PRESIDENT, THE PRESIDENT'S PARTY, HOUSE MAJORITY, SENATE MAJORITY or MINORITY — words only. No US player reads IN OPPOSITION with nothing to do.

*Needs:* US-7, US-18; R-US7; R-US8 (under (b), the delegations read).
*Done when:* `PlayerRoleDiagnostic`'s US rows re-pinned — on 12 Mar 2024 a DEM player governs as the president's party, and a REP player holds the House majority, may introduce an ordinary bill, and sees it die in the Senate; review row (SimulationManager); bars; the USA dry film; one filmed width of the Desk.
**Built: `COMPLETED.md` §852** - R-US7 (a) by bill kind: the president's party introduces everything and the budget is its alone; a party out of the White House introduces in the chamber it holds, a revenue bill only in the House (the revenue reading read off US-18's reconciliation table: the tax programme and the budget); a party holding neither introduces nothing (risk 18, stated). The chamber a bill begins in is `Congress.OriginOf`, read by the gate and the page (the administration's bills in the House, ASKED). The government is charged only for its own bills (`Congress.GovernmentBears`: the failed vote, a law's enactment, F5's lost day; who bears a congressional majority's bill ASKED). The roles on a day PRESIDENT (the game's own), THE PRESIDENT'S PARTY (of record), HOUSE MAJORITY, SENATE MAJORITY, MINORITY - words only, who governs unchanged. R-US8 built as (a), the delegated tariff powers stated as not modelled on the trade bill; (b) asked. Until US-21 a bill both chambers pass takes effect unvetoed (ASKED).

**US-20 — Poland's statute acts keyed on Poland's own rule. Size M.**
Poland's statute-act split (D4 and E2: §768, §773) keys today on `PresidentialVeto.Applies` — in the simulation (the budget act, the statute acts, the veto gate, the Finance partner's rates), on the Budget page (its slip and lines), in the controller, in `ChamberVerdicts.Veto` and in the film driver's staging. Switching it on for the USA would route US budgets through Poland's acts. The readers move onto `WorldClock.BudgetProcedureOf`, which gains Poland's statute-act procedure (and, at US-22, the US Congress's); `PresidentialVeto` holds its rule per country, Poland's unchanged: F3's at-risk test (B1 widened), the keyed draw at the generated rates (`Generated.PolishVetoRates`), B2 and the Sejm constants. Every reader is found by grep at the item, not from this list — E2 is still adding them. May land any time after E2's commit; nothing before US-21 needs it.
*Needs:* E2 committed.
*Done when:* Poland unchanged, proved by its own instruments — `PresidentialVetoDiagnostic`'s cases and the D4 and E2 cases (the no-policy dump meets no budget bill, §768, so the dump diff alone proves nothing here) — and by the dump diff; `GovernmentBudgetBillDiagnostic` re-pinned; Poland's Budget slip unchanged on Poland's dry film; review rows (SimulationManager, BudgetBill if touched); bars.

**US-21 — The US veto. Size M.** *(R-US17)*
Art. I §7 as a US rule beside Poland's. Every bill both chambers pass goes to the president of the day — the record's or the game's — who vetoes by R-US17's rule; a player-president chooses. The override needs two thirds of each House on the sourced base, voted by the same sides (Poland's premise, DECLARED — and measured and said: under it no Congress of record overrides). The pocket veto is not modelled, stated; the ten days are US-21b's Docket (a player-president's bill is law if unanswered after ten days, Sundays excepted). The signing plate draws the veto and the override in both chambers (a US frame like 89g, through the film driver's staging), and `ChamberVerdicts.Veto` projects it.
*Needs:* US-10 (its rates already in Elias's hands), US-19, US-20; R-US17.
*Owed from US-19 (§852):* a congressional majority out of the White House passes reconciliation kinds unvetoed until this item (US-19's ask F); the veto's and the override's costs compose with `Congress.GovernmentBears`.
*Done when:* a US veto diagnostic on US-10's record cases and on planted bills; a run from 12 Mar 2024 to 20 Jan 2025 prints the bills that reached the president and those vetoed, beside the record's count for the 118th; Poland's veto diagnostics unchanged; review row; bars; REAL films of the veto plate at 1280 and 2560.
**Built: `COMPLETED.md` §853** - R-US17's rule (c) in play: the draw at the gate (`Congress.Divide`; budget votes refused, an amendment neither drawn on nor presented, crossers entered only where they change a verdict), rule (a) for an AI president and the player-president's answer through a hook (its surface owed to US-23), the override at two thirds on the passage's recorded sides, origin first, one rule for the gate and the page (`Congress.OverrideCount`: an uncontested bill overrides on its whole seated base) (`Congress.Present`, `RecordVeto`, `SimulationManager.UsVetoGate`), composed with `Congress.GovernmentBears` through one lost-day writer (`Congress.RecordLost`); the projection on every verdict surface, no chance shown, an override's outcome stated only where the blocs send the bill; the plate's votes on the veto (89h, 89i); `UsVetoDiagnostic` and the 2024 run; `UsVeto.cs` and its catalog moved into Assets/Scripts/Elections byte for byte.

**Ruled after it (Elias's ruling I12, 2026-10-18, item 27 of `docs/ASKS_FOR_ELIAS.md`; installed `COMPLETED.md` §856; verbatim in `docs/RULINGS_2026-10-18.md`):**
- (i) **The Docket.** A bill unanswered after the Constitution's ten days (Sundays excepted) becomes law (Art. I §7). Poland's player-president is the same at Art. 122's 21 days, DECLARED. The pocket veto stays out of scope.
- (ii) **No cost** for a veto or an override: there is no sourced coefficient, and none is invented.
- (iii)-(vi) **as recommended:**
  - an amendment is neither presented nor drawn on;
  - the fund's drawdown is a budget vote;
  - the 2024 run's planted bills are the stand-in for play;
  - the president of the day comes from the game's own sworn contest.
- (vii) **Yes**, that is the override's arithmetic.

**Built (US-21b): `COMPLETED.md` §864** - the Docket. A bill presented to a player-president with no answer on the day is held in its slot (`DocketEntry`), answered through `SignDocketBill` / `VetoDocketBill` (acted on at its next day step, as of the day it was answered, whoever is president then; a veto to the override), and law without signature the day after the tenth day counted, Sundays excepted, from the day after presentment (`PresidentialDocket`). The budget's acts and a continuing resolution are docketed likewise. An AI president answers on the day. Poland's 21 days are DECLARED in the same rule, and no Polish path reaches it before SP-7b. The pocket veto is not modelled. No format step. `UsDocketDiagnostic`. Its surface stays US-23's.

**RULED (Elias's ruling J6, 2026-10-19; installed `COMPLETED.md` §870):**
- (i) An answer acts at once; its money moves on the next day step, as every law's does.
- (ii) A law's date is the day after the tenth counted day, if a law of record that became law unsigned bears that date (one is to be cited); otherwise it is asked again.
- (iii) Poland's terms, at SP-7b: 7 days for the budget act (Art. 224(1)) and for an urgent bill (Art. 123(3)), 21 otherwise (Art. 122(2)).
The build of (i) and (ii) is US-21c; (iii) is SP-7b's.
**Built (US-21c): `COMPLETED.md` §884** - the answer acts at once (`ActOnDocketAnswers`), its money on the next day step; the law's date SOURCED against P.L. 119-101 (presented 29 June 2026, law 11 July 2026). `UsDocketDiagnostic` (c), (f).


**US-22 — The US budget procedure. Size L.** *(R-US9)*
`WorldClock.BudgetProcedureOf` gains the US Congress procedure, sourced at US-14:
- the president's budget request is the government's bill (31 U.S.C. 1105(a), its date read), and the fiscal year opens on 1 October (31 U.S.C. 1102, now behind `FiscalYearData`);
- per R-US9 (a): the appropriation (the spending lines) needs cloture; the rates — and direct spending and the debt limit where they move — ride reconciliation at simple majorities (2 U.S.C. 641(a), (e)); the pension age is its own statute at cloture (641(g); 644(b)(1)(F)); E2's statute-act parts are the machinery;
- per R-US9 as ruled (F8: *"if funding lapses with no continuing resolution, the government shuts down ... The game must be able to produce a shutdown."*): a failed appropriation is followed by a continuing resolution - a bill of its own, at cloture, as H.R. 5371 was on the record; where none passes, funding lapses and the government SHUTS DOWN (the Antideficiency Act, read at US-14): non-excepted discretionary spending stops until an appropriation or a continuing resolution passes, the excepted share sourced at US-14, and the shutdown's days are said on screen. Both acts may be vetoed. The Byrd rule is otherwise not modelled, stated;
- where the AI governs (the player is not the president's party), its budget is tabled as the government's bill (§632) and goes through the same procedure.

*Needs:* US-14, US-18, US-20, US-21; R-US9.
*Owed from US-18 (§851):* the AI president's budget meets Congress here - until then it is voted by the House alone (`Congress.VotesBothChambers`), a DECLARED asymmetry beside the player's own budget, which meets cloture or reconciliation.
*Owed from US-19 (§852):* the budget's failed vote (`ParliamentSystem.ApplyBillResult`) reads `Congress.GovernmentBears` as the other bill kinds do - today a budget pending across a change of presidency is charged to the incoming government while its lost day is not marked (the review's second-pass N1, DECLARED).
*Done when:*
- a US budget diagnostic under the 119th of record: a rates change passes by reconciliation; a contested appropriation dies at cloture and a continuing resolution carries the standing figures; where the continuing resolution dies too, a shutdown opens on 1 October and ends the day a bill passes - the game produces one; a pension-age change is its own act at cloture;
- the US film's budget frame (93b), skipped today as *"THIS COUNTRY'S BUDGET PROCEDURE IS NOT YET MODELLED"* (§694), is filmed;
- Sweden's frame decision and Poland's acts unchanged; `GovernmentBudgetBillDiagnostic` re-pinned for Germany, Poland and the USA;
- `Reviews/` report and rows for every money path touched (BudgetBill, SimulationManager, `FiscalYearData`); bars; the dry film; one filmed width of the US budget slip.
**Built: `COMPLETED.md` §854** - the president's request on 1105(a)'s first Monday in February (DECLARED), voted as Congress's acts on E2's parts: the appropriation at cloture, the reconciliation by a majority, the pension act at cloture, each through both chambers and `UsVetoGate`, independent (DECLARED). A fallen appropriation is followed the same day by a continuing resolution at cloture, uncontested unless the player withholds it (reading (P), PROVISIONAL, asked with (S), (C), (L)); the withhold lapses when the withholding party takes the presidency. Funding lapses the day after its horizon and ends the day one becomes law, its days on screen; the spending stop is not built (the excepted share is BILLED, asked: A-E). The AI president's budget now meets Congress (US-18's owed line closed); the budget's failed vote reads `Congress.GovernmentBears` (US-19's N1 closed). Save format 41. `UsBudgetProcedureDiagnostic` reproduces 1 Oct to 12 Nov 2025 (43 days) by the procedure's arithmetic.

**Ruled after it (Elias's ruling I13, 2026-10-18, item 28 of `docs/ASKS_FOR_ELIAS.md`; installed `COMPLETED.md` §856; verbatim in `docs/RULINGS_2026-10-18.md`).** **Built (US-22b): `COMPLETED.md` §865** - (1) as below, with CBO 61823's output effect as Table 2's quarterly levels held over calendar quarters (the LAPSE family); (2) the withholder's draw FITTED n=13 on the 2025 lapse's cloture motions, lapse resolutions on the record's 3-day spacing; (3) the cadence, release ending a lapse on the next funding tick, the government's own resolution before a lapse signed on the day by a player-president (DECLARED, asked); save format 42. The ruling's items:
- (1) **A lapse defers spending; it does not cancel it.**
  - The lapsed acts' non-excepted outlays stop, and are paid in full when funding returns (back pay, 31 U.S.C. 1341(c)). Mandatory spending runs on.
  - The size and the output effect come from CBO's 29 Oct 2025 estimate (publication 61823) and from CBO's delayed outlays already read.
  - This is option (B), made up afterwards, with 61823's output effect added. It is its own BASELINE family wherever a lapse is reached.
- (2) **(P) stands.** R-US17's draw (H1) is added for the withholder's own senators on each resolution's cloture vote during a lapse, fitted on the record's lapse-period cloture votes and labelled FITTED n=….
- (3) **How a lapse ends:** when an act becomes law, on release, at the change of presidency (built), or through (2)'s defectors.
  - **The cadence:** when no appropriation for the coming year is law the day before the horizon, a resolution is tabled that day, so nothing withheld means no lapse. This fixes the 9-September case and the 21-day case.
  - **The request is tabled on its day whatever is before Congress** (the review's L2).
- (4) The twelve declared readings stand, with no. 7 amended by (3).
- (5) Format 41, as built.

**RULED (Elias's ruling J7, 2026-10-19; installed `COMPLETED.md` §870), on US-22b's asks:**
- (i) Release ends a lapse on the next funding tick: as built.
- (iii) Every lapse of record that has lapse-period cloture votes and an identifiable withholder joins the withholder's fit, and n is printed.
- (iv) Furloughs reach unemployment as BLS counts them (unemployed on temporary layoff, in the household survey), with the headcount from CBO.
- The eve's resolution goes on the Docket with the horizon as its deadline. Unanswered by then, it is signed, so a lapse needs the player's veto or a withhold. This replaces the DECLARED signing on the day.
- The hold rule across quarter ends: as built.
The build of (iii), (iv) and the eve is US-22c.
**Built (US-22c): `COMPLETED.md` §884** - (iii) FITTED n=16 over the lapses of 2013, January 2018, 2018-19 and 2025 (`raw/lapse/earlier/`); (iv) the furloughed (CBO's 650,000) counted in the published unemployment of a month whose reference week a lapse covered whole (BLS, `raw/furlough/`); the eve's resolution on the player-president's docket, deadline the horizon (its last day to answer), signed the next day if unanswered; no format step. `UsBudgetProcedureDiagnostic` (j), (k), (l). Asked: the veto of the eve's uncontested resolution is overridden, so a player-president cannot lapse.


**US-23 — The president's pen. Size L.** *(R-US7, R-US9)*
Where both chambers' majorities belong to the party out of the White House, that majority passes its own budget reconciliation once a fiscal year, before 1 October, and it lands on the president's desk. Its content is DECLARED as the AI finance ministry's book for that party (how the ministry takes a party's stance is read at the item). A player-president signs or vetoes it on the Docket, where it waits until decided; a veto goes to the override; a lapse with no continuing resolution shuts the government down (R-US9 as ruled; US-22). This gives spec §6's *"signing or vetoing bills"* a use against a hostile Congress — without it, the introduction rule leaves a player-president nothing from an AI Congress to sign.
*Needs:* US-21, US-22; R-US7; R-US9.
*Done when:* a desk-bill diagnostic — a DEM player-president facing a REP Congress (the game's or the record's) receives the REP reconciliation once before 1 October; signed, it takes effect; vetoed, the override fails and the standing rates hold; a president whose party holds Congress receives none; review row; bars; REAL films of the bill on the desk and of its veto at 1280 and 2560.

**US-24 — The federal debt limit live (B7) — its own BASELINE family. Size L.** *(R-US21)*
- The dated limit is read through US-14's bridge, with extraordinary measures for R-US21's DECLARED span.
- After that span: Congress's raise or suspension, as a bill through both chambers (reconciliation may carry it, 641(a)(3)) and the veto — or the game's cut of spending to revenue, in R-US21's form.
- The AI-governed USA acts per R-US21; the AI ministry's ratio-trend logic and its borrowed sequester are reconciled per R-US21; `FiscalRules`' US note made live (the limit, the debt subject to it, the measures' days left).

*Needs:* US-14, US-18, US-21, US-22; R-US21.
*Done when:* a debt-limit diagnostic — a Sweden-start world's AI USA meets the limit on its measured timetable, its miss against the CRS window stated, and a US game reinstates the limit on 2 Jan 2025; `Tools/traj_diff.pl` explains every mover per country (the dump's AI USA runs past the binding window); the sentinel re-based with a reviewed digest; `Reviews/` reports (AiFinanceMinistry, SimulationManager, the new Debt-named file); bars; the dry film covers the slip.

**US-25 — D-US asked, part two: Congress, the veto, the limit. Size S.** — after it, **checkpoint 3**
As US-17, against US-15 to US-24 built (questions in §5).
*Needs:* US-24.
*Done when:* as US-17; the PS-6 row reads *playable as either party's nominee*, with what stays owed and the item that pays each.

### Phase G — the later cycles, the campaign, the nominee

**US-26 — The history rolled forward (cross-country). Size M.** *(R-US11)*
Per R-US11 (a): after the game's own election, its last two counts become the next election's prior and loyalty pair for every modelled country — W-G1's gap, *"two elections in one game read the same history"*, Sweden's, Germany's and Poland's alike; the US's two-year cycle makes it acute.
*Needs:* US-16; R-US11.
*Done when:* a diagnostic shows a US world past 5 Nov 2024 predicting 2026 from the game's own 2024, and Sweden's, Germany's and Poland's second elections reading their own; the dump unmoved (it holds no election — proved by the diff); `elec<N>` unchanged; review row if SimulationManager moves; bars.

**US-27 — Midterms. Size L.** *(R-US20)*
`WorldClock.TryNextPollingDay` adds every even year (2 U.S.C. §7): a presidential year counts three contests on one day, a midterm the House and the class whose terms expire; the midterm per R-US20. The 2026 reference is BILLED until certified (the Clerk's 2026 statistics, expected in 2027), never estimated.
*Needs:* US-16, US-26; R-US20.
*Done when:* a midterm diagnostic — a US world from 12 Mar 2024 holds 3 Nov 2026 and seats the game's 120th on 3 Jan 2027, its swing printed against US-13's record; `PollingDayDiagnostic` carries the even years; bars; the dry film.

**US-28 — The two-term limit and the next nominee. Size M.** *(R-US6 as ruled (b); R-US2 sets who the player is)*
The 22nd Amendment as data: each person's elections — NARA's 2016 page read at US-3, 2020's and 2024's on disk — and the game's own. A person elected twice cannot stand. Per R-US6 as ruled (F8): "the term limit doesn't end the run. The player picks the party's next nominee (the Vice President by default) and campaigns on the outgoing record." The Vice President of record is read by date from `records_by_date.md` (the record's VP is the record ticket's running mate; a game-elected ticket's running mate is the party's own, DECLARED until a ticket is modelled). The term-limited president's party fields the Vice President by default, the player free to pick another of the party's eligible people (an AI party takes the default); the nominee stands at factor 1.0 (E1's rule for a later field); the outgoing term's record is the party's in the economic vote, as built. The pick is asked of Design (§5, D-US part two).
*Needs:* US-8; R-US6.
*Done when:* a term-limit diagnostic — a REP win in 2024 bars that nominee (Trump, elected 2016 and 2024) in 2028; the REP player's 2028 nominee defaults to the Vice President of record (Vance) and can be changed; the run continues; the 2028 campaign reads the outgoing term's record; a Harris win leaves her eligible; bars.
**Built: `COMPLETED.md` §848** - the 22nd Amendment as data (`UsTermLimit`: each person's elections from the catalog's NARA winners before the game's first contest and the game's own contests, the second clause DECLARED not reachable); the term-limited president's party fields the Vice President by default (the record's by date in one method, `TryVicePresidentOfRecordOn`, or the running mate on the game's ticket - asked, R-US27); the player's party its pick from the party's people who may stand (DECLARED pool), on a presidential polling day; an AI party the default; every nominee at the future factor; the party out of the White House unnamed - asked against §6 (R-US26); no format step (the pick additive under ruling A); the Parliament tab's nominee row. The pick goes on the later D-US list.

**US-29 — The Fed chair by statute — its own BASELINE family. Size M.** *(R-US12)*
Per R-US12 (a): for the USA the chair's term comes from 12 U.S.C. 242 (read), not the turn cadence (CL-4's stated exception retires for the USA). The president nominates and the Senate confirms by majority (G9's 2013 and 2017 precedents, read). The pool of fictional chairs is kept, and the chair's turn label goes.
*Needs:* US-15, US-19; R-US12.
*Done when:* a Fed-chair diagnostic — the term ends on its statutory date, and a hostile Senate refuses a nominee by the count; the family measured and explained per country (the AI USA's chair runs in every start's trajectory); review row if SimulationManager moves; bars; the dry film covers the bank tab.

**US-30 — The campaign staged on the states. Size L.**
`LiveCampaignSetup.TryFor` gains a US staging beside Germany's:
- the 51 jurisdictions as regions, through a reader on `GermanRegions`' pattern over US-3's catalog;
- a state's audience its valid presidential votes at the previous election, and its eligible electorate sourced (EAC's EAVS 2024 or the Census Bureau's citizen voting-age population) or BILLED;
- both major candidates everywhere; a minor candidate reached only where on the ballot of record (§699);
- casts by §695's rule; Gallup's salience on disk, or a wave dated before the campaign;
- the war chest in dollars BILLED — no US price table invented (Germany's is billed too).

The count reads the run's final shares, then the states (R-US14), then the electors.
*Needs:* US-8, US-9.
*Done when:* a US campaign diagnostic in the simulation bar, on `GermanCampaignDiagnostic`'s pattern — an idle campaign reproduces the day's prediction exactly; the dry film covers Campaign HQ; review row if a money path moves.

**US-31 — The state-by-state count and the AI's targeting. Size L.** *(R-US16)*
Per R-US16 (b): the USA counts each state with the ground game — W-D1's `ElectionDay` over the campaign's regional mobilization, with its declared regional noise — on top of R-US14's derived shares. The AI targets by a DECLARED rule (electors × closeness on the last published poll; `ELECTIONS_CAMPAIGN_SPEC.md`'s swing regions) in place of the largest audiences, `[AUTHORED-DRAFT]` on the play-calibration list. Under R-US16 (a) this item is not built, and Campaign HQ says the campaign is national in effect.
*Needs:* US-30; R-US16.
*Done when:* the campaign diagnostic shows the AI's local acts concentrated where the rule says, and a player's ground game in one state moving it by US-9's measured amount, able to flip it; Sweden and Germany unchanged unless R-US16 (c) — then their backtests and films move (`elec<N>`, their films), not the sentinel, since the dump holds no election; review row; bars; the dry film.

**US-32 — GPS 2019 read; the drafted people-versus-elite replaced. Size S.**
Save the GPS 2019 datafile and codebook (Harvard Dataverse, doi:10.7910/DVN/WMGTNS). Re-read the roster's typed economic, social and immigration values, naming every difference, and read V8 to replace the `[AUTHORED-DRAFT]` people-versus-elite values. Take the experts' spread, if the file carries per-expert placements, as each party's range for US-33, and re-derive the casts by §695's rule. A switch of source is Elias's.
*Needs:* US-0 (the read can run ahead; the roster change lands here).
*Done when:* `party_positions.md` cites the saved file's digest; `CampaignCastDiagnostic` re-pinned; the family explained if a trajectory moves; bars.
**Built: `COMPLETED.md` §837** - the GPS 2019 saved out of tree (G7) and read into `gps_2019_usa.csv` / `UsGps2019` by `Tools/us_gps_prep.pl`; V4/V6/V10 equal at two decimals; people-versus-elite on V8; the experts' spread extracted (US-33's range); the casts unchanged; V9 (REP -> Populist) HELD and V19's reading asked (G7).

**US-33 — Your own nominee (SP-6a). Size L.** *(R-US4)*
The creation flow is offered for the USA as a leader at the head of the DEM or REP ticket, with positions inside the party's range (US-32's spread if read, else DECLARED); the nominee stands at factor 1.0, and the character is the person the term limit reads (US-28). The third-party run is added only if R-US4 keeps it inside PS-6, with its billed rules.
*Needs:* US-28, US-32; R-US4.
*Done when:* a US game starts with its own nominee and plays to the count, which reads that nominee's position through the poll; `StartPointsDiagnostic` and `StartBriefDiagnostic` extended; the flow's US form filmed REAL.
**Built: `COMPLETED.md` §850** - the creation flow's US form (four steps, from the party panel's YOUR OWN NOMINEE); the range the middle half of the GPS 2019 experts' placements (DECLARED, re-derived from the by-expert file; the alternatives asked with their figures); a person of record refused by surname and first initial; the party's point the nominee's in the poll and the compass, the electorate held; one authority, the registry; the 2024 field at factor 1.0, its tally marked, the person the limit reads; the brief's nominee clause; no format step (additive under ruling A); R-US15 (iii)'s response printed, not acted on.

### Phase H — the night, the boards, the close

**US-34 — D-US's night built. Size L.**
Once D-US part one's boards are stamped LOOKED AT (§707): `ElectionNightFromModel.Available` for the USA; `ElectionNightScreen`'s US branch with a state tile view (`LaenderTileView` its model), the House and the Senate class, and the inks; deviations stated, answers recorded.
*Needs:* US-17 answered and stamped; US-16.
*Done when:* the night appears on a US polling day; REAL films at 1280 and 2560, a partial count included (as `CaptureGermanNightPartial` does for Germany); the controller no longer logs "no election night" for the USA.

**US-35 — D-US's Congress, veto and limit built. Size L.**
Board by board, once stamped LOOKED AT: the Congress view, the signing plate's veto and override, the president's desk, the debt-limit slip, the role chips, and the start card with both chambers.
*Needs:* US-25 answered and stamped.
*Done when:* REAL films of the signing plate and the start card at 1280 and 2560; the dry film; the cheap bar.

**US-36 — PS-6 closed. Size M.**
Every Canvas surface filmed REAL at 1280 and 2560: the start card, the signing and veto plates, the night. A US game is filmed from 12 Mar 2024 through the 2024 count, the oath, a bill through both chambers, a veto, the first budget, the debt limit's episode and the 2026 midterm; the driver stages each outcome (a DEM win, a REP win) as `-shotformation` stages a formation. An ultrareview is suggested to Elias and gates nothing (§649).
*Needs:* US-26 to US-35.
*Done when:* each film's SHOT lines are checked — what it captured, never its exit code; every guard passes; every bar is green on the committed tree; PS-6 + SP-6 and D-US are closed in the list, each with its §, and what stays owed is listed.

---

## 4. The rulings

### 4.1 Asked 2026-10-04 — RULED 2026-10-05 (Elias's ruling F8)

**R-US1 — Congress and the president as of record, by date.** *(blocks US-2; shapes US-8, US-15, US-16)*
§618's ruling 4: *"until then its chamber and head of state hold as of record"*. Today a US game keeps the start's: Biden and the 118th House govern for the whole run.
- **(a) By date:** whatever the game does not elect is seated on its record's dates — the 119th Congress at noon on 3 Jan 2025, and the president of record at noon on 20 Jan 2025 until the game holds the presidential election (US-8); after that only Congress, until the game elects it (US-16). A chamber not yet sourced (the 120th) is not seated: the 119th stands, said on screen. No coattails until US-16.
- **(b) As at the start:** the start's chambers and president hold until the game elects them, said on screen.

*Code recommends (a):* it is the record's own reading of "as of record", and it makes a US game true from its first year.

**RULED** (Elias's ruling F8, 2026-10-05): (a), as recommended.

**R-US2 — Who the player is in 2024.** *(blocks US-6; decides US-28's consequences)*
The start, 12 Mar 2024, falls before the Democratic ticket changed: from memory, Biden was the presumptive nominee, withdrew in July 2024, and Harris was nominated in August. The dates are read from the parties' own records — the RNC's site refuses this machine (G1).
- **(a) The November nominee of record:** Trump (REP) or Harris (DEM), carrying that candidate's fitted factor from the start. Named on screen from the nomination's date of record, *"the Democratic/Republican nominee"* before it. Biden governs as president until noon on 20 Jan 2025; the withdrawal is not replayed.
- **(b) The presumptive nominee on the start day, by the party's record:** Biden (DEM) or Trump (REP) — from memory, both reached their delegate majorities on 12 March 2024 itself. The player stays that person, and an AI DEM ticket follows the record by date. Biden has no 2024 general-election result of record, so his factor is 1.0 (E1's rule for a field the record does not hold).
- **(c) The nominee of record by date in every world:** Biden until the record's change, Harris after it — the withdrawal replayed on its date, for a DEM player too.
- **(d) Move the US start** to the day both nominations are of record (August 2024). The question disappears, the run-up shrinks from 34 weeks to about 13, and spec §4's standard run-up is amended.

*Code recommends (a):* decision 6 already names *"the party's nominee of record"*, E1's factors are fitted on the November field, and no model can decide a withdrawal. The consequences for R-US6: under (a), a REP win seats Trump, elected 2016 and 2024, so the two-term limit binds in 2028; a DEM win seats Harris, eligible in 2028. Under (b), a DEM win seats Biden, elected 2020 and 2024 — barred in 2028 too. (Under R-US6 as ruled (b) the limit binds the person, never the run: the term-limited party's next nominee is the player's pick, the Vice President by default.)

**RULED** (F8): (a), as recommended - the November nominee of record, Trump (REP) or Harris (DEM), with that candidate's fitted factor from the start; the withdrawal is not replayed.

**R-US3 — No candidate at 270.** *(blocks US-8)*
A 269–269 split is reachable with two parties.
- **(a) The 12th Amendment as written** (its text is on disk in NARA's saved amendments page, to be extracted): the House seated on 6 January chooses by state delegations — one vote per state, a majority of all the states to elect. That is the game's House where it elects one (US-16), else the record's. A tied delegation casts no vote (DECLARED). The Senate's choice of the Vice President, and a House that cannot choose, are not modelled, stated.
- **(b) Report "no majority — not modelled"** and keep the incumbent, which is wrong after noon on 20 January.
- **(c) The plurality of electors elects** — a fiction.

*Code recommends (a).*

**RULED** (F8): (a), as recommended.

**R-US4 — Third parties and SP-6's two forms.** *(needed for US-0's spec note; blocks US-33)*
- **(a) Reconcile:** PS-6 builds the major-party nominee (R-US2) and, late, your own nominee at the head of a major ticket (SP-6a, US-33). The third-party run becomes its own later row, once ballot access in 51 jurisdictions, public funding (26 U.S.C. 9001–9013, including §9004's rule) and a vote-share home on a two-party roster are sourced. Decision 6's *"not playable"* is amended to say so, and decision 10 points to the row.
- **(b) Decision 6 stands:** the nominee of record only; decision 10 struck.
- **(c) Decision 10 as written:** both forms inside PS-6.

*Code recommends (a).*

**RULED** (F8): (a), as recommended - decisions 6 and 10 reconciled in both specs; the third-party run is its own later row, SP-6b.

**R-US5 — The USA's elections in other countries' games.** *(scope)*
§618's ruling 4 and decision 3 owe every modelled country's elections in every run. Today only the player's country votes — Germany and Poland included — and no record transition applies to an AI country.
- **(a) A cross-country row:** PS-6 builds the US count as a pure function of a world's state (US-8), and one later cross-country row makes every modelled country vote on its own calendar as an AI country, seating the record by date where a country's model does not exist. That row moves every start's trajectory, so it is its own family.
- **(b) Inside PS-6,** for the USA alone.

*Code recommends (a).*

**RULED** (F8): (a), as recommended - the cross-country row is PS-8.

**R-US6 — The two-term limit and the end of a run.** *(blocks US-28)*
- **(a) The standing words** (spec §6 and §7): the run ends when the limit binds the player's nominee. Code reads that as the end of the last term the person may hold — noon on 20 January 2029 for a 2024 winner elected once before — the 2028 contest not being the player's. Under R-US2 (a), a REP player who wins in 2024 ends there: the most successful REP run from 2024 is the shortest.
- **(b) Amend §6 and §7:** the limit binds the person, not the run. The party's next nominee is the player's — an unnamed nominee at factor 1.0, or SP-6a's own — and the run ends only when the player ends it or declines.

*Code recommends (a)* because it is the standing decision and only Elias amends it; the consequence is named so that he can strike it.

**RULED** (F8): (b), as Elias amended it - "the term limit doesn't end the run. The player picks the party's next nominee (the Vice President by default) and campaigns on the outgoing record." `POLITICAL_SYSTEM_SPEC.md` §6 and §7 amended to say so; US-28 builds it.

**R-US7 — Who introduces bills, and what reaches the president's desk.** *(blocks US-19, US-23)*
- **(a) Divided government both ways:**
  - the president's party introduces the administration's bills and proposes the budget (31 U.S.C. 1105(a));
  - a party out of the White House introduces ordinary bills in any chamber it controls, and revenue bills only where it holds the House (Art. I §7 cl. 1);
  - a party holding neither the presidency nor a chamber votes and introduces nothing;
  - where the party out of the White House holds both chambers, its majority — the AI's when it is not the player's — passes its own budget reconciliation once a fiscal year and sends it to the president, so a player-president has a hostile Congress's bill to sign or veto.
- **(b) As built:** only the president's party introduces. A player out of the White House sends nothing, and a player-president never meets a bill from an AI Congress.
- **(c) Any party may introduce,** since committee and floor control are not modelled.

*Code recommends (a):* divided government is the spec's mechanic (§6), and (a) gives the veto a use from both sides.

**RULED** (F8): (a), as recommended.

**R-US8 — The president's levers.** *(blocks US-19)*
- **(a) Only the veto and appointments** (with the Senate's consent) are the president's alone. Every dial — the tariff included — is a bill through Congress, and the president's delegated tariff powers are named as not modelled.
- **(b) The tariff dial is the president's** under the delegating statutes (19 U.S.C. 1862 and 2411; the IEEPA use, whose litigation is read by date), each read and dated; everything else as (a).

*Code recommends (b)* if those statutes and the litigation's state are read by date when US-19 is built, otherwise (a), stated. From memory, the US tariff moved by presidential action through 2025, so (a) misstates the period a US game plays.

**RULED** (F8): as recommended - (b) where the delegating statutes and the litigation's state are read by date when US-19 is built, otherwise (a), stated.

**R-US9 — The US budget's path through Congress.** *(blocks US-18, US-22, US-23)*
- **(a) Two paths, on E2's statute-act machinery and by E2's principle:**
  - the appropriation needs cloture — the game's spending lines DECLARED as one appropriation, since they are not split into discretionary and mandatory;
  - the rates, and direct spending and the debt limit where they move, ride reconciliation at simple majorities (2 U.S.C. 641(a), (e));
  - the pension age is its own statute at cloture: 641(g) bars Social Security title II from reconciliation, and 644(b)(1)(F) makes it extraneous — both on disk;
  - a failed appropriation continues the standing figures as a full-year continuing resolution, the game's premise said on screen; no shutdown is modelled;
  - both acts may be vetoed; the Byrd rule is otherwise not modelled, stated.
- **(b) As (a), but a funding lapse is a shutdown** under the Antideficiency Act (31 U.S.C. 1341–1342): non-excepted discretionary spending stops until a bill passes. The excepted share must be sourced.
- **(c) One reconciliation bill** for the whole budget at simple majorities: every trifecta passes everything. 641(g) rules it out for the pension age.
- **(d) One bill under cloture:** almost nothing passes.

*Code recommends (a).*

**RULED** (F8): (a), with Elias's change - "if funding lapses with no continuing resolution, the government shuts down, as it did from 1 Oct to 12 Nov 2025 (43 days). The game must be able to produce a shutdown." So the continuing resolution is a bill of its own (on the record it needed cloture: H.R. 5371 failed the Senate's cloture from 19 September and passed only after cloture was invoked on 9 November 2025), and a lapse with none shuts the government down under the Antideficiency Act until an appropriation or a continuing resolution passes. The lapse of record, read 2026-10-05 from govinfo: P.L. 119-4 funded the fiscal year *"ending September 30, 2025"*; H.R. 5371 became P.L. 119-37, the House agreeing and the President signing on 12 November 2025. US-14 brings the pages in and sources the excepted share; US-22 builds it.

**R-US10 — The Senate's independents.** *(blocks US-12's seating, US-15)*
- **(a) With their caucus:** each counted with the conference it caucuses with, sourced and dated (the 118th's caucus is read at US-12); the chamber card says *"including N independents"*.
- **(b) Their own unit with no position:** they abstain on contested bills and never supply a cloture vote, misstating the 118th.

*Code recommends (a).*

**RULED** (F8): (a), as recommended.

**Read at US-12 (§792):** King and Sanders caucus with the Democrats by the Senate Democrats' own lists of 2024 and 2026, Sinema by the 2024 list, Manchin as an independent by the Senate's Changed Parties page ("he continued to caucus with the Democrats"). **Sinema is a reading put to Elias:** the Senate's page says she organized through the Democratic Conference "but would not participate in either party caucus"; the catalog counts her with the Democrats, the conference she organized through and the list that names her (`ElectionsData/usa/senate_record.md`, reading 5) - the alternative, an independent in no caucus, is a third unit in a two-unit chamber, US-15's question.

**Sinema, RULED (Elias's ruling G3, 2026-10-06; installed §797):** counted with the Democrats from the day she left the party, because the Democrats' own list shows her - a READING, DECLARED in the data note (`senate_seats.csv`'s `caucus_by`, `senate_record.md` reading 5). US-15's independent in no caucus stays an open question, not a build item.

**R-US11 — The history after the game's own election.** *(blocks US-26, US-27)*
W-G1's standing gap: every election in a game reads the epoch's history, so a US game would predict 2026 and 2028 from the history it predicted 2024 with.
- **(a) Roll forward:** the game's own last two counts become the next election's prior and loyalty pair, as one cross-country item (US-26) for every modelled country. No family is expected, since the dump holds no election; the diff proves it.
- **(b) Keep the epoch's history,** stated on the reference view.

*Code recommends (a).*

**RULED** (F8): (a), as recommended - US-26 rolls history forward for every modelled country, Poland's standing history gap (§767) included.

**R-US12 — The Fed chair in a US game.** *(blocks US-29)*
- **(a) The statute:** the four-year chair term (12 U.S.C. 242, read), the president nominating and the Senate confirming by majority (G9's precedents read); it replaces the turn cadence for the USA, as its own BASELINE family.
- **(b) The cadence:** keep the turn cadence, stated (CL-4), the appointment the governing party's as built; statutory appointments a later row.

*Code recommends (a):* spec §6 makes appointments a presidential power, and the Senate's consent is half of it. It is built late, once the Senate exists.

**RULED** (F8): (a), as recommended - built late, once the Senate exists.

**R-US13 — The electors from 2032.** *(needed before a run reaches 2032)*
The 2030 apportionment is due by 31 Dec 2030 and goes to Congress in January 2031 (13 U.S.C. 141(b); 2 U.S.C. 2a(a) — probed, saved at US-3).
- **(a) Carry forward** the 2020-census allocation, labelled on screen, until the 2030 apportionment is sourced.
- **(b) Project** by equal proportions on a stated population projection — an estimate, labelled.

*Code recommends (a).*

**RULED** (F8): (a), as recommended.

### 4.2 Asked with their measurements

**R-US14 — How a state's vote follows the national vote** *(US-4's table; blocks US-8)*.
- (a) §689's normalised uniform swing from the previous presidential election;
- (b) proportional swing;
- (c) plain additive swing (Poland's okręg form).

Code will recommend the method calling the most states right on 2016→2020 and 2020→2024 at the true national shares, (a) on a tie, for one rule across countries. 2012→2016 is reported as the stress case. No demographic layer is added unasked; the misses go into the card.

**RULED** (Elias's ruling G2, 2026-10-06; installed §797): (a), as recommended - §689's normalised uniform swing.

**R-US15 — Which vote the US poll is** *(US-5's table; blocks US-6)*.
- (a) The House national vote — the party's own vote, held on every federal polling day, midterms included: the history is the last two House elections, and the presidential candidates carry it by E1's factors;
- (b) the presidential popular vote as the history (2020 against 2016 at the start), midterms then reading the last two presidential votes;
- (c) two histories, one per contest.

The electorate is fitted free where it is identifiable, and otherwise with its spread held at the fitted countries' values (DECLARED). Code expects to recommend (a), unless its national miss is well above (b)'s.

**Asked §789, with the measurement** (`docs/reference/US_ELECTIONS.md`, *The readings (US-5)*; the figures are the card's, the record's `COMPLETED.md` §789):
- **The vote: code recommends (a)**, by the rule above - the card's verdict line, which the instrument generates from the 2024 rows and checks (a verdict the rule leaves to Elias would fail it, to be asked). Its out-of-sample line names the closer history for each earlier election: the two histories split. With two parties the vote model gives the prior back - checked on every fit - so the comparison is between each history's last election and the one it predicts, plus the economic vote. **(c)**, a history per contest, would pay only where the other contest's own history is the closer: the verdict line compares (a) with (b) on the presidential vote as well; and it costs an API change (one poll a country today, `NationalElection.TryPredictShares`).
- **The spread** (the rule's "otherwise" made concrete). The free fit is not identifiable: every spread the card fits holds T−1's split at the fitted points. Away from them the spread decides two things the card prints, spatially: how far the split moves when a party's point moves (US-33's own nominee off the party's point - the card's moved point), and what a third placed unit takes (SP-6b's third party, a created party - its midway unit). The US two-party record holds neither a moved point nor a third placed unit, so it cannot choose. Options:
  - **(i)** one country's pair - Sweden's, Germany's, Poland's or Italy's;
  - **(ii)** the four's median, σ and τ each the median of the four;
  - **(iii)** leave the choice to the first items that leave the fitted points - US-33's moved point, SP-6b's and a created party's third units - each measuring its own response, holding (ii) until then.

  **Code recommends (iii), holding (ii).** No country's pair has a claim on the US: each is its own election's fit, and each sits on a bound of `VoteModel.Calibrate`'s grid in at least one parameter (`PartySystems.TryElectorate`'s values against the grid). The instrument places the mean on the DEM-REP segment where the history's last election falls; US-6 can hold it there.
- **Noted, not asked:** 2024's economic vote moves both histories' 2024 polls toward DEM by the same amount (the card's term line). The term reading compares its two ends (§752), so 2022's inflation peak is not in it. It is the economic vote as built, printed in its own column; the House history reads the presidential row, the congressional one being US-13's.

**RULED** (Elias's ruling G2, 2026-10-06; installed §797): (a), the House national vote, **with the spread held at the four countries' median** - Elias's words, read as (ii) held; the later items' own measurement of their responses ((iii)) is not ruled and stays theirs to print. The 2024 economic-vote note is accepted as filed (G8).

**R-US16 — What "fought state by state" means** *(US-9's measurements; blocks US-31)*.
- (a) National in effect, as Germany's Länder are today, with the HQ saying so;
- (b) for the USA, a per-state count with the ground game on the derived shares, plus a DECLARED targeting rule for the AI (electors × closeness on the last published poll);
- (c) (b) for every staged country — Sweden's and Germany's backtests and films then move, the sentinel not.

Code expects to recommend (b), sized by US-9.

**Asked §846, with the measurement** (`docs/reference/US_ELECTIONS.md`, *The campaign geography - R-US16*; the figures are the card's):
- **(a), the count as built, is national in disguise, and at the US audience not even national.** Working a state moves it exactly as much as every state it was not worked in. No US campaign moves the national total either: every run closes both parties past the compatibility ceiling (115-1265 before the clamp at 100; the persuasion scale is per person reached, [AUTHORED-DRAFT]). The AI makes almost no local acts at this scale, and its offices sit in the largest audiences.
- **(b) would make the ground game count:** the scripted program moves Georgia about 0.8 points of margin; the mobilisation ceiling moves a state 2.90-9.00 points and could flip 13 calls (139 electors). W-D1's declared noise flips the closest states in up to a quarter of counts and the presidency in none of 400.
- **(c):** Sweden's and Germany's ground games move 0.035 and 0.003 points and no seat; W-D1's frame alone moves Germany 2 seats with no campaign.
- **Code recommends (b)** for the USA. (c) is not recommended until W-D1's frame reads each region's own turnout.
- **Also asked:** does R-US16 gate US-30's staging (risk 1), or only US-31 (the needs lines)? Code recommends the needs lines; if so, risk 1's last sentence reads "... before the count or the AI's targeting changes".

**RULED (Elias's ruling I3, 2026-10-18; installed `COMPLETED.md` §856; verbatim in `docs/RULINGS_2026-10-18.md`):** **(b) for the USA.** It gates US-31 only, so US-30 stages now (risk 1's last sentence reads "... before the count or the AI's targeting changes"). The ground game's state effect enters the presidential share, the Senate race and R-US18 (b)'s House swing alike.


**R-US17 — Which bills the president vetoes, and the override's base** *(US-10's document; blocks US-21)*.
- (a) B1 transposed: a bill a majority of the president's party's House members voted against;
- (b) a bill the president's party opposed in either chamber;
- (c) a rule fitted on the record.

The override is two thirds of those present and voting, a quorum present, as the interpretive source reads it. Code will recommend on the confusion matrix — (a) only if its precision on the US record is at least B1's on Poland's — and STOPS and re-asks if every rule names far more bills than the record vetoed, or if a party-bloc Congress under Rule XXII never sends a vetoable bill. That last is the US risk, the mirror of E4's 43 of 43.

**STOPPED AND RE-ASKED** (`COMPLETED.md` §802, with `docs/generated/US_VETO_BACKTEST.md`). The STOP is R-US17's own: under this plan's premise - each party voting as one, so one stance in both chambers (spec-risk 4) - a party-bloc Congress never sends a vetoable measure. Of the 34 measures the president's party opposed in a chamber, none reaches him: the other party never held both chambers in the 115th-119th Congresses (the seats are in the document). On the matrix alone code would recommend (a): its pooled precision is 22 of 33 (66.7 %) against B1's 45 of 166 (27.1 %) on Poland, its hit rate 22 of 25, and no rule falls below B1's precision. Read chamber by chamber instead, as the members voted there (the reading declared before the first run; the s802 review showed it departs from the plan's premise), a bloc Congress sends 13 vetoable measures (Trump's first term 9, Biden 4) and 5 of the 25 vetoed measures of record. Every CRA, War Powers, arms-sale, national-emergency and D.C. disapproval vetoed passed the Senate on defectors, which a bloc model cannot produce. Trump's two second-term vetoes passed both chambers without a roll call, so no rule names them.

**RULED** (Elias's rulings H1-H3, 2026-10-13; recorded §803; built after the gameplay audit, `docs/audit/AUDIT_PROTOCOL.md`):
- **H1 - the third option, (c), on the USA's veto path only.** Parties stay blocs by default. A seeded draw of defectors per party per chamber is added, fitted on the recorded chamber-by-chamber votes since 2017; the draw matters only where the chamber's margin is within the drawn count. Rule (a) then decides the veto on a bill that reaches the president against his party. Budget votes, the other five countries and Poland's B1 stay as they are, and Poland is proved unchanged on the simulation bar.
- **H2 - the fit is thin** (13 vetoable measures, 25 vetoes): every fitted figure prints its n and an interval beside it, labelled `FITTED n=…`. Trump's two 2025 vetoes, with no recorded vote, stay declared unpredictable and are never counted for or against.
- **H3 - preregistered:** the new rule is preregistered and its digest taken before its first run; the veto tool's mutation suite is re-run, plus one case for the superseded-vote guard.

<!-- R-US17 RULE BEGIN -->
```text
R-US17'S RULE (c) - Elias's rulings H1-H3: a seeded draw of defectors on the USA's veto
path, fitted and measured on the record before it reaches play (US-21). Preregistered in
COMPLETED.md section 832, specified by a design panel blind to every figure of the rule;
amended at section 833 (Elias's answer of 2026-10-16 on S2's reading, before any run).

"Asserted" means the named check (by default the instrument) fails while the statement is
false. A DEFECT is a code or data failure: it fails the check that finds it (the
instrument; in play, US-21's diagnostic) and is never one of the rule's outcomes or
registered ASKED. A DEFECT that only a ruling clears (a re-pin, a changed UNPREDICTABLE
set) is put to Elias as that ruling. "sNNN" and "sMMM" stand
for the COMPLETED.md section a later commit takes. Items of US-21's design that this rule
does not decide (ChamberVerdicts' projection, the plate, the gate's name, US-21's 2024
run) are US-21's text, outside this block.

==========================================================================================
THE TERMS
==========================================================================================
- THE RECORD: ElectionsData/usa/us_veto_measures.csv exactly as Tools/us_veto_prep.pl
  writes it, read as Tools/us_veto_backtest.pl's read_csv reads it (lines starting "#"
  skipped, the header taken by name, every row with the header's field count).
  SourceDigest = SHA-256 over its raw bytes. The rule reads nothing else of the US record
  and never the member XML.
- THE PINS (filled at the preregistering commit, s832, by hashing bytes before the block's
  digest was taken; no figure computed):
    SOURCE PIN: sha256 505c3fa19ec816d280f8df29c00165d9987ee1b72cbc5a154164e1282e845dec
    PREP PIN: git blob 358eba1c4a3f1dd0af7e65dc91feaae20ba77558 of Tools/us_veto_prep.pl
  A run whose SourceDigest or prep blob differs from a pin is a DEFECT until an amendment
  naming Elias's ruling re-pins it.
- B1'S YARDSTICK: 45 vetoed of 166 named (precision 45/166), recomputed from
  ElectionsData/poland/third_readings_term10.csv exactly as us_veto_backtest.pl does. Any
  other result is a DEFECT.
- A MEASURE'S IDENTITY: "<congress>|<measure>" (e.g. "116|H.R. 6395").
- ORIGIN: the House if measure begins "H.", the Senate if it begins "S."; else a DEFECT.
- VETOED: the outcome contains "veto". NOT VETOED: "signed" or "law without signature".
  Any other outcome is a DEFECT.
- UNPREDICTABLE (H2): a vetoed row where neither house_final nor senate_final begins
  "roll ". Asserted: exactly {119|H.R. 131, 119|H.R. 504}. Any other set is a DEFECT until
  the block is amended by a ruling.
- COUNTED MEASURES: every row not UNPREDICTABLE, in CSV row order; M = their number.
  COUNTED VETOES: the vetoed counted measures; V_COUNT = their number, computed, never
  typed (23 on the preregistered extract).
- COUNTED ONLY: every count, sum, denominator, cell, rate and digest of this rule reads
  counted measures only. The exceptions, none of which enters any figure, denominator or
  decision: THE US-10 IDENTITIES (THE FIRST RUN, item 10), which read all rows only to
  verify the re-implementation, print as "US-10's record (s802), not this rule's figure"
  and reprint no 25-based rate; the UNPREDICTABLE set's own derivation over all rows, its
  line, its sustained override votes' line and its digest entries (by name); and THE
  FIRST RUN item 1's counts "in all".
- A ROLL CALL: a house_final or senate_final that begins "roll <n>"; any other final is NO
  ROLL CALL. Its IDENTITY is "<H|S>|<congress>|<that final date yyyy-MM-dd>|<n>". A roll
  call final for several counted measures is ONE roll call everywhere. Its OWNER is the
  first of them in CSV row order, and every shared roll call is printed with its measures.
  Asserted: every roll call of a counted measure is dated 2017-01-03 or later (H1, "since
  2017").
- PRESIDENT, PP, OP: the row's president and president_party (the decision president;
  veto_record.md reading 2). OP is whichever of R and D is not the PP. Asserted: every
  counted row's PP is R or D. In play the president is the one of the vote's day: the
  game's seated president where PresidentialElection.PresidentAt finds a decided contest,
  otherwise the president of record from WorldClock.TryGovernmentAt (ExecutiveKind
  Presidency; PP its party, REP or DEM). PresidentAt alone finds no US president of record
  (PresidencyOfRecord holds Poland's only). No president found on a US vote day is a
  DEFECT. A president found with no party has no PP, so no draw is made and (a) never
  fires (DECLARED default, by analogy to G5 items 14-15, Poland's s778 readings).
- THE PRESIDENT ON A DATE (R9 only; the CSV carries the decision president alone): by the
  20th Amendment's noon of 20 January (the rule records_by_date.md applies), D before
  2017-01-20, R from 2017-01-20 to 2021-01-19, D from 2021-01-20 to 2025-01-19, R from
  2025-01-20.
- A LABEL'S SPLIT on a roll call (R, D or I): its yea, nay, present and not_voting columns.
  A yea of "-" is no split. MEMBERS = the four summed; VOTING = yea + nay + present.
- BLOC STANCE of R or D on a roll call (us_veto_backtest.pl bloc_passes and
  party_bloc_opposed): YEA iff yea > nay + present, otherwise NAY (a tie is NAY; present
  stands against, F3).
- F3 SET: the counted measures on which opposed_f3 holds in the House or the Senate
  (nay + present > yea for the PP); US-10's 34 is its count over all rows.
- INDEPENDENTS:
  - A Senate independent adds to the SEATS and to S of the bloc that senate_I_caucus names
    (each space-separated "C:n" entry with C in {R, D}; any other entry stays outside and
    is printed). He is in no split, stance or observation, but a drawn crosser may be any
    member of S.
  - A House independent is in no bloc. He sits inside the House sworn count, counting
    against.
- SEATS OF A CONGRESS (literally us_veto_backtest.pl's %seatsOf, over counted rows):
  1. Order the counted rows by house_final_date with ordinal comparison, stable, ties in
     CSV row order.
  2. For each row in that order and each chamber whose final is a roll call: let t = (R
     members, D members, sworn = R + D + I members), with the Senate's caucus entries
     added to R or D.
  3. Replace the stored entry for "<congress> <chamber>" iff there is none or the row's
     <ch>_final_date >= the stored date (ordinal). Then set the stored date to the row's
     <ch>_final_date IN EVERY CASE (the code's overwrite, kept on purpose).
  Asserted: R, D and sworn per Congress and chamber equal the seats table printed in
  docs/generated/US_VETO_BACKTEST.md. A difference is a DEFECT.
- KINDS: EXPEDITED iff kind is exactly one of "CRA disapproval", "arms-sale disapproval",
  "War Powers", "national emergency", "D.C. disapproval" (ordinal, case-sensitive). These
  are 5 U.S.C. 802, P.L. 94-329 s601(b), 50 U.S.C. 1622 and D.C. Code s1-206.04. Every
  other kind is RULE XXII, and a kind other than "bill" or "joint resolution" is printed.
- RECORD BUDGET CLASS, from the title field, upper-cased invariant, ordinal Contains:
  - RECONCILIATION: "PROVIDE FOR RECONCILIATION", "PROVIDING FOR RECONCILIATION",
    "RECONCILIATION PURSUANT TO", "PURSUANT TO SECTION 2002 OF THE CONCURRENT RESOLUTION";
  - APPROPRIATION: "MAKING APPROPRIATIONS", "MAKING CONTINUING APPROPRIATIONS",
    "MAKING FURTHER CONTINUING APPROPRIATIONS", "CONTINUING APPROPRIATIONS",
    "APPROPRIATIONS ACT";
  - DEBT LIMIT: "DEBT LIMIT", "FISCAL RESPONSIBILITY ACT", "BIPARTISAN BUDGET ACT".
  Every match is printed with its test. Every counted title containing "APPROPRIATION",
  "RECONCILIATION" or "BUDGET" that matches no test is printed as a NEAR MISS.
- SENATE PROCEDURE of a counted measure: MAJORITY (expedited) if its kind is EXPEDITED;
  else MAJORITY (reconciliation, US-18's 2 U.S.C. 641(e) path) if it matches a
  RECONCILIATION test; else RULE XXII.
- THE VETO PATH (record), V: the counted measures with at least one roll call on which the
  PP's bloc stance is NAY. |V| is printed beside F3's 34, with every measure in one and not
  the other (they differ only at yea = nay + present).
- V_XXII: the measures of V whose SENATE PROCEDURE is RULE XXII.
- THE VETO PATH (play): an ORDINARY LEGISLATION bill on which the PP's side in a chamber's
  division (as US-18 sets it) is AGAINST.
- BUDGET VOTE (play): every act the US budget procedure tables (WorldClock.BudgetProcedureOf,
  US-22: the appropriation, a continuing resolution, the pension-age act), a reconciliation
  bill (US-18), and US-24's debt-limit act. The debt-limit act is the panel's default
  (US-22 has the debt limit ride reconciliation; 641(a)(3)), with R18 printed. Every other
  US bill is ORDINARY LEGISLATION.
- SENT: both chambers pass under the model. NAMED: sent, and rule (a) fires. VETOABLE SENT:
  a measure of V that is sent.
- THE CELLS: HP and SP, the PP standing NAY in the House and the Senate. HO and SO, the OP
  standing YEA on those same roll calls.
- N_MIN = 12 observations: a floor preregistered with no data read. A data-free yardstick
  for the floor only: at n = 12 the 95% Wilson interval of a share at p = 1/2 is no wider
  than 0.5 (0.492; 0.509 at 11). The interval printed for a cell proportion is
  Clopper-Pearson (THE PRINT), which is wider at the same n.
- EXACTNESS: every comparison in THE DECISION is made on rationals (BigInteger numerator
  and denominator). Equal is never "below".

==========================================================================================
THE MODEL (what is drawn, from what, per what)
==========================================================================================
1. Parties stay blocs by default (H1): a party votes as one, and its bloc carries every
   seat it holds in the chamber. Nothing is drawn or recorded outside item 2.
2. WHERE A DRAW IS MADE (H1, "on the USA's veto path only"): only for a bill on the veto
   path, once per party per chamber per bill.
   - In play the bill is on THE VETO PATH (play). On the record the measure is in V, under
     premise (B) (item 9), and outside the RECORD BUDGET CLASS.
   - The PP standing AGAINST (NAY) draws from HP or SP. The OP standing FOR (YEA) draws
     from HO or SO.
   - No draw is made for: an OP standing AGAINST; under (A) or in play, a PP standing
     AGAINST in a chamber where the OP also stands AGAINST (UNCONTESTED); a side
     UNDECIDED; a third or created party (it votes its bloc); a party with S = 0; anything
     off the veto path; a budget vote (play) or a RECORD BUDGET CLASS measure (record);
     anything outside CountryId.USA; a president with no PP.
3. WHAT IS DRAWN: an index j into the cell's drawn list L (THE FIT), n = |L|, in FIT ORDER.
   - j = floor(u * n) in IEEE-754 double, clamped to n - 1. The drawn observation is
     (d, m).
   - K = 0 if S = 0; otherwise K = min( floor((S - 1) / 2), floor((2*d*S + m) / (2*m)) ),
     where S is the party's bloc seats in that chamber on that bill (Senate: members plus
     caucus entries). This is the recorded crossing share carried onto the bloc's size,
     rounded half up, and capped so that crossers stay a strict minority.
   - An empty list (only in a LOO list, a bootstrap replicate or an INFLUENCE removal) is
     the single index "K = 0" (n := 1) in Crossers, the enumeration and the realizations
     alike. Its occurrences are counted and printed.
4. The K crossers move to the other side: the PP's from nay to yea, the OP's from yea to
   nay.
5. ONE DRAW per party per chamber per bill serves every vote of that chamber on it
   (cloture, passage, concurrence). PREMISE, stated: the draws of the two parties and of
   the two chambers are independent (R7 prints the joint draw).
6. IN PLAY the drawn crossers enter a chamber's recorded division only where they change
   its outcome (WHEN THE DRAW MATTERS). They enter as each party's crossers on the other
   side, flagged as defectors. Everywhere else the division is the blocs', byte for byte.
7. The cells pool over presidents, Congresses, kinds and outcomes. They are split by
   chamber, role and stance only (four cells), and no split is chosen from the data.
8. A projection reads exact probabilities and never draws. Only the gate draws.
9. THE BACKTEST'S PREMISE (B), ONE STANCE (play's premise, US-18; spec-risk 4). For a
   measure in V:
   - the PP's one stance is NAY in both chambers;
   - the OP's one stance is its bloc stance on the House roll call if the PP's House
     stance there is NAY, otherwise on the Senate roll call;
   - seats are the SEATS OF A CONGRESS;
   - draws are made in both chambers whether or not a chamber has a roll call (none for a
     RECORD BUDGET CLASS measure).
   Off V a measure is never named and draws nothing. R1, reading (A) chamber by chamber,
   is printed beside it.

==========================================================================================
THE FIT (data, members counted, measures, defector definition, estimator)
==========================================================================================
- DATA: the final-agreement roll calls of counted measures, the only roll calls the
  extract carries with a party split (it names each override roll call with its totals
  alone). The extract holds no cloture vote and no failed measure. The override
  roll calls are saved out of tree with their member votes, but the extract keeps only
  their totals; carrying their party split would be a prep change (re-pinned, by
  amendment). The rule reads the extract alone. The RECORD BUDGET CLASS
  roll calls stay in the fit (H1: "the recorded chamber-by-chamber votes since 2017"; R16
  (b) removes them).
- SELECTION ON PASSAGE, DECLARED PREMISE: the record holds passed measures only, so every
  observation comes from a measure that passed. Sending is estimated with a bias toward
  sending. Per chamber, the instrument prints how many PP-NAY roll calls failed at K = 0
  on their recorded bloc result, as x of y, not fitted. R6 prints the fit without
  selection.
- THE FITTED ROLL CALLS, per chamber: every distinct roll call on which the PP of its OWNER
  has bloc stance NAY. Each gives:
  - a PP observation: d = PP yea, m = PP voting;
  - an OP observation only if the OP's bloc stance is YEA: d = OP nay + OP present, m = OP
    voting. A roll call where the OP also stands NAY gives no OP observation and is
    listed.
  Not-voting members are outside m. Independents are in no observation. An observation
  with m = 0 is excluded and listed. Asserted: every share d/m <= 1/2.
- DEFECTOR: a member voting against his party's bloc stance as recorded on that roll call.
  It is not the president's position: no saved page records it, and reading it from the
  outcome would be circular.
- A CELL'S OWN LIST L_own: its observations; n_own = |L_own|, which is also its number of
  roll calls with an observation. n_own alone decides pooling (THIN CELLS).
- A CELL'S DRAWN LIST L: L_own, or for a pooled cell the union it draws from (THIN CELLS);
  n = |L|. L and n serve the draw index, CP, q_any, q10, the mean share and the label.
  Members are never the unit.
- FIT ORDER: ascending share (compare d1*m2 with d2*m1 in integers), ties by the roll
  call's IDENTITY in ordinal order.
- ESTIMATOR: each cell's empirical distribution of (d, m), the nonparametric
  maximum-likelihood estimate of that party-role's crossing-share MARGINAL. It has no
  parameter, no optimiser and no convergence failure. Reasons: it assumes no shape for a
  distribution heavy at zero and overdispersed; each marginal draws only shares the record
  showed; and each figure is a share of n roll calls. R8 prints the binomial at the pooled
  rate. The pairing of the two parties' draws is item 5's independence premise.
- THIN CELLS (decided once on the full fit, and held fixed in every LOO list, bootstrap
  replicate, INFLUENCE removal and realization):
  - A cell with n_own < 12 is POOLED with the same role-and-stance cell of the other
    chamber (HP with SP, HO with SO). It draws from the union of the two own lists, sorted
    in FIT ORDER.
  - The partner keeps its own list unless its n_own too is below 12, in which case both
    draw from the union.
  - A union of fewer than 12 observations leaves the cell UNFITTED, and S3 fires.
  - R11 pools with the other role of the same chamber instead.
- LEAVE ONE OUT (LOO): measure j's draws in the backtest read every cell minus every
  observation of every roll call final for j (all copies). Play and the catalog use the
  full cells. In-sample is R5.
- THE CELL FIGURES: q_any, the share of observations with d >= 1; q10, the share with
  10*d >= m; the mean share.

==========================================================================================
WHEN THE DRAW MATTERS (the margin test per chamber and procedure)
==========================================================================================
Notation for a chamber: Y0 and N0 are the bloc yeas and nays with every K = 0. Kp is the
PP's draw and Ko the OP's (0 where none is made). D = Kp - Ko, Y = Y0 + D, N = N0 - D.

- HOUSE (a majority of those voting; a tie fails). PREMISE of this block: suspension's two
  thirds is not modelled.
  - (B): passes iff 2*Y > W_H, where W_H is the House sworn count of the SEATS
    (independents inside, counting against). With the OP standing YEA, Y = S_OP - Ko + Kp;
    with the OP standing NAY, Y = Kp.
  - (A): passes iff Y > N over the R and D blocs as the roll call lists them (members),
    independents outside.
  - Play: passes iff Y > N among the seated members, every one voting his bloc's side or
    as a crosser.
- SENATE, MAJORITY procedure (expedited or reconciliation):
  - (B): passes iff 2*Y > W_S, the SEATS' sworn count with independents in their caucus.
  - (A): passes iff Y > N, with the caucus entries in their blocs.
  - THE VICE PRESIDENT breaks a tie with the PP's stance in that chamber and is never a
    crosser. He votes under (B), under R1 and in play. On V under (B) that stance is NAY,
    so a tie fails. A vacant vice presidency (play) fails the tie.
- SENATE, RULE XXII (every other measure; every ordinary game bill): cloture needs
  Y >= floor((3*W + 4) / 5), three fifths of the senators duly chosen and sworn, never a
  fixed 60.
  - W is: (B) the SEATS' sworn; (A) the roll call's members, all labels; play, the sworn
    count on the day.
  - Passage then follows by a majority on the same draw. Cloture implies a majority, and
    the Vice President has no cloture vote.
- A CHAMBER WITH NO ROLL CALL (record): (A) passes undrawn ("unanimous under blocs",
  US-10). (B), for a measure of V, is modelled on the SEATS with the draw.
- UNCONTESTED (the PP not AGAINST, or both blocs AGAINST): no draw, and the test at K = 0.
  This applies off V, under (A) and in play. On V under (B) the PP draws under item 2
  wherever it draws there, the OP's NAY included (never on a RECORD BUDGET CLASS measure
  or with S = 0).
- RECORD BUDGET CLASS on V: the test at K = 0 in each chamber, with no draw.
- THE DRAW MATTERS iff the outcome at the drawn (Kp, Ko) differs from the outcome at
  (0, 0). This is H1's "the chamber's margin within the drawn count", made exact.
- MARGIN: the smallest |D|, with the sign that reverses the K = 0 outcome, among D in
  [-cap_OP, cap_PP], where cap = floor((S - 1) / 2) for a party that draws and 0 otherwise.
  It prints "unreachable" where none exists.
- QUORUM: in the model every bloc seat votes, so floor(W/2) + 1 is always present. On the
  record it is asserted on every roll call of a counted measure: VOTING summed over R, D
  and I >= floor(MEMBERS summed over R, D and I / 2) + 1.
- VACANCIES enter only through the bases (members listed, sworn, seated). A vacant seat is
  in no bloc and no base.
- SAME-DAY ACTIONS: the final agreement is the extract's (veto_record.md reading 4; the
  guards of s802 and s831). same_day is printed only. The two chambers' draws are keyed
  apart.
- US-10 MODE (used only by THE US-10 IDENTITIES): kinds as US-10 reads them (EXPEDITED or
  Rule XXII, with no reconciliation path and no budget class), no Vice President (a tie
  fails), all rows.

==========================================================================================
THE VETO (rule (a) on a bill in play)
==========================================================================================
- A bill both chambers send goes to the president of the day (US-21).
- (a) fires iff S_PP,H >= 1 and 2 * (the PP's House nays in the chamber's division) >
  S_PP,H. "More than half" is strict, as in B1.
  - S_PP,H is the PP's House bloc seats (play, (B)), or its House members as the roll call
    lists them ((A); US-10's member count).
  - The nays are S_PP,H - Kp where the PP stands NAY, and 0 otherwise.
  - A president whose party holds no House seat vetoes nothing.
- IDENTITY, asserted in every run, reading and replicate: under the cap, (a) fires exactly
  where the PP's House stance is NAY and S_PP,H >= 1. The draw decides whether a bill
  REACHES the president, never whether (a) fires. Under (B), NAMED = VETOABLE SENT and
  E[named] = E[vetoable sent], asserted. A bill the PP opposed only in the Senate is signed
  under (A).
- (a) is DETERMINISTIC (H1: "Rule (a) then decides"). R12 prints a drawn veto at (a)'s
  fitted precision.
- (a) reads every act presented to an AI president, budget acts included (US-22: "both acts
  may be vetoed"), on their bloc divisions. Only the draw is withheld from budget votes.
- A player-president chooses (US-21/US-23), and (a) is not read for him. Congress's draw is
  the same whoever is president.

==========================================================================================
THE OVERRIDE
==========================================================================================
- BASE (CRS RS22654): "two-thirds of the Members voting, a quorum being present". A chamber
  overrides iff Y >= 2*N.
- SIDES: US-21's "voted by the same sides" means the passage's RECORDED division in that
  chamber. Crossers count only where they entered under item 6, and no fresh draw is made.
  Reason: the extract carries the override votes' totals only (their party split is out
  of tree; THE FIT, DATA), so no override crossing can be fitted from what the rule reads.
  Per premise:
  - (B): Y = the YEA side of that division over the SEATS; N = W - Y, where W_H counts
    House independents against and W_S includes the caucus.
  - (A): Y and N over the roll call's R and D blocs (members), plus the Senate caucus
    entries; House independents outside.
  - (A), chamber with no roll call: the SEATS, with the PP's bloc AGAINST, the OP's FOR and
    no crossers.
- ORDER: the ORIGIN chamber votes first, and the veto falls iff both chambers override.
  Where the origin fails, the other chamber does not vote. Both votes fall on the passage's
  day (Poland's premise). Cloture on an override is not modelled, and with every senator
  voting, the two-thirds bar is never below three fifths of the sworn (asserted).
- PRINTED ONLY, never deciding:
  - omega_j = P(sent and both chambers override), exact. Each chamber's (pass and override)
    probability is enumerated, and the two are multiplied.
  - P(override | named) and the expected overrides.
  - Every recorded override vote beside the model's, and whether 116|H.R. 6395 is in V.
  - US-21's sentence "under it no Congress of record overrides", measured again with the
    draw.
  R15 prints (a) the passage's full drawn crossers applied to the override and (b) a fresh
  keyed draw ("|override") from the same cells.

==========================================================================================
THE PRINT (H2)
==========================================================================================
- A FITTED FIGURE is any number computed from the cells' observations, under the default
  or any reading: the cell figures, every per-measure probability that reads a cell, every
  expected count, precision, hit rate and P(NONE), and every in-game chance. It prints in
  the label below on EVERY surface: the document, the catalog's comments, any screen and
  any reply.
- NOT FITTED, printed plainly as "x of y": counts of the record (measures, vetoes, roll
  calls, n_own, n, |V|, |V_XXII|, |V_EXP|, the draw-free share of V), the companions, B1's
  yardstick, the identities at K = 0, INFLUENCE's k ("k of <distinct fitted roll calls>")
  and a RECORD BUDGET CLASS measure's per-measure figures ("0/1" or "1/1 at K = 0, no
  draw").
- DRAWN (not fitted; a check): the seeded realizations' statistics, each naming the FITTED
  figure it checks.
- DIAGNOSTIC (not a fitted figure's print; for US-21's assertion only): the full-cell,
  in-sample P(sent_j) and P(named_j) of every measure of V, written as reduced rationals
  in their own section of US_VETO_DRAW.md after item 3, with no label or interval, and on
  no other surface. US-21's diagnostic reads them there.
- n: for a cell figure, the cell's n (its drawn list, full cells); for a derived figure,
  the number of DISTINCT observations (by roll-call identity and role) in the union of the
  full drawn lists the figure reads, so a pooled union counts once.
- m, for a derived figure: 1 for a per-measure figure; |V| for the sums over V, precision
  and the pooled P(NONE); |V_XXII| for E_XXII and P(NONE_XXII); |V_EXP| for P(NONE_EXP);
  V_COUNT for the hit rate. A per-president figure takes the same set within that term.
  Cell figures carry no m.
- A TERM WITH NO COUNTED VETO (Trump's second term on the preregistered extract): its
  record row prints "0 counted vetoes; 2 declared unpredictable"; its sums over V print as
  usual; its hit rate, and every other figure with V_COUNT in its denominator, prints
  "no counted veto (H2)" in place of "UNDEFINED (denominator 0)".
- INTERVALS, all at 95%:
  - CELL PROPORTIONS (q_any, q10): Clopper-Pearson exact, CP. With x of n, lo solves
    P(X >= x | n, p) = 0.025 and hi solves P(X <= x | n, p) = 0.025, each by bisection on
    the exact binomial sum to |delta p| <= 1e-12. lo = 0 at x = 0, and hi = 1 at x = n.
  - EVERY OTHER FITTED FIGURE: the percentile bootstrap, BOOT, B = 2000 (record
    resampling).
    - Replicate b (1..2000) makes M picks with replacement from the counted measures in
      CSV order. Pick t (0-based) is index floor(Unit("R-US17|boot|" + b + "|" + t) * M).
    - Its cells hold the observations of the roll calls OWNED by the picked measures, with
      the picks' multiplicity. Pooling and fitted status are held from the full fit.
    - PER-MEASURE figures: every measure j of the full V is scored from the replicate's
      cells, LOO (every copy of j's roll calls removed), whether or not j was picked.
    - TOTALS: taken over the replicate's picks with multiplicity, using those per-measure
      figures. The replicate's V_COUNT is its picked counted vetoes with multiplicity.
    - Computed in IEEE double, with sums in pick order t = 0..M-1.
    - A replicate whose ratio has a zero denominator is UNDEFINED, and k is the number of
      such replicates. The UNDEFINED values sort first. If k >= 50 the interval prints
      "UNDEFINED IN <k> OF 2000". Otherwise lo = sorted[49] and hi = sorted[1950]
      (0-based), and for 1 <= k < 50 " (<k> of 2000 replicates undefined)" is appended.
- THE LABEL, exactly:
    "<value> FITTED n=<n> 95% [<lo>, <hi>] CP"            for a cell proportion;
    "<value> FITTED n=<n> 95% [<lo>, <hi>] BOOT"          for the mean share;
    "<value> FITTED n=<n> m=<m> 95% [<lo>, <hi>] BOOT"    for every derived figure.
  For each pooled cell the figure reads, " (<XX> pooled with <YY>)" follows n=<n>.
- NUMBER FORMAT: shares, probabilities and rates print as percent to one decimal ("41.3 %");
  expected counts print to two decimals. An exact rational rounds half up on its exact
  value; a double rounds half up on its exact decimal value. Invariant culture, "." as the
  decimal mark.
- TOO THIN (H4's form):
  - an UNFITTED cell prints "UNFITTED n_own=<n_own>, pooled with <YY> n=<n> - fewer than
    12 observations", n its union's size (THE FIT), with no figure and no interval;
  - every figure that reads it prints "UNFITTED", with no value;
  - a derived figure with m = 0 prints "UNFITTED m=0";
  - a ratio whose exact denominator is 0 prints "UNDEFINED (denominator 0)", with no
    interval.
- DRAWN FORM: "<mean> DRAWN 1000 worlds [<26th>, <975th>] - seed noise of a single game,
  not fit uncertainty; checks <figure> FITTED n=<n> above".

==========================================================================================
SEEDS AND DETERMINISM
==========================================================================================
- UNIT(key): FNV-1a 64 over the key's UTF-8 bytes (offset 14695981039346656037, prime
  1099511628211), finished with MurmurHash3's fmix64. The result is (h >> 11) / 2^53. This
  is PresidentialVeto.Draw's arithmetic, implemented in UsVeto. PresidentialVeto.cs is not
  edited. Asserted: UsVeto's Unit on Poland's key format equals PresidentialVeto.Draw on 8
  fixed (masterSeed, date, title) inputs outside any plant scope.
- No SimulationRandom stream is created, drawn or moved, and its enum is unchanged. Reasons
  (Poland's precedent): a stream draw shifts every later draw; a projection must consume
  nothing; a reload, the shadow and a fork that reach the vote on the same day must draw
  alike.
- THE PLAY KEY: masterSeed (invariant) + "|USA|" + yyyy-MM-dd + "|" + title + "|" +
  ("House" | "Senate") + "|" + ("REP" | "DEM") + "|defectors".
  - The date is the date of THAT CHAMBER'S FIRST DIVISION on the bill. It is carried on the
    bill for every later vote of that chamber on it, and US-21's diagnostic asserts one key
    per chamber per bill.
  - Two divisions with one title in one chamber on one day are one act. A bill retitled,
    or first divided on another day, draws apart (Poland's premise, stated).
  - UsVeto has its own plant scope and never reads PresidentialVeto's _planted.
- THE BACKTEST'S DECISION FIGURES are EXACT, and no seed decides anything.
  - Per chamber: P(pass) = (number of index pairs (j_PP, j_OP) over the LOO lists that
    pass) / (n_PP * n_OP), as a reduced rational. A party with no draw, or an empty list,
    is the single index "K = 0" (n := 1).
  - P(sent_j) = P_H * P_S. Histograms of K may implement this but must equal the
    enumeration.
- THE SEEDED REALIZATIONS (DRAWN, printed only):
  - master seeds 1..1000 run the play key over every measure of V, on the LOO lists;
  - the title is "<congress>th Congress <measure>", and party R -> REP, D -> DEM;
  - the date is the chamber's final date, or decision_date where it has none, and it
    serves every vote of that chamber.
  - Asserted (a failure is a DEFECT): for E[vetoable sent], E_XXII[vetoable sent] and
    E[hits], |mean - exact| <= 4 * sd / sqrt(1000). sd is the sample sd (1/(N-1)) in
    double, summed in seed order; equality is required where sd = 0.
- The bootstrap keys ("R-US17|boot|", and R17's "R-US17|cboot|") carry no master seed and
  are disjoint from every play key, which begins with an integer.
- A second run in one session gives the same decisive digest (asserted).

==========================================================================================
THE FIRST RUN (what the backtest on the record measures and prints)
==========================================================================================
THE INSTRUMENT: a new Editor check, Assets/Editor/UsVetoDrawCheck.cs, in the cheap and
documents groups. It calls the pure class UsVeto (namespace PoliSim.Elections; Applies,
Unit, Key, Crossers, ChamberOutcome, PassProbability, NamedByA, OverrideCarries) on
chambers built from the CSV, so the backtest runs the game's arithmetic.
- UsVeto.cs lands in Assets/Editor beside the instrument, and the catalog in
  Assets/Editor/Generated, until US-21's gate first calls them; US-21 then moves both into
  Assets/Scripts/Elections (and its Generated folder) byte for byte, asserted by blob id.
  So no Assets/Scripts file goes unreached before US-21, and UnwiredSubsystemCheck's
  UNREACHABLE count stays where it is (asserted in the first-run commit's bars).
- It writes docs/generated/US_VETO_DRAW.md, stamped with SourceDigest, the pins, the
  block's digest, the decisive digest and the ruling-record state.
- It writes the catalog (HOW THE FIGURES REACH THE GAME).
- us_veto_backtest.pl, US_VETO_BACKTEST.md and UsVetoBacktestCheck stay byte-unchanged, as
  the record of s802.
- The first run is on the block as preregistered, or as amended before it by Elias's
  rulings (his answer to QUESTIONS FOR ELIAS, a re-pin), the RuleDigests list's last entry
  (H1, H3). A choice
  changed after a result
  is in sight takes a new digest and is reported to Elias (G1).

Per measure j of V, under (B), exact and LOO: P_H(j), P_S(j), P(sent_j), P(named_j) and
omega_j; VETO_j = 1 iff j is a counted veto. Beside them, never deciding: the DIAGNOSTIC
full-cell figures (THE PRINT).

Totals, pooled and per president (A TERM WITH NO COUNTED VETO: THE PRINT).
- E[vetoable sent] = sum over V of P(sent_j); E_XXII[vetoable sent] = the same over V_XXII
- E[named] = sum over V of P(named_j) (asserted equal to E[vetoable sent])
- E[hits] = sum over V of VETO_j * P(named_j)
- E[false positives] = E[named] - E[hits]; E[misses] = V_COUNT - E[hits]
- PRECISION = E[hits] / E[named]; HIT RATE = E[hits] / V_COUNT
- P(NONE_XXII) = product over V_XXII of (1 - P(sent_j)); P(NONE) = the same over V;
  P(NONE_EXP) = the same over V_EXP, the measures of V whose kind is EXPEDITED.
Each prints FITTED with its BOOT interval and, where it is a count, its DRAWN spread. B1's
yardstick prints beside the precision.

PRINTED, in this order:
1. THE RECORD per president: measures and vetoes, counted and in all. The UNPREDICTABLE
   line. |V| beside F3's 34, with the differences. |V_XXII|. The RECORD BUDGET CLASS
   matches and NEAR MISSES.
2. THE CELLS: n_own and n; every observation of the drawn list (identity, owner, d, m) in
   fit order, each marked with its own cell; pooled or UNFITTED; q_any and q10 (CP); the
   mean share (BOOT). The selection count per chamber (x of y).
3. PER MEASURE OF V, in CSV order: kind, Senate procedure, budget class, president,
   recorded outcome; each chamber's procedure, bloc margin at K = 0, MARGIN and roll-call
   presence; the cells read; P_H, P_S, P(sent), P(named) and omega (FITTED, or for a
   RECORD BUDGET CLASS measure "0/1" or "1/1 at K = 0, no draw", THE PRINT); and P(the
   draw matters) in each chamber. Then the DIAGNOSTIC section (THE PRINT).
4. THE TOTALS above, with the SENDING CALIBRATION beside them: E[vetoable sent] beside
   "|V| - all sent on the record" and E_XXII beside "|V_XXII| - all sent on the record",
   both not fitted. E[named] prints beside V_COUNT. The precision prints beside the
   draw-free share of V (counted vetoes in V, as x of |V|, not fitted).
5. Every counted veto with P(named), FITTED (or "not in V", or THE PRINT's form for a
   RECORD BUDGET CLASS measure); every measure named with P > 0
   and not vetoed.
6. THE OVERRIDE's prints.
7. THE SEEDED REALIZATIONS (DRAWN).
8. COMPANIONS, not fitted, on the counted base only (V_COUNT as the denominator), by
   us_veto_backtest.pl's own definitions: (a) named and vetoed; both chambers; one stance;
   chamber by chamber, vetoable and sendable vetoes. A pointer gives
   docs/generated/US_VETO_BACKTEST.md for US-10's figures over all rows.
9. Every shared roll call; every caucus entry ignored as neither R nor D; every unknown
   kind; every roll call with no OP observation; every observation with m = 0; every
   empty-list occurrence.
10. THE IDENTITIES, asserted (a failure is a DEFECT):
    - SEATS equal US_VETO_BACKTEST.md's seats table;
    - US-10 IDENTITIES, in US-10 MODE, printed as "US-10's record (s802), not this rule's
      figure":
      (i) per measure of F3, at K = 0 with the OP forced YEA on full seats, the (B) send
          verdict equals US-10's one-stance verdict, and the count equals the document's;
      (ii) (A) at K = 0 over all rows reproduces the document's chamber-by-chamber counts
           (measures sent, vetoable, vetoes sendable);
      (iii) (a) on the recorded House votes reproduces the document's (a) pooled row;
      the measures of V minus F3, and those where the OP's recorded stance is NAY, are
      printed with their K = 0 verdicts;
    - every share <= 1/2;
    - (a) fires exactly where the PP's House stance is NAY, and NAMED = VETOABLE SENT
      under (B);
    - the UNPREDICTABLE set; B1 = 45 of 166; Unit = PresidentialVeto.Draw on the 8
      inputs; the seeded means agree with the exact figures; the pins.
11. INFLUENCE (printed only): for each distinct fitted roll call, its observations are
    removed from every cell, LOO lists included, with pooling and fitted status held. The
    outcome is recomputed exactly. Every roll call whose removal changes the outcome is
    listed with the new outcome, and k = their number.
12. DECLARED READINGS, each with its outcome and headline figures (E_XXII[vetoable sent],
    P(NONE_XXII), E[vetoable sent], E[hits], precision, hit rate), labelled.
13. THE DECISION, the verdict line and the decisive digest.

==========================================================================================
THE DECISION (the outcome of a run defined once)
==========================================================================================
H1 has ruled (c). A run installs (c) or re-asks; it never chooses among options. The
outcome reads only the default's exact LOO figures under (B).

THE STOPS, evaluated in this order:
- S3, TOO THIN (the panel's addition, not one of R-US17's two STOPs): any cell is UNFITTED
  after pooling. A decision cannot be computed on an unfitted cell (H4), so the rule
  re-asks. When S3 fires, S1 and S2 are not evaluated, and their figures print UNFITTED.
- S2, NEVER SENDS (R-US17's second STOP: "a party-bloc Congress under Rule XXII never
  sends a vetoable bill"; the record's measures of V_XXII stand for the vetoable bills;
  "never" read as Elias answered on 2026-10-16, in the typical world). It fires iff:
  - case "no Rule XXII measure on the veto path": V_XXII is empty (its figures print
    "UNFITTED m=0"); or
  - case "never sends": 2 * P(NONE_XXII) >= 1, that is, the chance that no measure of
    V_XXII is sent is at least one half.
- S1, FAR MORE (R-US17's first STOP as US-10 read it before its first run, s802,
  PROVISIONAL under G5; also R-US17's test "(a) only if its precision ... is at least
  B1's"), pooled over every kind. It is evaluated only where E[named] > 0, and it fires
  iff 166 * E[hits] < 45 * E[named]. Where E[named] = 0, it prints "not evaluated
  (denominator 0)".

THE OUTCOME is exactly one of: INSTALL (no stop fires); S3; S1; S2; S1 + S2. Its figures
are not part of it.

PRINTED ONLY, never part of the outcome:
- every interval, and where 27.1 % lies against the precision's BOOT interval (below,
  inside or above);
- the pooled P(NONE) and P(NONE_EXP), the hit rate, the per-president figures, the
  override, the realizations, the companions and INFLUENCE;
- every declared reading and its outcome. A reading whose outcome differs is named in the
  verdict line, and no reading ever stops the rule.

THE VERDICT LINE, generated and never typed:
- INSTALL: "By R-US17's rule (c) as ruled (H1-H3), the draw installs: under Rule XXII a
  bloc Congress with the draw sends <E_XXII[vetoable sent]> vetoable measures of the record
  FITTED n=.. m=.. 95% [..] BOOT (all <|V_XXII|> were sent on the record), P(none under
  Rule XXII) <q> FITTED ...; over every kind it sends and names <E[named]> FITTED ...,
  <E[hits]> of them vetoed FITTED ...; precision <p> FITTED ... against B1's 27.1 % (45 of
  166), the draw-free share of V <x> of <|V|>; hit rate <h> of the <V_COUNT> counted
  vetoes FITTED ...; every cell fitted; H.R. 131 and H.R. 504 (119th) declared
  unpredictable, in no count (H2); <FRAGILE: <k> roll calls each flip the outcome | no
  single roll call flips the outcome>; <the same under every declared reading | the
  outcome differs under <readings>: <outcomes>> - held INSTALLED-PENDING-E4: the rates go
  to Elias before US-21 wires them (E4)."
- STOPPED: "R-US17's rule (c) does not install: <each stop with its figures and, for S2,
  its case>; <FRAGILE clause>; <readings clause> - asked."
Whatever the outcome, the full document goes to Elias (E4).

THE STOPS' OPTIONS, each with "amend the rule as ruled" beside it:
- S3: pool all four cells into one; pool per chamber across roles (R11); or source more
  roll calls (the Senate's cloture and failed votes, BILLED).
- S2: install (c) as measured; R1, reading (A), as play's premise (this amends US-18's one
  stance, re-pinned); source the Senate's cloture and failed votes (BILLED) and run again;
  or keep the blocs with no draw (an AI president never vetoes; US-21 builds the plate for
  a player-president).
- S1: install (c) as measured; (c) with R12's drawn veto; or keep the blocs, as in S2's
  last option.

==========================================================================================
WHAT STAYS AS IT IS AND ITS PROOF
==========================================================================================
- US-10: Tools/us_veto_prep.pl, us_veto_measures.csv (held to the pins),
  us_veto_backtest.pl and US_VETO_BACKTEST.md are byte-identical, and UsVetoBacktestCheck
  is unchanged and green. Tools/us_veto_mutations.sh stays 13 of 13 (s831) and is re-run
  whenever the prep tool changes, followed by this instrument.
- BUDGET VOTES draw nothing (H1):
  - UsVeto refuses every budget vote, and its draw-call counter reads 0 on that path.
  - On the record, no RECORD BUDGET CLASS measure draws (asserted).
  - Planted cases: a contested budget act at margin 1 draws zero.
  - US-21's diagnostic plants j = n - 1 (the largest share): every US budget division is
    identical to the unplanted run, and the counter reads 0. This is installed at US-21
    and re-run at US-22.
- THE FIVE OTHER COUNTRIES (Sweden, Germany, France, Italy, Poland):
  - UsVeto.Applies is true for CountryId.USA alone, asserted over all six values.
  - The draw counter reads 0 on every non-US path the diagnostic runs, and SimulationRandom
    is unchanged.
  - The simulation bar's trajectory sentinel holds byte-identical against its pins on both
    seeds (777 and 424242), and the dump diff is inert. Nothing calls UsVeto in play before
    US-21.
- POLAND'S B1/F3 (H1: "proved unchanged on the simulation bar"):
  - PresidentialVeto.cs, Generated/PolishVetoRates.cs and docs/generated/VETO_B1_BACKTEST.md
    are byte-identical (the review row shows an empty git diff over the three).
  - PresidentialVetoGate's body is untouched.
  - PresidentialVetoDiagnostic with its D4 and E2 cases, and GovernmentBudgetBillDiagnostic's
    Poland pins, pass unmoved.
  - The sentinel holds.
  - All of this is required in the first-run commit and again at US-21's.
- R-US18's and R-US19's block digests are unchanged (5c89b0cf..., 1caec656...).

==========================================================================================
TRUMP'S 2025 VETOES
==========================================================================================
H.R. 131 and H.R. 504 (119th) passed both chambers without a roll call; both were vetoed
and sustained in the House on 8 January 2026. Under H2 they are UNPREDICTABLE and never
counted for or against. They are:
- not in the fit (no roll call; asserted to own no observation);
- not in V or V_XXII, and not in any expected count, precision, hit rate, P(NONE), V_COUNT,
  M or the bootstrap population;
- not in the realizations or INFLUENCE;
- not in the override's counts: their sustained votes print on their own line only;
- not in the per-president rows (Trump's second term: "0 counted vetoes; 2 declared
  unpredictable");
- not in the companions (counted base only).
They are read by THE US-10 IDENTITIES only as US-10 read them, printed as US-10's record,
and they enter the decisive digest by name only.
One line prints them: "UNPREDICTABLE (H2): H.R. 131, H.R. 504 (119th) - passed both
chambers without a roll call; never counted for or against."

==========================================================================================
HOW THE FIGURES REACH THE GAME (US-21)
==========================================================================================
- THE CATALOG: UsVetoDefections.cs (namespace PoliSim.Elections.Generated, "GENERATED - DO
  NOT EDIT"), written by UsVetoDrawCheck into Assets/Editor/Generated and moved into
  Assets/Scripts/Elections/Generated at US-21 (THE FIRST RUN, THE INSTRUMENT).
  - It holds SourceDigest, the pins, RuleDigest, DecisiveDigest, Outcome,
    MinimumObservations = 12 and M.
  - For each of HP, HO, SP and SO it holds Fitted, PooledWith and the EXACT LIST THE CELL
    DRAWS FROM: already merged for a pooled cell, in fit order, each entry (Identity,
    OwnerIndex in counted CSV order, Defectors, Voting), with Identity and OwnerIndex as
    fields.
  - The runtime indexes these arrays and never sorts or merges them. LOO stays
    instrument-side.
  - The cheap bar regenerates the catalog in memory and fails on any byte of difference.
    The byte-for-byte form is new here: Generated.PolishVetoRates is the precedent for the
    catalog's shape only (a static class with its source digest and record arrays), held
    by its rows and source digests in PresidentialVetoDiagnostic.
- THE RUNTIME: UsVeto.cs, pure, lands with the instrument (in Assets/Editor until US-21's
  move) and stays unwired until US-21.
  The gate alone draws, and a projection reads exact probabilities and never draws.
- H2 IN PLAY: any surface that shows a fitted chance shows it in THE PRINT's label, with
  its BOOT interval. The interval is computed from the catalog by THE PRINT's replicate
  rule (the keyed picks over M, owners' multiplicities), so it is the same interval the
  instrument would compute. Whether a surface shows a fitted chance at all is US-21's
  design.
- US-21'S VETO DIAGNOSTIC asserts:
  - the ruling record INSTALLED (with its E4 section) or RULED with a buildable option, at
    the current block and catalog digests;
  - the projection's draw counter at 0, and the budget and five-country counters at 0;
  - one play key per chamber per bill;
  - for every counted veto in V, enumerating the runtime's planted indices reproduces the
    instrument's exact P(sent) and P(named) as the same rationals (on the full cells).

==========================================================================================
DECLARED READINGS (each runs the whole rule with one change; outcome and headline figures
printed; never a stop)
==========================================================================================
- R1: (A), chamber by chamber as recorded (s802's declared reading, with the draw and the
  Vice President), in place of (B).
- R2: the fit over every contested roll call (R and D bloc stances opposite). Each party's
  observation is taken against its own stance, with cells by chamber and role, pooled over
  stances. These are the cells a draw on every contested ordinary bill would use, printed
  split by stance.
- R3: cells split by Senate procedure, MAJORITY apart from RULE XXII (eight cells, each
  under the thin-cell rule).
- R4: cells by party label (R/D) in place of role.
- R5: in-sample, no LOO.
- R6: selection removed. The PP cells keep only roll calls whose chamber's recorded bloc
  result at K = 0 already passed. Every cell left empty is printed with "no
  selection-free observation on disk" and falls under the thin-cell rule.
- R7: a joint draw: one index per chamber over the PP cell's roll calls gives both parties'
  shares (Ko = 0 where that roll call has no OP observation).
- R8: the binomial, K ~ Binomial(S, sum d / sum m), capped as in the model (doubles, exact
  pmf).
- R9: THE FITTED ROLL CALLS and their observations read with the PP of THE PRESIDENT ON A
  DATE of each roll call in place of its owner's decision president (the selection and
  the PP and OP observations alike); V, (B) and (a) keep the decision president.
- R10: present outside the stance and the observation (stance YEA iff yea > nay; d over
  yea + nay); any share above 1/2 is capped by the model.
- R11: a thin cell pooled with the other role of its chamber.
- R12: the veto drawn at (a)'s FITTED precision (F3's form): expected vetoes = E[named] *
  precision.
- R13: S2 at E_XXII[vetoable sent] = 0 exactly (the words' own reading, the preregistered
  default); S2 at E_XXII[vetoable sent] < 1; S2 over every kind (V in place of V_XXII);
  S1 on the BOOT upper bound (strict) and on the lower bound (lax).
- R14: N_MIN = 20.
- R15: the override with (a) the passage's full drawn crossers, whether or not they
  entered the passage, and (b) a fresh keyed draw ("|override") from the same cells.
- R16: the RECORD BUDGET CLASS (a) scored as its kind with the draw (reconciliation as
  RULE XXII), and (b) with its roll calls removed from the fit.
- R17: the cell bootstrap: the scoring set fixed (the real V, VETO_j), with the owners of
  observations resampled. With O owners in CSV order, pick t of replicate b is
  floor(Unit("R-US17|cboot|" + b + "|" + t) * O). It changes intervals only.
- R18: debt-limit titles outside the RECORD BUDGET CLASS (scored as their kind with the
  draw).

SETTLED, NOT READINGS: present stands against in the default (US-10, F3; R10 prints the
alternative, with V and F3's comparison recomputed under it, and never stops the rule); the
independents (US-10); the
Vice President (equal to US-10 on V); the override base (sourced); the UNPREDICTABLE set
(H2); the narrow scope (H1).

PREMISES, stated and not read:
- the record holds passed measures only (selection toward sending);
- the parties' and chambers' draws are independent;
- the House majority without suspension;
- the record's budget class read from titles (the extract has no budget column), with
  unmatched reconciliation acts scored as their kind;
- the ten days and the pocket veto are not modelled;
- retitling a bill, or first dividing it on another day, redraws (Poland's premise).

==========================================================================================
QUESTIONS FOR ELIAS
==========================================================================================
None open. The one asked at s832, S2's reading of "never", Elias answered on 2026-10-16,
before any run: the typical world, P(NONE_XXII) >= 1/2 (THE DECISION, S2), written in by
amendment at s833; R13 prints the words' own reading (E_XXII[vetoable sent] = 0 exactly,
the preregistered default) and E_XXII[vetoable sent] < 1 beside it. Every other choice is
a default with its reason and, where it is contestable, a reading printed beside it and
named in the verdict line where its outcome differs.

==========================================================================================
THE RULING RECORD AND THE BARS
==========================================================================================
- THE BLOCK sits between the plan's two R-US17 RULE marker lines (BEGIN and END),
  after H1-H3, in plain ASCII inside a ```text fence, with the pins filled. Its digest is
  SHA-256 over the UTF-8 bytes (no BOM) of the lines strictly between the marker lines,
  fence lines included, each ending LF with no CR. It is read from the index blob's raw
  bytes and required equal to the working copy's.
- THE DIGEST LIST: the preregistered digest is recorded in COMPLETED.md s832 and in the
  plan's "The block's digests" line (R-US18's form) at the preregistering commit.
  - UsVetoDrawCheck's RuleDigests is created in the first-run commit, with that digest as
    its first entry ("preregistered (s832)"), then one entry per amendment naming Elias's
    ruling ("AMENDED (Hn, sNNN): old -> new").
  - The block must equal the list's last entry. A change without a ruling fails.
  - The first-run commit's review row shows the preregistering commit's plan blob hashed
    to the first entry, and that commit preceding the one that adds UsVetoDrawCheck.cs.
- THE DECISIVE DIGEST: SHA-256 over these lines, UTF-8 without BOM, each "key=value"
  ending LF, in this order:
    source=<SourceDigest>
    cell.<XX>.status=FITTED|UNFITTED        (XX in HP, HO, SP, SO, in that order)
    cell.<XX>.pooled=<YY>|-
    cell.<XX>.n_own=<n_own>
    cell.<XX>.n=<n>
    cell.<XX>.obs.<i>=<identity>|<congress>|<measure>|<d>|<m>   (i 0-based, drawn list,
                                                                 fit order)
    unpredictable.<i>=<congress>|<measure>  (CSV order)
    counted=<M>
    v_count=<V_COUNT>
    v=<|V|>
    v_xxii=<|V_XXII|>
    p_sent.<congress>|<measure>=<p/q>       (every measure of V, CSV order; then p_named)
    p_named.<congress>|<measure>=<p/q>
    e_sent=<p/q>  e_sent_xxii=<p/q>  e_named=<p/q>  e_hits=<p/q>  p_none_xxii=<p/q>
                                                    (one line each, in this order)
    outcome=INSTALL|S3|S1|S2|S1+S2
  Rationals are "p/q" in lowest terms, q > 0, zero written "0/1", with no sign. Every key
  is written in every run: a figure that reads an UNFITTED cell is written key=UNFITTED;
  over an empty V_XXII, e_sent_xxii=0/1 and p_none_xxii=1/1 (the empty sum and product);
  an UNFITTED cell writes cell.<XX>.n as its union's size and its obs lines over that
  union (fit order), with pooled naming the partner it pooled with. Nothing else enters:
  no interval, reading, (A), override, realization, INFLUENCE or companion.
- THE RULING RECORD, held by the instrument:
  - NONE: the state before the instrument exists. The instrument fails while it reads
    NONE, so the first-run commit registers the outcome.
  - INSTALLED-PENDING-E4 (sNNN): a first run that gives INSTALL, with its decisive digest.
  - INSTALLED (sNNN; E4 sMMM): once a COMPLETED.md section sMMM records the reply to Elias
    that carried the rates.
  - ASKED (sNNN), with the outcome and the digest.
  - RULED (Elias's ruling, sNNN), with its option, the outcome it was made on and the
    digest.
  - Each state passes while the generated outcome and the decisive digest equal the
    registered ones.
  - A CHANGED OUTCOME, in either direction and from any state, fails until it is
    registered ASKED with the old and new outcomes and digests. Only Elias's ruling moves
    ASKED to RULED.
  - A MOVED DIGEST WITH THE SAME OUTCOME fails until a commit re-pins it, naming the old
    digest, the new one, its cause (source, code or amendment) and "outcome unchanged".
    The next reply to Elias lists the per-measure P(sent) differences (E4, G1). A changed
    SourceDigest after the first run is always reported this way.
  - A ruling must name a buildable option: (c) as measured, a stop's option, or (c)
    amended by name. Otherwise the record stays ASKED.
  - THE MIRROR: the line after the block mirrors the state, section and ruling, never a
    digest: "**Ruling record:** INSTALLED-PENDING-E4 (sNNN): (c)." /
    "**Ruling record:** INSTALLED (sNNN; E4 sMMM): (c)." /
    "**Ruling record:** ASKED (sNNN): S1 + S2." /
    "**Ruling record:** RULED (sNNN): <option>."
    The instrument fails while the mirror and the record differ.
- THE MUTATION SUITE: Tools/us_veto_draw_mutations.sh. Each case must fail the check with
  its own message, and the control reproduces the document and the catalog byte for byte.
  The cases:
  - a third vetoed row with no roll call;
  - H.R. 131 given a roll call;
  - a share above 1/2;
  - a CSV byte changed (the source pin);
  - an edited catalog list;
  - the block edited without a listed digest;
  - the mirror line changed;
  - a RECORD BUDGET CLASS measure made to draw;
  - planted runs proving the UNFITTED, S3, S2 (both cases) and S1 paths (s830's form).
- THE BARS:
  - THE PREREGISTERING COMMIT (documents only, no instrument): the block with its pins;
    the digest in COMPLETED.md s832 and in the plan's digests line; the documents batch,
    with MojibakeCheck, DocumentClaimCheck, ResidueCheck and PreWiringPremiseCheck by name.
  - THE FIRST-RUN COMMIT: UsVeto.cs, UsVetoDrawCheck.cs (RuleDigests first entry = the
    preregistered digest), the catalog, the document and the mutation suite; the cheap bar
    with UsVetoDrawCheck added; UsVetoBacktestCheck unchanged and green;
    UnwiredSubsystemCheck's UNREACHABLE count unchanged (UsVeto.cs and the catalog in
    Assets/Editor until US-21); the documents
    bar; the simulation bar (Poland's proof and the five countries inert, H1); R-US18's
    and R-US19's digests unchanged; the review row's preregistration proof; the ruling
    record registered (INSTALLED-PENDING-E4 or ASKED).
  - US-21: everything above, plus its veto diagnostic as stated in HOW THE FIGURES REACH
    THE GAME, and its own done-when.
```
<!-- R-US17 RULE END -->

**Ruling record:** INSTALLED (§834; E4 §835): (c).

**The block's digests** (the instrument's list): preregistered `577456a92d1ff7380bd44374b0c6fd02738452f09dfd53396f5aa988887e877a` (§832); AMENDED (Elias's answer on S2's reading, 2026-10-16, §833): `577456a92d1ff7380bd44374b0c6fd02738452f09dfd53396f5aa988887e877a` -> `24de40a77bbaddfaccb98df91a648206fcd5c47bf1b5b6a658caba7270058c9c`.

**Asked blind, before the first run (§832).** One choice, which H1-H3 leave open and which the review named (`Reviews/2026-10-16_s832_rus17c_rule_review.md`, fidelity-F1 and inference-S2-knife-edge, refuted as a declared default rather than a defect): **S2's reading of "never"** - R-US17's second STOP, "a party-bloc Congress under Rule XXII never sends a vetoable bill". Under the draw a measure is sent with a probability, so "never" has to be read. By default it is read as the words have it: the expected number of the record's vetoable measures sent under Rule XXII exactly 0 (equivalently, the chance that none is sent exactly 1). A single measure of the record that the draw can carry through both chambers with any probability at all - past cloture in the Senate and, where the president's party holds the House, through the House too, on crossings recorded on other measures' roll calls - then keeps S2 silent. The alternatives: the chance that none is sent at least one half (the design panel's judge chose this; its reviser restored the words' own reading), or an expected count below one. Reading R13 prints both on every run. **The first run does not wait on the answer** - a default of this block (QUESTIONS FOR ELIAS), not a ruling of Elias's: R-US18's and R-US19's choices waited for his word (G1; §791, §794), while H3 asks only that the digest precede the first run. An answer that comes before it is written into the block by amendment with a new digest; one that comes after it is a choice changed after a result is seen (G1's principle, by analogy), and reported.

**Answered (Elias, 2026-10-16, before any run; installed §833):** the typical world - S2 fires when the chance that no vetoable measure of the record is sent under Rule XXII is at least one half (2 x P(NONE_XXII) >= 1, on exact rationals). Written into the block by amendment, the digest moved as listed above. Reading R13 now prints the words' own reading (the expected count exactly 0, the preregistered default) and the expected count below one beside it. The first run is on the amended block.

**R-US18 — The House's seat method** *(US-11's errors; blocks US-16)*.
- (a) 435 districts by uniform swing on the latest House election's district results, uncontested seats by a DECLARED rule, and mid-decade redraws entering as dated variants once each enacted map is read;
- (b) state delegations by a state swing;
- (c) a national seats-votes rule fitted on the record (one parameter).

The measurement decides; Code expects (a) where maps held.

**The rule, declared before the instrument's first run (§791).** A design panel specified it blind to rough scratch estimates of the three methods, which the session had made first while mapping the item: the panel was told not to open them and not to count any method's seats, and its brief held the plan's question, the rulings it answers, the game's constraints and facts of the record - no estimate. The session, which had seen the estimates, condensed the specification into the block below and US-11's *Design*, and corrected them where its review found the condensation or the specification wrong. `COMPLETED.md` §791 says what the brief held (the statements in it that no saved page bears out among it), what the panel saw and did, each correction, and holds the preregistered digest. The rule is the fenced block between the two markers below; everything outside the markers - the ruling record's mirror, the asks, the ruling lines - is not part of it. The arithmetic the rule runs on, the scoring and the decision are the block's; US-11's *Design* holds the methods' inputs, assertions, outputs and use in play, and a ruled option's build (the reconciliation of (c)). The instrument holds the list of the block's SHA-256 digests - over the UTF-8 bytes, no BOM, of the lines strictly between the two marker lines (the opening and closing fence lines included), each ending LF, no CR: the preregistered one, then one per amendment, each naming Elias's ruling ("AMENDED (Fn): old -> new"). The block must match the list's last digest, and a change without a ruling fails. After an amendment the instrument runs again and the ruling record is checked against each run's new outcome.

<!-- R-US18 RULE BEGIN -->
```text
R-US18'S RULE - US-11's measurement decides the House's seat method. Preregistered in
COMPLETED.md section 791; amended at section 797 (Elias's ruling G1).

THE TERMS
- The record's House elections are 2016, 2018, 2020, 2022 and 2024, and 2026 once C5 is
  measured; its first transition is 2016->2018.
- The cycles B->T: C1 2016->2018, C2 2018->2020, C3 2020->2022, C4 2022->2024, and C5
  2024->2026 once measured. Each keeps its role in every reading and run: C1, C2 and C5
  are swing cycles, C4 is the level cycle, and C3 is printed, never deciding.
  D = {C1, C2, C4}.
- V(E) is REP's share of REP+DEM in US-5's own-line House series, as house_by_state.csv
  holds it, never recomputed. Delta = V(T) - V(B). (a)'s and (b)'s shares are swung plain
  additive and never re-solved.
- A district's deciding count is its final_r and final_d (house_districts.csv).
- n_B and n_T are a state's seats at B and at T (house_maps.csv). Every row gives each state
  its n_T seats: DEMhat_s = n_T - REPhat_s.
- A state is held for B->T if its lines in force changed at no election after B up to T
  (house_maps.csv's in_force: lines_changed, except where a saved page says a new plan was
  not used) and its seats did not change; otherwise it is redrawn. A district number is
  never matched across a redraw.
- The half rule: a half goes to the party that held more of the state's base seats, then to
  the national base majority.
- (a) is district uniform swing with F-old:
  - A base district's share d: none if it has no winner and no votes (N); 1 or 0 by its
    winner's party if unopposed and printed without votes (U); where exactly one of final_r
    and final_d is zero, 1 if final_d = 0 and 0 if final_r = 0; otherwise
    final_r / (final_r + final_d). dhat = d + Delta.
  - A held state: each district is REP if dhat > 0.5, DEM if dhat < 0.5, and its base winner's
    party at exactly 0.5. A held state with a base district that has no share is counted as
    a redrawn state is, by F-old over the shares it has.
  - A redrawn state, F-old: the state's base shares as an unlabelled set, swung and sorted
    x(1) <= ... <= x(M); n = n_T. If M = n, REP = #{x(j) > 0.5} plus
    #{x(j) = 0.5 whose base winner is REP}. If M != n, q(k) = Q((k - 0.5)/n) for k = 1..n, Q
    linear through the points ((j - 0.5)/M, x(j)) and flat beyond the end points;
    REP = #{q(k) > 0.5}, a q(k) of exactly 0.5 going by the half rule.
  - F-prop, the alternative fallback: a redrawn state counted as (b0) counts it.
- (b) is anchored, per state:
  - u = sum final_r / sum (final_r + final_d) over the state's base districts with votes;
    U_R and U_D are the base's U seats by party; m_B the base's decided seats; R_B REP's
    decided base seats, U seats included; c_B = m_B - U_R - U_D; c_T = n_T - U_R - U_D;
    uhat = u + Delta. A state with c_B = 0 takes u = R_B / m_B.
  - P(c, x) = the nearest integer to c * min(1, max(0, x)), a half going by the half rule.
  - n_T = 1: REP if uhat > 0.5, DEM if uhat < 0.5, the base winner's party at exactly 0.5.
  - n_T >= 2: REP = min(n_T - U_D, max(U_R, R_B + P(c_T, uhat) - P(c_B, u))).
- (b0), unanchored: REP = U_R + P(c_T, uhat) where n_T >= 2, as (b) at one seat.
  (b U-blind): (b) with c_B = m_B and c_T = n_T.
- (b'), the strawman: (b) with each P(c, x) replaced by the nearest integer to c * p(x), a
  half going by the half rule, where p(x) = 1 / (1 + exp(-(alpha + k * ln(x / (1 - x))))) with
  x clamped to [1e-9, 1 - 1e-9]. It is fitted each cycle on its base alone, by binomial
  maximum likelihood over the base's states with n_B >= 2, c_B >= 1 and 0 < u < 1, each
  contributing R_B - U_R successes in c_B trials; Newton from (alpha, k) = (0, 1), converged
  when the step's largest component is below 1e-12, at most 100 iterations. Not converged
  (separation included), or k <= 0: not fitted.
- (c) is the anchored bilogit: logit Shat(T) = logit S(B) + rho * [logit V(T) - logit V(B)],
  logit x = ln(x / (1 - x)), V clamped to [1e-9, 1 - 1e-9], S = REP seats / decided seats.
  REP = the nearest integer to 435 * Shat, a half going to B's majority.
  rho = sum(x * y) / sum(x^2) over the window's transitions, x = logit V(t) - logit V(t-1),
  y = logit S(t) - logit S(t-1). The window is every transition of the record whose later
  election precedes T, less any transition in which more than half of the 435 seats were
  redrawn or re-apportioned (the seats of the states whose lines in force or seats changed,
  house_maps.csv). A cycle with an empty window has no (c). A forward swing cycle is a
  swing cycle of D whose window is not empty.
- No change: in every state, B's REP share of its decided seats carried onto n_T by P - B's
  own seats wherever n_T equals B's decided seats - and the delegations those seats give.
- The truth is the record's own district plurality (house_districts.csv's winner). Z_s is a
  state's undecided seats (NC-9 2018); O_s its seats won outside REP and DEM (none in
  2016-2024, by the truth gate). R_s and D_s are the record's decided seats by party,
  REPhat_s and DEMhat_s a row's.
  - W_s, the state's seats misplaced:
    W_s = (|REPhat_s - R_s| + |DEMhat_s - D_s| + O_s - Z_s) / 2 - the distance of REPhat_s
    from [R_s, R_s + Z_s] where O_s = 0; with O_s > 0 it is S7's first scoring, and S7's
    second moves O_s into Z_s.
  - G = the sum of W_s over the 50 states. H = the same sum over the held states.
  - N = the signed distance of REP's national seats from [R, R + Z + O], positive = REP
    over; under S7's first scoring |N| is that distance plus O, under its second O is in Z.
  - K = the delegations, of 50, whose class (REP majority, DEM majority, tied) equals the
    record's under some resolution of the state's undecided seats. With O_s > 0 a class
    compares REP's seats with DEM's alone under S7's first scoring; its second resolves
    O_s's seats as undecided ones.
- Every comparison of W, H, G, N and K in steps 1-5 is made exactly as its operator is
  written, on integer counts, with no noise band; every other test exactly as written.
  Equal is never better.

STEP 1 - (a)'s fallback. Over D's redrawn state-cycles, sum W under F-old and under F-prop.
- F-old is kept if its sum is not above F-prop's, ties included.
- If F-prop's sum is strictly lower, the rule STOPS (S5) - unless step 2 picks (b) and S2 does
  not fire, (b) having no fallback; the comparison is then printed. The rule never switches
  the fallback by itself.

STEP 2 - (a) against (b), on held-state seats misplaced.
- Code recommends (a) if the sum over D of H(a) is not above the sum over D of H(b).
- Code recommends (b) only if all of these hold:
  - H(b) < H(a) in every swing cycle of D;
  - H(b) <= H(a) in the level cycle;
  - the sum over D of G(b) < the sum over D of G(a);
  - K(b) >= K(a) in every cycle of D.
- Otherwise the rule STOPS (S1), naming each condition that failed.

STEP 3 - control. If step 2 picked a method, the rule STOPS (S2) if that method's |N| is
strictly larger than the other's in every swing cycle of D. After S1 both directions are
evaluated and listed under S1, not as a stop of their own.

STEP 4 - (c). The rule STOPS (S3) only if all of these hold:
- D has at least two forward swing cycles;
- in every one, (c)'s |N| is strictly below the step-2 method's (after S1, below both (a)'s
  and (b)'s);
- in every one, (c)'s |N| is strictly below no change's.
(c) is never recommended alone. Its comparisons print in the verdict line.

STEP 5 - the strawman guard. If (b') is fitted in every cycle of D, the rule STOPS (S6) if
(b') beats the step-2 method by step 2's own switch conditions: H strictly lower in every
swing cycle of D, H not higher in the level cycle, the sum over D of G strictly lower, and K
not lower in any cycle of D. After S1, (b') is compared with both (a) and (b) by those
conditions and the results are listed under S1, not as a stop of their own.

STEP 6 - the declared readings. Steps 1-5 are run again under each alternative, one at a
time:
- (i) 2020's truth as published: [HH-DIV]'s division, REP 212 of 434 decided, NY-22
  undecided and scored by interval; (c)'s S(2020) = 212/434;
- (ii) the item's own cycles, D less C1 - {C2, C4}, and {C2, C4, C5} in the run over D plus
  C5: the step-2 verdict and S5 are compared; S2, S3 and S6 are compared only where this D
  has at least two swing cycles, and otherwise printed, since each would rest on one - a
  printed S2 counting as not firing in step 1's exception;
- (iii) Minnesota 2018 held; (iv) Colorado 2018 held; (v) both held;
- (vi) the swing on the deciding count: V_dc(E) = sum final_r / sum (final_r + final_d)
  over the 435 races replaces V(E) in every method - PRINTED ONLY: choice 4 answered,
  the swing on US-5's own-line series (G1, section 797);
- (vii) the level cycle out of the sums: every sum over D in steps 2 and 5 taken over D's
  swing cycles alone.
A reading made printed only by an answered choice is run and printed, and compared with
nothing. The rule STOPS (S4) if any other reading changes the outcome, naming the reading.

STEP 7 - two parties. The rule STOPS (S7) if a measured cycle whose later election is after
2024 has a winner outside REP and DEM, printing that measurement under each of S7's first
two scorings. (In 2016-2024 such a winner fails the truth gate, a data defect.)

TIE-BREAKS: in step 2, equal sums over D of H go to (a), and equal sums over D of G fail
(b)'s condition; in step 1, equal sums go to F-old.

THE OUTCOME of a run is the step-2 verdict - (a), (b) or S1 - with the set of the other stops
that fire: S2, S3, S5, S6, S4 with the readings that fire it, and S7. Its figures are not
part of it. S4 compares outcomes less S4 and S7. A run is decided - "code recommends (a)"
(with F-old) or "code recommends (b)" - when step 2 picks a method and no stop fires;
otherwise the rule does not decide, and every stop is listed with its figures. The rule
reaches (a) with F-old, (b), or an ask - never F-prop, (b') or (c) by itself. While
reading (vii) stands, C4 carries no switch without an ask: its own conditions only veto,
and where its figures in step 2's or step 5's sums decide, reading (vii) stops the rule.
The verdict line is generated, never typed:
- decided: "By R-US18's rule, code recommends (a): ..." (or (b)), with H, G, K and N per
  cycle and summed, (c)'s comparison, and "the same under every declared reading";
- otherwise: "R-US18's rule does not decide: <each stop with its figures> - asked."
R-US18 is asked with the full table whatever the outcome.

THE STOPS' OPTIONS, each with "amend the rule as ruled" besides:
- S1: (a) as measured, or (b) as measured.
- S2: the picked method, or the other.
- S3: the step-2 method as measured (after S1, (a) or (b) as measured); (c)'s total with
  that method's delegations reconciled (after S1, either's); or another delegation rule for
  (c), not yet specified, which leaves R-US18 asked.
- S4: the declared reading, with its reason from the game's model, or the alternative - put
  as conventions, never as "X with its verdict".
- S5: F-old; F-prop, persisting in every state it counts; or sourcing published results by
  new district for each enacted plan before US-16 and US-27.
- S6: the step-2 method as measured, or (b) declared again by amendment as (b')'s curve.
- S7: the seat scored as misplaced for every method alike; scored as undecided by interval;
  or the two-party premise amended (SP-6b's third party).

THE ASK AND THE BARS
- The ruling record is held by the instrument: NONE; ASKED, with the ask's section, the
  outcome of each run asked and the decisive digest; or RULED, with the ruling, its option,
  the outcome of each run it was made on (the run over D and, once C5 is measured, the run
  over D plus C5, with step 1's result over C5's redrawn states where the ruled option
  counts them by (a)'s fallback) and the decisive digest it was made on. The line after
  this block mirrors its state, section and ruling, never a digest, and the instrument
  fails while the two differ.
- The decisive digest is SHA-256 over a fixed list of figures, each computed whatever an
  earlier condition of its step gives, under every reading not made printed only: W per
  redrawn state-cycle of D under F-old and under F-prop; H, G, N and K of (a) with F-old,
  of (b) and of (b') in every cycle of D, and whether (b') is fitted; where D has at least
  two forward swing cycles, N of (c) and of no change in each; once C5 is measured, the
  same over D plus C5. Nothing else is in it.
- NONE: every run fails the bars until its ask is registered. R-US18 is asked whatever the
  outcome, decided or stopped, so the commit that first runs the rule registers the ask;
  NONE holds only before it.
- ASKED: the bars pass while the decisive digest and each run's outcome equal the registered
  ones. A moved digest or a changed outcome fails until the ask is registered again with
  the new table, and Elias is told.
- RULED: the bars pass while each run's generated outcome equals that run's outcome in the
  ruling. A moved digest with the same outcomes fails until a commit re-pins it, naming the
  old digest, the new one and "outcome unchanged". A changed outcome fails until R-US18 is
  asked again; it is never re-pinned. A run the ruling was not made on - the run over D
  plus C5, under a ruling made before C5 was measured - has no outcome in it: it fails the
  bars until R-US18 is asked again with the new table, and the new ruling records every
  run. A ruling binds a decided verdict as it binds a stop.
- A ruling must name an option that can be built: (a) with F-old or with F-prop; (b),
  including (b) declared again by amendment as (b')'s curve - saying what it gives where
  the curve is not fitted, and whether its two parameters are refit on the game's previous
  House or held from the record; or (c) reconciled to (a) with F-old, to (a) with F-prop or
  to (b). Otherwise the record stays ASKED.
- US-16's Congress-election diagnostic asserts R-US18 RULED, with an option that can be
  built and a current digest. While not, it fails.

C5 (2024->2026) is BILLED until the Clerk's 2026 statistics are saved. Then it is measured,
a swing cycle; the rule is run again over D plus C5 - in that run every D of this block
reads D plus C5 - and step 1 over C5's redrawn states where the ruled option counts them by
(a)'s fallback. Either run giving an outcome other than its own in the ruling - for step 1
over C5's redrawn states, a result other than the one recorded in the ruling - or a run the
ruling was not made on (THE ASK AND THE BARS) asks R-US18 again, and the bars fail until the
ask is registered. D itself changes only by amendment.
```
<!-- R-US18 RULE END -->

**Ruling record:** RULED (§856): I1 - (b), state delegations by a state swing (S2 answered by the other method); S5 and S4 [(ii)] moot, (b) having no fallback; a map redrawn since the base enters once an election has been held on it (DECLARED).

**The block's digests** (the instrument's list): preregistered `b827a7f234c2a34933c0e92d506beaa190f16e7655e6b044682f7fe73280152c` (§791); AMENDED (G1, §797): `b827a7f234c2a34933c0e92d506beaa190f16e7655e6b044682f7fe73280152c` -> `5c89b0cf400e50c0e64e687f3f05b8cb74f13f76384bca184773f3724673307a`.

**Asked blind, before the first run (§791).** The rule runs under seven choices, each with a default. The instrument is built once Elias has answered them or given his word to run on the defaults (US-11's *Needs*): the figures, once on the card, would be in sight of any later answer. A choice he answers - for its default or for its alternative - is ruled: it is written into the block by amendment, and its S4 reading becomes printed only (choices 1-4 and 7: readings (ii), (i), (iii)-(v), (vi) and (vii)). Word to run on the defaults rules none of them: each keeps its default and its S4 reading. Choice 5 has no S4 reading: an answer for (b')'s curve declares (b) again by amendment - saying what (b) gives in a cycle where the curve is not fitted, and whether its two parameters are refit in play - and S6 then has nothing to compare; an answer for the default leaves S6 as written. Choice 6 rewrites THE TERMS' first year of the record.
1. *The deciding cycles.* D with C1 (2016→2018) - the record's largest swing and its only midterm on mostly held lines, the kind of election a 2024-start game meets on 3 Nov 2026, its first midterm (its first House count, 5 Nov 2024, is a presidential year's) - by default; or the item's own cycles less C3, {C2, C4}.
2. *2020's truth.* The district plurality on the Clerk's counts, NY-22 to the winner that count names - the game counts all 435 seats by plurality and models no certification - by default; or the division as published, NY-22 undecided: `[HH-DIV]`'s 212 of 434, which the Clerk's own table of political divisions in the 2020 volume prints too.
3. *Minnesota's and Colorado's 2018 lines.* Redrawn, as the Census Bureau's list reads them - the binary rule takes no view of a change's size - by default; or either or both held (Minnesota's change "cosmetic" on the Bureau's pages, Colorado's undescribed). Pennsylvania 2018 is not asked: its change is taken as a redraw on the Bureau's list, DECLARED - the "court-ordered" plan the brief named is on no saved page, BILLED.
4. *The swing's basis.* US-5's own-line series, the poll's own measure under R-US15 (a), by default; or the deciding count the shares are read on. It is also R-US19's B7 (§794): an answer to either answers both.
5. *(b)'s seat rule.* Anchored proportional, with no parameter, by default; or (b')'s two-parameter curve.
6. *The record before 2016.* Not sourced, by default; or the Clerk's statistics before 2016 sourced, which would give (c) a forward fit for C1 and make S3 live.
7. *C4 in the sums.* Counted in step 2's and step 5's sums over D, as the panel's arithmetic has it, by default; or left out of them, as the panel's own account of C4 ("C4 can only veto") reads - the two part only where C4's near-zero swing moves a seat (§791).

**Answered (Elias's ruling G1, 2026-10-06; installed §797):** run on the defaults as printed - the word that rules none of them - but for the choices he answered: **the swing's basis is US-5's own-line House vote** (R-US18's choice 4 and R-US19's B7, one answer; their readings (vi) and (vii) made printed only), and **a full tie (B2) comes to Elias with the figures**. Both instruments run as preregistered; a result never changes a choice - a choice changed after a result is in sight needs a new digest and a report to Elias.

**Confirmed (Elias's ruling H4, 2026-10-13; recorded §803):** the outcome asked at §800 is confirmed as listed - (a) + S2, S5, S4 [(ii)]. **An unfitted (b') prints as UNFITTED and never counts in the decision**; where the block says otherwise, the block's text wins and Elias is told. Read against the block: step 5 runs only where (b') is fitted in every cycle of D, and the rule never reaches (b') by itself, so the decision agrees; the block's decisive digest does carry whether (b') is fitted and its figures, so a change there moves the digest and asks R-US18 again - the text stands there. The ruling record stays ASKED: the block's THE ASK AND THE BARS has a ruling name an option that can be built, and a confirmation names none. **The UNFITTED label is built (§830):** an unfitted (b')'s row prints `UNFITTED` and the exit that failed, with no figures, and step 5 names the cycles; the decision never reads it. The text stands in two places, and Elias is told: the decisive digest carries an unfitted (b')'s figures and whether it is fitted, so an unfit moves the digest and asks R-US18 again (a planted run, §830); and in the run over D plus C5, not built, reading (ii)'s D (C2, C4, C5) compares S6, so a (b') unfitted in C1 alone would skip the main run's step 5 while reading (ii)'s ran, and S4 could fire on that difference.

**RULED (Elias's ruling I1, 2026-10-18; installed `COMPLETED.md` §856; verbatim in `docs/RULINGS_2026-10-18.md`):** S2's stop is answered by the other method: **(b), state delegations by a state swing.** (b) about halves the national miss over D and calls the House majority right in C2, where (a) does not; it costs held-state seats and two delegations per swing cycle, and the contingent call is right either way. **S5 and S4 (ii) are moot**, because (b) has no fallback. **DECLARED:** a map redrawn since the base enters once an election has been held on it. The instrument holds RULED, option (b), with the outcome and the decisive digest the ruling was made on (`UsHouseCountCheck`); US-16's Congress-election diagnostic asserts it.


**R-US19 — The Senate races** *(US-12's table; blocks US-16)*.
- (a) The state's presidential-level derived share that day (a straight ticket);
- (b) the seat's previous result swung by the national change;
- (c) either, with per-race factors fitted once on 2024's Class I and its specials (E1's principle; later fields at 1.0).

Code will recommend whichever misses fewer 2024 Class I seats. From memory, Democrats won Senate races in several states Trump carried in 2024, which counts against (a); it is measured, not assumed.

**The rule, declared before the instrument's first run (§794).** As R-US18's was (§791), it was specified by a design panel blind to any computed result of either method on the 2024 seats: the panel was told not to open the session's two scratch maps of US-11 and US-12 and not to compute, estimate or reason from any method's result on the 2024 seats, from the catalogs, by hand or from memory. Its brief held the plan's question, the rulings it answers, the game's constraints and the catalogs' structure, with the irregular seats described (Maine's winner among them, an independent with a majority) - no vote figure and no method's result. Its tool calls are audited in §794: no map opened, no method's result computed, the 2024 race rows it printed carrying candidate counts and flags only; but it saw the 2024 outcome as other records hold it. The precedent designer printed `senate_on.csv` (the Senate by class and party on named days, Class I's split on 3 Jan 2025 among them) and read the session's memory note (US-4's presidential results and the 119th's opening division) and `UsStateSwingCheck`'s pins, under which R-US14 (a) from 2020 calls 2024's presidential states exactly. The game-use designer's grep of the US card printed US-4's 2016→2020 and 2020→2024 rows (every method calling 2024's presidential states right) and R-US15's table with US-5's national own-line House shares, 2024's among them - the levels from which (b)'s national change follows. Those two designers read `senate_record.md`'s race half, which names the 2024 specials' winners and holds every winner to the roster. The methods critic and the reviser printed independents' and some holders' rows of `senate_seats.csv`, and the reviser's grep of `senate_record.md` for "reach" displayed its roster count at the record's reach (2026-09-06), as the specification itself records. Six of the seven agents read R-US19's entry above, with its from-memory remark against (a); the methods critic did not open the plan, and its brief quoted the entry's three options and its recommendation sentence without the remark. The specification says no choice uses any of these. The session, which had read the maps (one of them reasons from memory about seats a straight ticket would miss), condensed the panel's final specification into the block below, the eight choices asked blind after it, and US-12's *Needs* and *Design*; the specification is kept verbatim in `Reviews/2026-10-06_s794_us12_blind_panel.md`, and §794 lists each departure of the plan from it and holds the preregistered digest. The rule is the fenced block between the two markers below; everything outside the markers - the ruling record's mirror, the asks - is not part of it. The instrument (`UsSenateRaceCheck`, on `UsStateSwingCheck`'s pattern, in the cheap and documents bars, writing its own GENERATED block on the US card) holds the list of the block's SHA-256 digests, computed and enforced as R-US18's are: the preregistered one, then one per amendment naming Elias's ruling; the block must match the list's last digest, and a change without a ruling fails. The catalog step the block reads (US-12's *Design*) follows this preregistering commit and measures nothing.

<!-- R-US19 RULE BEGIN -->
```text
R-US19'S RULE - US-12's measurement decides how the game elects the Senate's seats.
Preregistered in COMPLETED.md section 794; amended at section 797 (Elias's rulings G1-G3).

THE TERMS
- A side is REP or DEM. A candidate's side: R gives REP and D gives DEM, by the Clerk's
  printed label; I gives the conference named by the caucus of his senate_seats.csv holder
  row (R-US10 (a), matched by state, class and surname), and no side if he holds no seat in
  the catalog's window or his caucus is N (a sourced "neither"); any other party and every
  write-in has none. A caucus read in 2023-2026 is carried back to the same person's
  earlier races, declared (no saved page dates an earlier caucus). A nomination the Clerk
  does not print is not read. A holder's side: REP for party R, DEM for party D, and for
  I or D/I the conference named by his caucus, none if it is N.
- A race's deciding count: the runoff wherever one was held under state law (the Clerk's
  marks or a saved certified state page; a runoff on neither is BILLED, never replaced by
  the November count); a ranked count's last round (the FEC's footnote rounds or a saved
  state page; else the Clerk's first choices, declared); otherwise the general, a
  candidate's fusion lines summed. Its base count: the last count of that sequence in
  which both sides have a candidate; where no count of it has both, the deciding count,
  flagged. In a count, each side's votes are summed over its candidates. gen_r and gen_dc
  (the general's ballot), final_r and final_dc (the base count), final_r_top, final_dc_top
  and final_none_top (each side's strongest candidate,
  and the strongest with no side, in the base count) and base_count (which count, on which
  page) are generated by Tools/us_senate_races_prep.pl and re-read by the cheap bar, never
  typed; an independent's votes that cannot be separated from another's are a data defect.
- The cycles, base year -> target year: K1 2018->2024 Class I (a presidential year); K3
  2016->2022 Class III (a midterm), read from the saved Clerk statistics and FEC sheets
  before the first run; K2 2020->2026 Class II, BILLED. D = {K1, K3}.
- A cycle's count: its target year's regular full-term races of its class - 33 in K1, 34 in
  K3, one per state of the class, asserted for the default seat set (a reading that removes
  seats prints its own count). A special to another class is printed only. A same-seat,
  same-day special (California's Class I contest of 2024) is never counted, and each
  method's call on it is asserted equal to its call on the full term.
- A seat's base: its latest race of record to its state and class in a year before the
  target year, regular or special; where one seat had two races that year, the full-term
  race. A race of record missing from the catalog is BILLED, never served by an older row.
- V(E) = sum votes_r / sum (votes_r + votes_d) over house_by_state.csv's 50 rows of year E,
  as US-5 holds it, never recomputed. A January runoff takes its general's year.
- x(base) = final_r / (final_r + final_dc) where both are positive. Otherwise - a base
  with no count in which both sides stand (California 2018's top two, an unopposed race,
  an independent of no side against one party) - x(base) is F-H: votes_r / (votes_r +
  votes_d) of house_by_state.csv's row for the base's state and year; where that row also
  lacks a side, the state's two-party presidential share of record at the latest
  presidential year at or before the base (president_by_state.csv). Every substitution
  is flagged and printed with whether its side agrees with the base winner's.
- (b) = x(base) + V(T) - V(base year), unclamped (clamped for printing only).
- (a), the state's REP share of REP+DEM by R-US14 (a)'s derivation (RegionalVoteModel.
  RegionalSharesByUniformSwing, unchanged, its residual asserted below 1e-9), from the
  previous presidential election's statewide rows (others pooled; weights that election's
  votes_total, declared (US-4); DC a jurisdiction, never a seat; the Maine and Nebraska
  district rows not read):
  - at a presidential T: derived from T-4 at T's true national presidential shares
    (sum votes_d, votes_r, votes_other over sum votes_total over T's 51 rows);
  - at a midterm T: a_s(P) + V(T) - V(P), P = T-2, a_s(P) being P's derivation from P-4
    at P's true national shares.
  Every seat of one state on one day takes one share.
- A call is REP above 0.5 and DEM below. At exactly 0.5: the side of the seat's holder at
  the end of the polling day; where the seat is vacant or its holder has no side, the side
  holding the Senate majority that day, the Vice President's vote counted; where no side
  holds one so counted, the instrument fails (a defect) and prints the seat. A call by
  this cascade is printed.
- No change: each seat to its base winner's side.
- The identity: every base of D, and every play base the catalog step reads with them
  (the 2020 Class II and 2022 Class III races, each special's previous race), whose winner
  has a side has x > 0.5 exactly where that side is REP and x < 0.5 exactly where it is
  DEM, substituted shares included. A base that fails it is S8's.
- The truth: the side of the winner of the target race's deciding count (senate_races.
  csv's winner). Z: the count's seats with no truth (none by default). O: its seats whose
  winner has no side.
- M = the seats of the count whose call differs from the truth's side; a Z seat never; an O
  seat a miss for every method alike under S7's first scoring - the default for steps 1-4,
  the printed figures and the digest - and moved into Z under its second.
- N = the signed distance of the called REP seats from [R, R + Z + O], R the truth's REP
  seats; positive = REP over; the same under both of S7's scorings. Under the first, |N| + O
  is printed as that scoring's count; it adds the same O to every method and changes no
  comparison.
- Every comparison of M and N is made exactly as written, on integers, with no noise band.
  Equal is never better.

STEP 1 - (a) against (b), cycle by cycle.
- If M(X) <= M(Y) in every cycle of D and M(X) < M(Y) in at least one, code recommends X
  (by M).
- If M(X) < M(Y) in one cycle of D and M(Y) < M(X) in another, the rule STOPS (S1), naming
  each cycle with its M, whatever the sums.
- If M(a) = M(b) in every cycle of D: code recommends X if |N(X)| <= |N(Y)| in every cycle
  of D and |N(X)| < |N(Y)| in at least one (by |N|); otherwise TIE - the rule does not
  decide, and the tie is put to Elias with the figures (B2 answered, G1, section 797).
- Asserted: the rule never recommends a method with more misses in K1 than the other.

STEP 2 - control. Where D has at least two cycles and step 1 picked a method by M, the rule
STOPS (S2) if the pick's |N| is strictly above the other's in every cycle of D. Where D has
one cycle, S2 is printed and counts as not firing. After S1 or TIE both directions are
listed there, not as a stop of their own.

STEP 3 - (c), a parent ((a) or (b)) with per-race factors fitted on 2024's 33 regular Class
I races, California's Class I contest and Nebraska's Class II special: p the parent's REP
share and s = final_r / (final_r + final_dc) (where no count has a candidate of one side,
final_none_top stands for the missing side, declared; where that is zero too, not fitted
and at the parent's call), both clamped to [1e-9, 1 - 1e-9]; c_j = logit s - logit p;
applied as logistic(logit p + c_j), the parent's call rule and cascade. On every field
after 5 Nov 2024 its factors are 1.0. Printed per parent: (c)'s M and N on K1 as computed,
labelled "in-sample; zero by construction except O seats, races not fitted, and races
whose base-count share and deciding-count winner disagree"; per race c_j; mean, largest
and RMS of |c_j| over the fitted races, the others named; the races whose call c_j
reverses. (c) is in no step, never in the digest, never recommended and never a stop.

STEP 4 - the declared readings. Steps 1-2 are run again under each, one at a time:
- (i) D = {K1};
- (ii) the specials in the count: Nebraska's 2024 Class II special on its 2020 base, and
  K3's specials to other classes on theirs;
- (iii) target races with a side absent from the general's ballot (gen_r = 0 or gen_dc =
  0) out, for both methods;
- (iv) a base with no count in which both sides stand at 1 or 0 by its winner's side;
- (v) the seats whose base has no such count out, for both methods;
- (vi) each side's strongest candidate alone in every base count (final_r_top,
  final_dc_top in place of final_r, final_dc);
- (vii) V_dc(E) = sum final_r / sum (final_r + final_d) over house_districts.csv's 435
  races of E replaces V(E) in (a) and (b) - PRINTED ONLY: B7 answered, US-5's own-line
  series (G1, section 797);
- (viii) (a) by R-US14 (b), proportional; (ix) (a) by R-US14 (c), plain additive - both
  PRINTED ONLY: R-US14 ruled (a) (G2, section 797);
- (x) (a) at a midterm re-derived: pi(P) = P's true national REP share of REP+DEM, o =
  P's national others' share, pi* = pi(P) + V(T) - V(P), asserted in (0, 1), N(T) =
  ((1 - o)(1 - pi*), (1 - o) pi*, o); the state shares by R-US14 (a)'s method from (a)'s
  own derived three-way shares at P, weighted as they were derived; asserted to return
  a_s(P) within 1e-9 at V(T) = V(P);
- (xi) a truth or base side resting on a caucus put to Elias (R-US10's reading of Sinema)
  at its alternative, an independent of no side - PRINTED ONLY: the reading ruled (G3,
  section 797).
A reading made printed only by an answered choice or a ruling is run and printed, and
compared with nothing. The rule STOPS (S4) if any other reading changes the outcome, naming
the reading. S4 compares outcomes less S4, S7, S8 and S9, and, where a reading's D has one
cycle, less S2.

STEP 5 - two sides. The rule STOPS (S7) if a cycle of D has O > 0, printing its measurement
under two scorings: a miss for every method alike; and O moved into Z.

STEP 6 - premises waiting on a ruling (registered, never a red bar):
- S8: a base of D, or a play base read with them, whose share fails the identity,
  two-sided or substituted. Steps 1-5 run with the share as computed, the base flagged.
- S9: a target or base race of D whose deciding count rests on no saved page. Steps 1-5
  run over the cycles fully read, printed.

THE OUTCOME of a run is step 1's verdict - (a), (b), TIE or S1 - with the set of the stops
that fire: S2, S4 with the readings that fire it, S7, S8 with its bases, S9 with its races.
Its figures are not part of it. A run is decided - "code recommends (a)" or "code
recommends (b)" - when step 1 picks a method and no stop fires; otherwise the rule does not
decide, and every stop is listed with its figures. The rule reaches (a), (b) or an ask,
never (c). The verdict line is generated, never typed:
- decided: "By R-US19's rule, code recommends (a)|(b): ..." with M and N per cycle and
  summed, the discordant seats d_ab:d_ba with the exact two-sided sign-test p per cycle and
  pooled ("within the record's noise" where p > 0.05), "the same under every declared
  reading", and "(c) is in-sample by construction";
- otherwise: "R-US19's rule does not decide: <each stop with its figures> - asked."
R-US19 is asked with the full table whatever the outcome, (c) beside it as a design
preference.

PRINTED, never deciding: control per method (the class's calls with the holdovers on the
next 3 January, vacancies named; 51, the Vice President at 50, cloture's 60; against
[SEN-DIV]); the mean absolute two-party miss, labelled not neutral; the per-seat table
(base race, its counts and their sources, the DEM side's make-up, each fallback and its
side agreement, x, the change, each method's share and call, the truth, misses split "a
only", "b only", "both", and every cascade); no change; (a-record), the state's own
presidential share of record at T; (a) at the House V; (a) at a midterm on 2018 Class I
from 2016's derivation; (x)'s variant from P's record rows; (b) proportional (= logit); the
hybrid-midterm row on K3 (each seat's (a) share at its base year plus V(2022) - V(base
year), labelled "an illustration; not measured as play runs it"); the game's range per
method - the REP-caucus Senate at V from 40 to 60 percent, 5 Nov 2024 and 3 Nov 2026; the
exceptions named (fusion, Arizona 2018's unnamed entry, Ohio 2018's FEC difference,
Mississippi's marks, California's top two and two contests, Vermont, Nebraska, Maine, West
Virginia's oath, every fallback, Alaska 2022's one-sided last round, Georgia's runoffs,
K3's other runoffs and ranked counts). The holder on 3 Jan 2025 is printed beside the
truth, not a reading: it differs only in West Virginia's seat, the same person sworn late,
and no published division disagrees.

THE STOPS' OPTIONS, each with "amend the rule as ruled" besides:
- TIE: (a); (b); or source 2012->2018 Class I (K0) before ruling.
- S1: (a); (b); (a) on presidential days and (b) at midterms; (b) on presidential days and
  (a) at midterms - each hybrid labelled "in-sample by construction (each cycle's
  winner)", the hybrid-midterm row beside it; or source K0.
- S2: the picked method, or the other.
- S4: the declared reading, with its reason from the game's model, or the alternative - put
  as conventions, never as "X with its verdict".
- S7: the seat a miss for every method alike; undecided by interval; or the two-side
  premise amended (R-US10's third unit, US-15).
- S8: that base by F-H (accepting the crossing), at 1 or 0 by its winner's side, or - for a
  base of D only - out of the count (play still needs one of the others, named in the
  answer); or the base count's definition amended.
- S9: source the race; K3 with that seat out; or D = {K1}.

THE ASK AND THE BARS
- The first run waits on Elias's answers to the eight choices asked blind below the block,
  or his word to run on the defaults; the word rules none of them, each keeping its
  default and its reading. An answer after the first run is an amendment in sight of the
  figures and is printed as one.
- The ruling record is held by the instrument: NONE; ASKED, with the ask's section, each
  run's outcome and the decisive digest; or RULED, with the ruling, its option, each run's
  outcome and the decisive digest it was made on. The line after this block mirrors its
  state, section and ruling, never a digest, and the instrument fails while the two differ.
- The decisive digest is SHA-256 over: M and N of (a) and (b) in every cycle of D under
  the default and under every reading not made printed only; the (state, class) of each
  miss per method per cycle under the default; the (state, class) of each O seat and each
  Z seat per cycle of D; the bases firing S8 and the races firing S9; whether K3 was read;
  once K2 is measured, the same over D plus K2. Nothing else.
- NONE: every run fails the bars until its ask is registered; R-US19 is asked whatever the
  outcome, so the commit that first runs the rule registers the ask.
- ASKED: the bars pass while the decisive digest and each run's outcome equal the
  registered ones; otherwise they fail until the ask is registered again and Elias is told.
- RULED: the bars pass while each run's outcome equals the ruling's. A moved digest with
  the same outcomes fails until a commit re-pins it, naming the old digest, the new one and
  "outcome unchanged". A changed outcome, or a run the ruling was not made on, asks R-US19
  again. A ruling binds a decided verdict as it binds a stop.
- A ruling must name an option that can be built: (a), with R-US14's ruled method and its
  midterm form as ruled; (b), with its rule for a base without both sides as ruled and its
  record bases scheduled (2020 Class II before 3 Nov 2026, 2022 Class III before 7 Nov
  2028, each special's base before its election); (a) on presidential days and (b) at
  midterms, or the reverse, each seat storing its share and V whichever method set it; or
  (c) over one of these, naming its parent and, under (b), which share the roll-forward
  stores. Otherwise the record stays ASKED.
- The ruling holds only while the poll is R-US15 (a)'s own-line House vote; a different
  R-US15 ruling asks R-US19 again, and the block is amended so V is the ruled measure.
- R-US14 is ruled (a) (G2, section 797): (a) runs R-US14 (a), and readings (viii)-(ix) are
  printed only.
- US-16's Congress-election diagnostic asserts R-US19 RULED, with an option that can be
  built, a current digest and a V basis equal to R-US15's ruling. While not, it fails.

K2 (2020->2026 Class II) is BILLED until the Clerk's 2026 statistics, the 2020 Class II
races and the 3 Jan 2027 roster with each independent's caucus are saved and read. Then it is
measured and the rule run again over D plus K2; a different outcome, or a run the ruling was
not made on, asks R-US19 again, and the bars fail until the ask is registered. K0
(2012->2018 Class I), named in the options of TIE and S1, is not built: it needs the 2012
Class I races, the Clerk's 2012 House statistics (V(2012) in house_by_state.csv, and 2012
in house_districts.csv for reading (vii)) - which extends US-5's series and R-US18's
record, R-US18's digest re-pinned or R-US18 asked again - and D = {K0, K1, K3} by
amendment. D itself changes only by amendment.
```
<!-- R-US19 RULE END -->

**Ruling record:** RULED (§856): I2 - (c) over (a): the state's presidential-derived share (S1 answered by (a), S4 [(i)] agreeing), with per-race factors fitted on 2024's class only (E1's form), later fields at 1.0.

**The block's digests** (the instrument's list): preregistered `03382d0c0dd22975dea191650f74962bd852477a86ad177944bd93455f425f9c` (§794); AMENDED (G1-G3, §797): `03382d0c0dd22975dea191650f74962bd852477a86ad177944bd93455f425f9c` -> `1caec656193a6d2d2c9c235af0cefe4d591134a24a3f7d338be0dac0108e3957`.

**Asked blind, before the first run (§794).** The rule runs under eight choices, each with a default. The instrument is built once Elias has answered them or given his word to run on the defaults, as for R-US18's seven: one word can cover both lists. A choice he answers - for its default or for its alternative - is ruled: it is written into the block by amendment, and its S4 reading becomes printed only (B1 (i), B3 (ii), B4 (iii), B5 (iv)-(v), B6 (vi), B7 (vii), B8 (x)). Word to run on the defaults rules none of them. B2 has no reading: an answer for (a) or (b) is written into step 1. Readings (viii)-(ix) become printed only when R-US14 is ruled, and (xi) when Elias rules R-US10's reading of Sinema.
1. *B1 - the deciding cycles.* 2016→2022 Class III (a midterm, from the Clerk's statistics already saved) joins 2018→2024 Class I, D = {K1, K3}, by default: a game's second Senate election is a midterm, (a) at a midterm runs a declared form only a midterm cycle tests, and one cycle of 33 mostly safe seats cannot separate methods a seat or two apart; under step 1, K3 decides alone only where K1 is tied on misses, and where the cycles part the rule stops; with two cycles step 2's control stop (S2) is live, a K1 tie on misses broken by |N| in K1 alone becomes TIE where K3's |N| points the other way, and K3's own seats and bases can fire S7, S8 or S9. Or K1 alone, the rule's text, K3 printed.
2. *B2 - a full tie* (equal misses in every cycle, |N| not separating): asked with the table, by default - the plan's text names no ground; or (b), native at every election and R-US18 (a)'s form; or (a), one state geography for both contests and no Senate base to store.
3. *B3 - the specials.* Nebraska's 2024 Class II special (and K3's specials to other classes) printed only, by default - R-US19 names Class I; or in the count on its own base.
4. *B4 - a target race with no candidate of one side on the general's ballot* (Nebraska's 2024 Class I race; sides read by caucus, so Vermont has both): scored against its winner's side, by default - neither method reads the target ballot and play cannot know it; or out for both methods.
5. *B5 - a base with no count in which both sides stand* (California 2018; in K3 and in play any such base): F-H - the state's own-line House share in the base's year, then the presidential share of record, a share on the wrong side of its winner firing S8 - by default, since under US-26's roll-forward 1 or 0 would freeze a statewide seat, one of 100, for the whole game; or 1 or 0 by the base winner's side (R-US18's district rule); or out of the decisive count (play still needs one of the others, named in the answer); or F-H where its side agrees with the winner's, else 1 or 0.
6. *B6 - several candidates on one side* (Maine's caucusing independent beside a Democrat; a top-two or ranked count's several of one party): their votes summed, by default - R-US10 (a) seats a caucusing independent with the Democrats and the game holds one unit per side; or each side's strongest candidate alone.
7. *B7 - the swing's basis* for (b) and for (a)'s midterm form: US-5's own-line series, by default; or the House deciding count. It is R-US18's choice 4: one poll's change moves both chambers, so an answer to either answers both, whichever comes first, making both readings printed only (R-US19's (vii), R-US18's (vi)); a word to run on the defaults for either leaves both live.
8. *B8 - (a) at a midterm* (US-27): anchored - the share (a) held on the last presidential day moved by the poll's change, never re-solved, telescoping like (b) and R-US18's form - by default; or re-derived - the presidential share P's derivation was solved to, moved by the poll's change, then R-US14's method from the state shares the game held at P.

**Answered (Elias's ruling G1, 2026-10-06; installed §797):** run on the defaults as printed - the word that rules none of them - but for the choices he answered: **the swing's basis is US-5's own-line House vote** (R-US18's choice 4 and R-US19's B7, one answer; their readings (vi) and (vii) made printed only), and **a full tie (B2) comes to Elias with the figures**. Both instruments run as preregistered; a result never changes a choice - a choice changed after a result is in sight needs a new digest and a report to Elias. R-US14 ruled (a) makes readings (viii)-(ix) printed only (G2), and Sinema's reading ruled makes (xi) printed only (G3).

**Confirmed (Elias's ruling H4, 2026-10-13; recorded §803):** the outcome asked at §801 is confirmed as listed - S1 + S4 [(i)]. The ruling record stays ASKED, as R-US18's does: a confirmation names no option the block's THE ASK AND THE BARS can build.

Asked beside the verdict, not blind ((c) decides nothing, so its figures cannot steer a verdict): *should a 2024-start game's 5 Nov 2024 Senate class reproduce 2024's races where its vote follows history?* No; yes on (a); yes on (b) storing the factored share; yes on (b) storing the parent's.

**Elias's rulings of 2026-10-06, G1-G9 (installed §797).** G1-G4 are written where each question stands (R-US18's and R-US19's blocks and asks, R-US10, R-US14, R-US15, US-11's *Design*). The rest:
- **G5, the open readings - PROVISIONAL on the recommendation where reversible, stopped where not.** Each line: the reading | the recommendation | if it is wrong. Every one is already built on its recommendation; none is changed by G5. Their records: `ElectionsData/usa/president_returns.md` reading 3; `ElectionsData/poland/coalition_declarations_2023.md` §12; `COMPLETED.md` §778-§780, §783, §784.
  1. *US-3 reading 3:* NARA names 2016's seven faithless votes' recipients, but only *Chiafalo* names electors (three of seven) | accept as built - the three named, the four recorded as unnamed | four names absent from a record; no game value moves (the electors are not modelled). PROVISIONAL.
  2. *§784 (1):* a line starts only on words naming the party (a leader's name keys it) | as built | facts 8-9 start later (late June or July 2023); no polling-day line moves. PROVISIONAL.
  3. *§784 (2):* 27 March's "interest in ministries" is no opening (it names no partner) | as built | facts 5-7 end 27 March and restart in June or August; no polling-day change. PROVISIONAL.
  4. *§784 (3):* the 1 March quiz answer is no line | as built | fact 9 starts 1 March, fact 4 goes. PROVISIONAL.
  5. *§784 (4):* a Rada Liderów member before 14 Feb 2023 is not "its leader" | as built | Bosak's post of 20 Jul 2022 starts fact 8 and the line stands from Poland's opening day. PROVISIONAL.
  6. *§784 (5):* a member party's account is not the party's own | as built | facts 3-9 start in 2022; read as keying PSL and Lewica, the lines toward TD and NL turn one-way and support-blocking **on polling day** - the one reading that would move a polling-day line, unmeasured: measured before any change. PROVISIONAL.
  7. *§784 (6):* Winnicki's line of 5 Sep 2022 is no refusal | as built | fact 3 starts then. PROVISIONAL.
  8. *§784 (7):* refusing Solidarna Polska does not key PiS's list | as built | fact 3 starts 19 Jan 2023. PROVISIONAL.
  9. *PiS→KO (a):* the "TVN24, PAP" credit on Kaczyński's words of 23 Jul 2023 is a relayed wire, not TVN24's own page | as built | PiS→KO starts 23 Jul instead of 8 Sep; no polling-day change. PROVISIONAL.
  10. *PiS→KO (b):* [PIS-P1]'s Tusk passage names no power, so it is not F1's | as built | as (9), on PiS's own record. PROVISIONAL.
  11. *US-2's shadow asymmetry:* a new game's no-policy shadow (no player) never seats the record; a shadow forked from a load does | as built, routed to §618's R5 (is the shadow seeded or forked) | a US game's "without your policies" line differs between unbroken and loaded play after the oath. PROVISIONAL (the shadow is never saved).
  12. *§778, the base share:* the bloc vote carries no abstentions | as built | vetoes rarer than the record's. **STOPPED - not reversible**: carrying abstentions changes how every chamber votes and moves every baseline; nothing is changed.
  13. *§778, the Tribunal:* a referral counts as not vetoed | as built | the generated veto rates change (regenerated table). PROVISIONAL.
  14. *§778:* a backing party with no member voting puts nothing at risk | as built | every ordinary statute at risk at the pooled rate. PROVISIONAL.
  15. *§778:* a president no party backs puts nothing at risk | as built | as (14). PROVISIONAL.
  16. *§778, DECLARED:* a party with no position on a bill's axes counts as abstaining | as built | fewer statutes at risk. PROVISIONAL.
  17. *§779 (F5):* losing the frame decision to the alternative is a budget adopted, not a bill lost | as built | that day's government is not put under cabinet pressure. PROVISIONAL.
  18. *§780 (F7's world):* the acceptance world is the game's own count (KO+TD+NL forms), not the record's seats | as installed | a formation change owed. The recommendation is the status quo; **adopting the other reading is STOPPED** (a formation change across countries, pins, saves and history).
  19. *§780 (F2):* a broadcaster's page relaying another outlet's interview dates nothing | as built | the June opening dated 26 June; no polling-day change. PROVISIONAL.
  20. *§780 (F2):* a newspaper's own video programme is no broadcaster | as built | as (19). PROVISIONAL.
  21. *§780:* "always wins" read as §652's condition | as built | nothing changes since §784. PROVISIONAL.
  22. *§780:* the starts not read as F1's (NL→PiS 2021, NL→Konf's support half, KO→PiS 2022) | as built | earlier or support-blocking starts; no polling-day change. PROVISIONAL.
  23. *§780:* [TD-P11], edited on 1 Jul 2023, dated 15 May | as built | the line starts 10 Aug 2023. PROVISIONAL.
  24. *§780:* [KONF-I4] read as Mentzen's words verbatim | as §776 read it | matters only under (3). PROVISIONAL.
- **G6, the order:** US-20 next - a money path: the full instrument and film proof, Poland shown unchanged on the simulation bar and on film before the per-country procedure lands, a single-agent review; then US-10, US-14 and US-32. R-US18's and R-US19's instruments (G1) are built after US-20.
- **G7, US-32:** the GPS file fetched, its digest kept and not the raw file; a move of any party's cast reported, each move's size, before it is installed.
- **G8, accepted as filed:** PF-18, PF-19, Smith's footnote mark, Florida's oath of 8 Jan 2019, the 2024 economic-vote note. PresidentialVoteBacktest's card and the stale texts land with US-6.
- **G9:** no page; this register stays in the plan.

**Elias's rulings of 2026-10-13, H1-H5 (sent 6 Oct, received 13 Oct; recorded §803, built after the gameplay audit).** H1-H3 are written at R-US17, H4 at R-US18's and R-US19's asks. The rest:
- **H5, the order:** PS-6 is PARKED until the gameplay audit (`docs/audit/AUDIT_PROTOCOL.md`) is done; after it, US-14, then US-32. Read with "build them after the audit": H1-H3's preregistration, fit and mutation suite are US-10's follow-on and come first, R-US18's UNFITTED label (H4) with them (built §830); rule (c) preregistered §832, amended §833, first run §834 (INSTALL), INSTALLED §835 once its rates had gone to Elias (E4); the rule reaches play with US-21, whose needs are unchanged.

**RULED (Elias's ruling I2, 2026-10-18; installed `COMPLETED.md` §856; verbatim in `docs/RULINGS_2026-10-18.md`):** S1 is answered by **(a), the state's presidential-derived share**: the misses tie, (a)'s margin miss is smaller in both cycles, and it is one state geography for the presidency and the Senate. S4 (i) agrees. **The side question: yes.** A 2024-start game in a world that follows history reproduces 2024's Senate class by (a) with its per-race factors (c_j) for that class only (E1's form), later fields at 1.0. So the instrument holds RULED with the option "(c) over (a)" (`UsSenateRaceCheck`).


**R-US20 — Midterms** *(US-13; blocks US-27)*.
- (a) The model as it stands, with the congressional economic vote wired (Table 9.1's US legislative row, the weakest in the sample);
- (b) a *[FITTED]* midterm term on the president's party, read from the record since 1946 — per-term data, as E1's factors are per-candidate data;
- (c) a sourced literature model.

Code will recommend (b) if the model's midterm falls outside the record's range, and (a) otherwise.

**The rule, declared before the instrument's first run (US-13).** It was written before any figure of the model's midterm was computed, by hand or by any run, and before the instrument existed. What the session had seen while writing it, stated: the record's side - [HH-DIV]'s divisions, and a secondary compilation's midterm figures (Vital Statistics on Congress, Table 2-2, fetched while probing for a vote record older than the Clerk's text pages); the probe of the Clerk's statistics, 1944-1996 page images and 1998-2024 text; and the code's constants, from which the size of the legislative cell's effect is bounded (its magnitude times at most one unit of deterioration) without any run. Its first wording was set before the record tool had run; its two sentences on the Clerk's sums were then made exact once the tool had met two pages whose own Total rows differ from their states' rows in a column of neither major party - the record's band known by then, the model's figure not - and the change touches nothing the band or the decision reads. The rule is the fenced block between the two markers below; everything outside the markers is not part of it. The instrument (`UsMidtermCheck`, in the cheap and documents bars, writing its own GENERATED block on the US card) holds the list of the block's SHA-256 digests, computed and enforced as R-US18's are: the preregistered one, then one per amendment naming Elias's ruling.

<!-- R-US20 RULE BEGIN -->
```text
R-US20'S RULE - US-13's measurement decides Code's recommendation on the midterm.
Preregistered before UsMidtermCheck's first run (US-13); amended only with Elias's ruling.
Amended under Elias's ruling I4 (2026-10-18): unified or divided over both chambers (the
both-chambers amendment asked with US-15); the model's seat change after R-US18's ruling I1.

THE TERMS
- An election is a House general election. The president's party at an election is the
  party of the winner of the last presidential election before it, by NARA's Electoral
  College results page for that election: a midterm reads the presidential election two
  years before it, a presidential year the one four years before it (the sitting
  president's party; Truman, Johnson and Ford each keep their predecessor's party).
- Unified or divided at an election: unified where the president's party holds both
  chambers sitting on its day - the House and the Senate, each the Congress the election
  two years before chose - the reading EconomicVote's presidency branch takes for the
  model where a Senate is seated (US-15); divided otherwise.
  - The House: the president's party is its largest party, by [HH-DIV]'s divisions.
  - The Senate: the president's party is its majority party as the Senate's Party
    Division page ([SEN-DIV]) prints it for that Congress, on the line holding the
    election's day where the page prints the Congress in periods (the 107th); a change
    inside a Congress that only the page's notes record is not read, as [HH-DIV]'s
    divisions are its Congress's initial ones. The printed majority is held to the line's
    own counts, and a line whose counts contradict it fails the tool: an independent
    counts with the party the line says he caucuses with (R-US10 (a)); an equal division
    is the Vice President's party's - his deciding vote (Art. I, sec. 3, cl. 4) gives his
    party the majority, as the page's notes on the 107th and the 117th record - and on an
    election's day that is the sitting president's party, one ticket; where independents
    whose caucus the line does not name could decide it, the page's printed majority is
    read, and the record says so.
  - The model's world: the House's largest party, and the majority of the Senate seated
    on the polling day by UsSenate.MajorityOf (the side with more seats, each independent
    with his conference; an equal division the Vice President's party's, read that day).
- A seat change: the president's party's seats in the Congress the election chose less its
  seats in the Congress before, both as [HH-DIV] prints them - the divisions as of each
  Congress's initial election results, so deaths, specials, vacancies and party switches
  inside a Congress are not in them, and the chamber's size is each row's own.
- A vote swing: the president's party's share of the two-party national House vote at the
  election less its share at the election two years before, in points. A share is the
  party's own ballot line - the Republican and Democratic columns of the Total row of the
  Clerk of the House's "Recapitulation of Votes Cast for United States Representatives"
  (US-5's basis, G1) - those two columns held to the sums of its states' rows exactly (a
  difference the page carries in another column is declared by the tool and printed).
- The record's midterms are the House general elections of 1946 to 2022 held between
  presidential elections, each against the election two years before; the presidential
  years 1948 to 2024 are printed beside them and never read by the rule.

THE RECORD THE RULE READS
- VOTES, NOT SEATS. The model's midterm is a vote swing (US-5's House history, R-US15 (a)).
  A seat change of the model needs R-US18's method, RULED (b), state delegations (Elias's
  ruling I1): the rule never reads a seat figure of the model, and the seat change of
  R-US20 (b)'s fitted term (Elias's ruling I4) is US-27's.
- The record's vote swing is read where the Clerk's statistics of both elections are saved
  as text. The Clerk's statistics of 1998 to 2024 carry a text layer; those of 1944 to
  1996 are page images with none (each probed on 2026-10-16), so the vote swing of every
  midterm from 1946 to 1998 is BILLED - never estimated, never typed from an image, never
  taken from a secondary compilation. The rule's record is the midterms of 2002 to 2022,
  each against the election two years before.
- A year of 1998 to 2024 whose two major-party columns the tool cannot hold to its states'
  rows fails the tool; it is never dropped from the record.
- BOTH GOVERNMENT TYPES TOGETHER. The rule reads every midterm of its record, unified and
  divided alike; the split is printed beside, never deciding (Table 9.1's two cells carry
  the government type into the model already).

THE BAND
- The record's range is the closed interval [L, U]: L the least and U the greatest of the
  rule's record's midterm vote swings, each computed from the vote counts at full
  precision - nothing is rounded before the comparison.

THE MODEL'S MIDTERM
- The world: built as a new US game builds one - the epoch 4 Nov 2024, the eve of 5 Nov
  2024, set before the world is created; seed 777; the player's country the USA and the
  player's party DEM, so that the AI governs the president's party through the term the
  midterm scores (from the oath of 20 Jan 2025); no player decision on any turn; stepped
  one day at a time in the controller's order (the day, the player's day tick, the turn
  at its boundary) to 3 Nov 2026, the 2026 polling day (2 U.S.C. 7), and read on that
  day. It must hold the record (R-US1 (a), US-2): Trump (REP) the president of record
  from the oath, and the House the 2024 election chose from 3 Jan 2025. A world that does
  not hold them fails the instrument; it is not an outcome of this rule.
- The poll: US-5's House history as ruled (R-US15 (a), G2) - the electorate held at the
  four countries' median spread, fitted to the 2024 House split (T-1), the loyalty 2024
  against 2022, in the live chain's order; the identity checked (the poll no farther from
  the prior than the fit's own residual).
- The economic vote: the record over the term on 3 Nov 2026 as the live code reads it
  (from the day the government took office), in its units of deterioration, times Table
  9.1's US legislative cell for the world's government type - unified -0.016 (0.03)
  n = 2, divided 0.012 (0.038) n = 9 [draft] - each with its printed sign, a magnitude
  being the cell's negative as the presidential row's is in code; on the president's
  party alone, delivered exactly. In the instrument only: nothing live.
- The model's midterm, m: the president's party's two-party share in that poll less its
  two-party share at the 2024 House election of record (the Clerk's, US-5's catalog), in
  points, at full precision.

THE DECISION
- m < L or m > U: "By R-US20's rule, code recommends (b): the model's midterm m lies
  outside the record's range [L, U]", with the figures.
- L <= m <= U: "By R-US20's rule, code recommends (a): the model's midterm m lies inside
  the record's range [L, U]", with the figures.
- The verdict line is generated, never typed. R-US20 is asked with the full table
  whatever the outcome.

PRINTED, NEVER DECIDING
- m with no economic vote (the history alone); m with the presidential row (the live
  constants, the cell US-5's House history reads in 2024); m with the legislative cell
  moved by its parenthesis either way (the parenthesis is the table's - by Table 9.2's
  note, a standard deviation of the studies' estimates);
- the record's seat changes, every midterm 1946-2022 and every presidential year
  1948-2024, by government type, each set's least and greatest;
- the record's vote swings by the Clerk, every midterm and presidential year it reads, by
  government type, each set's least and greatest;
- a secondary compilation (Vital Statistics on Congress, Table 2-2: each party's
  percentage of all votes, to one decimal, 1946-2024), labelled SECONDARY: its two-party
  swings by government type, and its differences from the Clerk where both read a year;
- the model's seat change: not computed by the instrument - R-US18 RULED (b) (I1) and
  R-US20 RULED (b) (I4); the seat change of the fitted term, by (b), is US-27's.

THE ASK AND THE BARS
- UsMidtermCheck holds the list of this block's SHA-256 digests - the preregistered one,
  then one per amendment naming Elias's ruling; the block must match the list's last, and
  a change without a ruling fails.
- The ruling record is held by the instrument: NONE; ASKED, with the ask's section and the
  registered outcome - the verdict, the government type, and L, U and m to two decimals;
  or RULED, with the ruling, its option ((a), (b) or (c)) and the outcome it was made on.
  NONE fails the bars until the first run's ask is registered.
- ASKED or RULED: the bars pass while the generated outcome equals the registered one. A
  moved figure with the same verdict fails until a commit re-registers it, naming its
  cause and "verdict unchanged"; a changed verdict fails until R-US20 is asked again with
  the new table, and Elias is told.
- The rule's record changes only by amendment with Elias's ruling - a vote record read
  further back (the Clerk's 1944-1996 statistics, or a source ruled acceptable) - never
  silently. The 2026 midterm's own result is BILLED until the Clerk's 2026 statistics are
  saved (expected 2027); then it is printed beside m, never read by this rule.
```
<!-- R-US20 RULE END -->

**Asked §840, with the measurement** (`docs/reference/US_ELECTIONS.md`, *The midterm - R-US20*; the figures are the card's, the record's `COMPLETED.md` §840): by the rule, Code recommends **(a)** - the model's midterm m = -0.21 (the legislative cell -0.016 on a unified government, 0.132 units of deterioration over the term) lies inside the band [-8.97, +2.26]. **The rule's reach, stated with the ask:** the poll is fitted to the 2024 split m is measured from, so |m| can never exceed 1.60 points under Table 9.1's cells, and the band's nearer edge is 2.26 - no world that holds the record can give (b) under this rule; five of the record's six midterms since 2002 cost the president's party more than 2.9 points. (b) needs a data choice first: the per-term vote exists only for 2002-2022 - before 1998, the Clerk's page images transcribed under a ruling, Table 2-2 ruled acceptable, a fit on the six midterms, or a fit on seats (which needs R-US18). (c) none fetched, BILLED.

**Asked §849 (US-15), R-US20's term:** the block's "unified ... the president's party is the largest in the House ... the reading EconomicVote's presidency branch takes" no longer describes the live code exactly - since US-15, EconomicVote reads both chambers where a Senate is seated. Amend the block to read both chambers? Under a both-chamber term the record's 2002 midterm moves from unified to divided in the printed-only split ([SEN-DIV]: the Democrats held the Senate from 6 Jun 2001 to 12 Nov 2002); L, U and m do not move. The block changes only with Elias's ruling.

**RULED (Elias's ruling I4, 2026-10-18; installed `COMPLETED.md` §856; verbatim in `docs/RULINGS_2026-10-18.md`):** **(b), a FITTED midterm term on the president's party from the record since 1946, with n and its interval printed.** The rule can never reach (b) (its reach, above), so (a) is untested, and the model's m misses the record's midterm regularity. **Revisit it against (c) once approval reaches the vote** (A-LOOP). **The both-chambers amendment (§849): yes.** **Built (US-13b): `COMPLETED.md` §863** - the block amended under I4 (digest `6cf56a5d...`): unified over both chambers sitting on the day, [SEN-DIV]'s Senate parsed by `Tools/us_midterm_record.pl`. The printed split moves 2002 (midterm) to divided; L, U, m and the verdict are unchanged. The instrument holds RULED (b) (`UsMidtermCheck`). US-27 builds the fitted term and its seat change by R-US18 (b).


**R-US21 — B7's open details** *(US-14's dates; blocks US-24)*.
- (i) **The bridge:** debt subject to limit = the model's debt × the ratio read at the seed's vintage *(recommended)*; or re-seed US debt on that perimeter, a family for every start.
- (ii) **A 2024-start game:** the law in force at the start — suspended to 1 Jan 2025, reinstated on 2 Jan at the game's own debt subject to limit (P.L. 118-5 §401(b)), with later raises the game's Congress's acts *(recommended)*; or the record's dated figures (P.L. 119-21's +$5.0 trillion on 4 Jul 2025) whatever the game's Congress does. A start after 4 Jul 2025 holds $41,103,995,660,231 as of record either way.
- (iii) **Extraordinary measures:** one DECLARED span read from the record's last episode (Treasury's letters), inside the CRS's *"a few weeks to several months"* *(recommended)*.
- (iv) **The cut to revenue:** pro rata across non-interest outlays *(recommended)*; or in a statute's order.
- (v) **The AI ministry's ratio-trend "debt-limit logic"** and its borrowed sequester size (`AiFinanceMinistry.UsSequesterPercentOfGdp`): replaced by the dollar limit for the USA *(recommended)*; or kept beside it.
- (vi) **An AI-governed USA at the limit:** it raises or suspends the limit, as every episode of record ended (to be read) *(recommended)*; or it takes the game's cut.

**RULED (Elias's ruling I5, 2026-10-18; installed `COMPLETED.md` §856):** **(i)-(vi) as recommended:**
- (i) the ratio bridge;
- (ii) the law in force at the start, later raises being the game's Congress's acts;
- (iii) one DECLARED span from the record's last episode;
- (iv) the cut pro rata across non-interest outlays;
- (v) the dollar limit replaces the AI ministry's ratio-trend logic and its borrowed sequester for the USA;
- (vi) an AI-governed USA raises or suspends the limit.

US-24 is unblocked. The new question under (ii), the 2022 and 2023 starts opening over P.L. 117-73's limit, stays open under I16.

**RULED (Elias's ruling J3, 2026-10-19; installed `COMPLETED.md` §870): (ii)'s new question.** A start in 2022 or 2023 seats the chamber of record for the start's date where its table is on disk (the 117th or the 118th), and the latest table only where it is missing. The debt limit is the law in force that day. This replaces the latest-table reading `WorldClock.SeatingDeviation` states. The build is US-2c.


**R-US22 — Who governs when a contest is still undecided at the oath** *(US-7's reading 3, §845; reachable once the game elects the House, US-16)*. A contingent election the House cannot decide (no candidate at 26 delegations) leaves the contest undecided on 20 January. R-US3 (a) left "a House that cannot choose" unmodelled without saying who governs then.
- (a) The sitting president stays, possibly to the next term's oath (the code as built; `PresidencyDiagnostic` (f) pins it until ruled);
- (b) the record's president for the date;
- (c) the Speaker acts under 3 U.S.C. §19 (the House is modelled; the Senate's choice of a Vice President, the Twentieth Amendment §3, is not).

No recommendation: each option names a different premise, and none is reachable before US-16.

**RULED (Elias's ruling I6, 2026-10-18; installed `COMPLETED.md` §856): the Twentieth Amendment's order.**
- The Senate chooses the Vice President by a majority of the whole Senate (12th Amendment), and the Vice President acts as President until the House chooses.
- If the Senate cannot choose either, the Speaker acts (3 U.S.C. §19).
- Never the sitting president past noon on 20 January (Am. XX §1).

Option (a) is retired. `PresidencyDiagnostic` (f)'s pin of it moves with the build, which is reachable once the game elects the House, and is built with US-16.


**R-US23 — How the 2024 nominees are named** *(US-6's naming asymmetry with US-8's reading 5, §847)*. Harris is named from the DNC's date (6 Aug 2024); Trump is "the Republican nominee" on every screen before the count, the RNC's date BILLED (gop.com 403); from the count on, the game's own count names both by NARA's names (the contest, the oath's government, the Parliament row: "ELECT TRUMP" from 6 Nov 2024).
- (a) As built: unnamed before the count, NARA's name from the count - the plan's own stand-in;
- (b) never named, the count's record included (the oath's government reads "the Republican nominee (REP)");
- (c) named from a dated source fetched by browser (an errand of Elias's, as E-47).

**RULED (Elias's ruling I7, 2026-10-18; installed `COMPLETED.md` §856): even-handed.** **Built (US-8b): `COMPLETED.md` §866** - neither nominee is named before the later convention's date (`PresidencyOfRecord.TryNamedFrom`); the RNC's date, 15 July 2024, is read from its own press release as the Internet Archive captured it (gop.com refuses this machine), so both are named from the DNC's 6 August.
- Neither nominee is named before the count until both are dated; then both are named from their conventions' records.
- Elias saves the RNC's page for the 15 July 2024 roll call by browser, as for E-47 (`ERRANDS.md`).
- Until that page is read, Harris too is unnamed before the count (US-8b).


**R-US24 — The state prior of the game's second presidential count** *(US-8's reading 8, §847; US-26's)*. A 2028 count swings its states from:
- (a) the record's 2024 states, until US-26 (as built);
- (b) the game's own 2024 states, re-derived exactly from the stored contest (`UsPresidentialElection.StatesOf`), with no save step.

And does US-26 own the choice? The 2024 count is unaffected either way. If (b), US-26's done-when gains: "a US game's second presidential count swings its states from the game's own previous count, its states printed beside the record's 2024 map".

**RULED (Elias's ruling I8, 2026-10-18; installed `COMPLETED.md` §856):** **(b), the game's own 2024 states, and US-26 owns it** (R-US11 (a)). US-26's done-when gains the line above.


**R-US25 — A fork's own count** *(US-8's reading 10, §847; beside R-US22)*. The impact ledger's and the shadow's forks of a loaded US game step through polling days and hold their own counts, as they already hold Poland's presidential rounds and US-2's record; a fork can therefore seat a different president, and the ledger's leave-one-out attribution then includes that change of government.
- (a) As built: forks hold their own counts;
- (b) forks carry the live game's contest forward and hold no count of their own.

With US-28 (§848): a fork made before the player's pick names the default in its own later count while the real game names the pick - shares and electors identical, the name read only for display and within one world.

**RULED (Elias's ruling I9, 2026-10-18; installed `COMPLETED.md` §856; built §866: `SimulationRandom.ElectionNoiseOn`, a keyed source every fork shares, with the noise's size 0 in play until q2):** **(a), with every fork drawing the live game's election noise (common random numbers)**, so a fork elects a different president only because of policy (US-8b).


**R-US26 — The out-party's nominee** *(US-28's reading 4, §848)*. `POLITICAL_SYSTEM_SPEC.md` §6: "**Losing does not end the run.** A defeated president may run again four years later." As built, a party out of the White House fields its candidate unnamed, and the pick exists only for a term-limited party: Harris elected in 2024 and beaten in 2028 is eligible in 2032 (one election), yet DEM fields "Democratic Party's candidate (2032)" and the player cannot field her.
- (a) the player's out-party may pick from its people, the defeated president among them, the default unnamed;
- (b) the out-party fields its last eligible nominee;
- (c) keep it unnamed and amend §6.

**RULED (Elias's ruling I10, 2026-10-18; installed `COMPLETED.md` §856; built §866: `Nominee.OutParty`, the Parliament row's pick):** **(a).** The player's out-party may pick from its people, the defeated president among them, with the default unnamed (US-28b).


**R-US27 — The running mates** *(US-28's reading 3, §848)*.
- (i) **The 2024 game ticket's running mate:** DECLARED as the record's (Vance, Walz); R-US2 (a) names only the presidential nominee. The alternative reads the plan's "a game-elected ticket's running mate is the party's own" literally: after a game Trump win, REP's 2028 default would be "Republican Party's candidate for Vice President (2024)", not Vance.
- (ii) **A re-elected president's ticket** gets a new unnamed running mate (Harris re-elected in 2028: from 20 Jan 2029 the Vice President is "Democratic Party's candidate for Vice President (2028)", and DEM's 2032 default is that mate, Walz an alternative). The alternative: keep the sitting Vice President on a re-elected ticket.

**RULED (Elias's ruling I11, 2026-10-18; installed `COMPLETED.md` §856; built §866: `UsTermLimit.KeepsSittingVicePresident`):**
- (i) as declared: the 2024 game ticket's running mates are the record's (Vance, Walz);
- (ii) the alternative: a re-elected president keeps the sitting Vice President, as the record does (US-28b).


---

## 5. The Design asks

**D-US part one — the night** (US-17, once the three contests are counted; standard form, `uploads/D-US_ask/`; asked against the built count, not against a night — as D-DE and D-PL were). A US night's three contests are asked together, by E3's principle; whether one board carries them is question 8. The questions:

1. **The Electoral College map's form.** A tile cartogram of the 51 jurisdictions with Maine's and Nebraska's districts as cells of their own: equal tiles, or tiles sized by electors (24b sized the Länder by electorate; a per-state electorate is BILLED if Design wants it). No US state geometry exists and `MapRenderer` holds one US centroid, so the map is the night's own tile table on `LaenderTileView`'s pattern, not the world map.
2. **What a tile carries at rest:** the leader and its margin, the swing since 2020, or the electors split.
3. **Where the 270 line, the running count and the record's 312–226 sit.**
4. **Where the House (435, 218 to control) and the Senate class sit:** the races up, the new total, the Vice President at an equal division.
5. **The minor candidates' ink** (Stein, Kennedy, Oliver) — a question, not an asset request.
6. **The president-elect until noon on 20 January.**
7. **How a 269–269 split and the House's vote by state delegation read.**
8. **Whether one board carries the three contests, and whether a midterm night reuses it** without the race to 270. Code recommends one board, Congress's half shown alone at a midterm.

Attachments: the state table generated by `Tools/us_states_table.pl` (electors, the 2020 and 2024 margins, the ME/NE districts); the German night as built, as REAL frames, for its grammar (and D-PL's boards if answered by then); the US Parliament tab. Design is told that this is the Electoral College's night, not D-PR's run-off.

**D-US part two — Congress, the veto, the limit** (US-25, against US-15 to US-24 built):

1. **The Congress view:** both chambers, the majorities, the Senate by class, the independents with their caucus, the cloture line at three fifths of the senators sworn, the override lines at two thirds of each House, the Vice President's vote, the leaders of record.
2. **A bill's path** House → Senate → president → override on the signing plate, the pocket veto stated as not modelled, the ten days the Docket's (US-21b).
3. **The president's desk:** a hostile Congress's reconciliation awaiting signature or veto.
4. **The debt-limit slip:** the dollar limit, the debt subject to it, the measures' days left, Congress's raise or the cut to revenue.
5. **The role words:** PRESIDENT, PRESIDENT-ELECT, THE PRESIDENT'S PARTY, HOUSE MAJORITY, SENATE MAJORITY, MINORITY.
6. **Divided government's mark,** and the lame duck between the night and 20 January.
7. **A lapse's continuing-resolution line.**
8. **Whether the presidential start card shows the Senate beside the House** — §627 ruled the House bar before a Senate existed.

**Later, each against its built part:** Campaign HQ on the states (if US-30 and US-31 outgrow the built HQ); SP-6a's own-nominee step inside D-CP's creation flow (US-33); a third-party run's ballot-access tasks only if R-US4 keeps that form.

**Not asked:** REP and DEM marks and inks exist; `mark_party_us_lib` stays held unless a third-party run is ruled in. Nothing before checkpoint 1 waits on Design: every US surface is built as it stands and asked after, the D19 way. A night or a board is built only from boards stamped LOOKED AT (§707).

---

## 6. The sourcing list (primary sources)

Host status is the 2026-10-04 probe's (by status code); *not probed* means unknown. Large raw sets go out of tree under `PoliSim-captures/sources/` with extracts in the repo; everything with SHA256SUMS.

| for | primary source | status |
|---|---|---|
| US-3 | FEC, *Official 2024 Presidential General Election Results* (2024presgeresults.xlsx; no party labels — matched to NARA's certificates) | 200 (the *Federal Elections 2024* page is 403) |
| US-3 | FEC, *Federal Elections* 2016 and 2020 (xlsx), president by state | 200 |
| US-3 | FEC, *Federal Elections 2012* | saved (§786; the xls in `raw/returns/`, the PDF out of tree) |
| US-3 | Maine Secretary of State tabulations and Nebraska's Canvass Book by congressional district, 2012–2024; Me. Rev. Stat. tit. 21-A §§801–802 and §723-A, Neb. Rev. Stat. §§32-710 and 32-1038 as pages | nebraskalegislature.gov refused — the Secretary of State's canvass or Wayback; else BILLED |
| US-3, US-28 | NARA, Electoral College results 2012 and 2016 (2020 and 2024 on disk); NARA's allocation page | archives.gov serves |
| US-3, R-US13 | Census Bureau, 2010 and 2020 apportionment tables; 2 U.S.C. 2a and 2c; 13 U.S.C. 141(b); 3 U.S.C. 3 (GPO, 2024 edition) | 200 (2020 table and GPO) |
| US-3, US-8 | 3 U.S.C. as rewritten by the Electoral Count Reform Act of 2022 (Pub. L. 117-328, div. P): §§1, 5, 7, 15, 21 (GPO, 2024 edition) | 200 |
| US-3 – US-19 | On disk, to extract with the quote check (no fetch): the 12th, 17th, 23rd, 25th Amendments; Art. I §3 cl. 4, Art. I §7 cl. 1, Art. I §9 cl. 7, Art. II §1 cl. 2 and 5, Art. II §2 cl. 2 | on disk |
| R-US2, US-6 | DNC releases on the 2024 nomination; President Biden's withdrawal statement (bidenwhitehouse.archives.gov); the RNC's convention record; the FEC's ballot listings per state | democrats.org and the archive serve; gop.com refused (G1) — BILLED, NARA's certificates naming the candidates |
| US-5 (the House series, every year - its design), US-11 (the districts) | The Clerk of the House, *Statistics of the (Presidential and) Congressional Election*, 2016–2024 (PDF; text by `pdftotext -raw`) | saved, 2016–2024 (§786, §788; `ElectionsData/usa/raw/returns/`) |
| US-5 (the cross-check), US-11 (the cross-check of the winners, the ranked-choice counts and Alaska's 2022 rounds), US-12 | FEC, *Federal Elections* 2016–2022 (xlsx) | saved (2016 and 2020 §786, 2018 and 2022 §788; the PDFs out of tree) |
| US-11 | The maps in force each cycle — the Census Bureau's Redistricting Data Program tabs (115th–120th Congresses) and its geography page, which name the states whose lines changed for each election; the instruments that serve for 2020 and 2024 | saved (§790; `ElectionsData/usa/raw/maps/`, `house_districts.md` reading 8) — Georgia's 2024 act and North Carolina's S.L. 2023-145 refused (403/401, the Internet Archive 429), Alabama's order day on PACER: BILLED |
| US-12 | The 118th and 119th Senates by seat and date — GPO's Official Congressional Directory (govinfo), or dated Wayback captures of senate.gov; each independent's caucus | saved (§792; `ElectionsData/usa/raw/senate/`, `senate_record.md`'s register): the Directory of 25 Apr 2024 and of 1 Oct 2025 from govinfo; senate.gov's lists, its 50 state pages and the Senate Democrats' member list by the Internet Archive's timegate (senate.gov 403 directly) |
| US-12, US-16 | 2024 Class I returns and specials; 2018's (FEC; the Clerk's statistics2024.pdf); the 2026 specials (G6) | the Clerk's 2018 and 2024 statistics and the FEC's 2018 workbook saved; the FEC's 2024 volume 403 (`raw/returns/fetch_log.txt`), so 2024 rests on the Clerk; G6 BILLED |
| US-10 | House Historian, *Presidential Vetoes*; the Clerk's roll-call XML; the Senate's roll calls via govinfo's Congressional Record or Wayback; GovInfo's public laws of the 118th Congress; 5 U.S.C. 801–802 (GPO); an interpretive source on the override's base (a CRS report via congress.gov's crs_external_products path) | 200 / serves; senate.gov 403 — BILLED by name where nothing serves |
| US-13 | NARA's results pages for each president's party since 1944 (`[HH-DIV]` on disk) | saved (§840, `raw/midterms/`): NARA 1944-2008, the Clerk 1998-2014 (2014 from history.house.gov; 2016-2024 already in `raw/returns`); the Clerk's 1944-1996 statistics page images - BILLED; Vital Statistics Table 2-2 SECONDARY |
| US-14, US-22 | GPO 2024 edition: 31 U.S.C. 1102, 1105(a), 1341–1342; 2 U.S.C. 631, 632, 681 ff.; Congressional Budget Act §904(c)–(d) (2 U.S.C. 621 note). On disk: 2 U.S.C. 641(a), (e), (g) and 644(b)(1)(F) | 200 |
| US-14, US-24 | Treasury MSPD tables 1–3 at the seed's vintage and on 2 Jan 2025 (table 2 on disk); Treasury's letters on the 2025 extraordinary measures; a source and perimeter for the US debt seed (none today; PN-4) | api.fiscaldata.treasury.gov serves; home.treasury.gov not probed |
| US-32, US-33 | GPS 2019 datafile and codebook (Harvard Dataverse, doi:10.7910/DVN/WMGTNS) | 200; CHES-USA still "Coming soon" |
| US-19, R-US8 (b) | 19 U.S.C. 1862 and 2411; 50 U.S.C. 1701–1702 (GPO); the IEEPA litigation's state by date | GPO serves; the litigation not probed |
| US-29 | 12 U.S.C. 242 (GPO); the 2013 and 2017 nomination-cloture precedents (G9) via the Congressional Record or Wayback | GPO serves; senate.gov 403 |
| US-30 | A per-state electorate (EAC EAVS 2024, or the Census Bureau's citizen voting-age population); a salience wave dated before the campaign; FEC campaign finance for a war chest | not probed — BILLED until read |

**BILLED with dates — cannot be read yet:** the 2026 midterm results (certified after 3 Nov 2026; the Clerk's 2026 statistics expected in 2027) and the 2026 specials (G6); the 2030 apportionment (by 31 Dec 2030, to Congress in January 2031); Nebraska's LB3, re-checked from January 2027 (R-EL12's expiry: the 110th Legislature); each post-2024 redistricting, as a dated variant once its enacted map is read (the Census Bureau's 120th tab names the states that redrew for 2026, Missouri's plan in doubt after a court ruling of 3 September 2026 - `house_maps.csv`, §790); R-K9's re-verification of every US SOURCED file (G12); a live senate.gov re-fetch (G2) from a network it serves.

**Ruled in by F8 (R-US9's shutdown):** the excepted share of discretionary spending in a lapse, at US-14. **For the later row SP-6b (R-US4 (a)):** ballot access and deadlines in 51 jurisdictions and 26 U.S.C. 9001–9013.

---

## 7. The risks

1. **The state-by-state campaign is national in disguise.** The count reads the campaign's national final shares, and `NationalElection.DeriveRegional` spreads them afterwards; the AI weighs local acts only in the largest audiences; W-D1's per-region count is wired to nothing. Staging 51 states and targeting them by electors would change no elector. US-9 measures it, and R-US16 rules it, before anything is staged.
2. **A two-party fit is underdetermined.** The electorate has more parameters than a two-party split pins, and E1's factors reproduce the 2024 national vote by construction — so the national total proves nothing, and the acceptance rests on the states and on 2020 out of sample.
3. **Uniform swing's state misses.** From memory, 2024's swing was larger in safe states than in the battlegrounds, so the map can be called right while safe states miss by points. The card lists every miss; no layer is added unasked.
4. **Gridlock by construction, and a veto that may never fire.** With each party voting as one, no Senate of record in the window (the 117th to the 119th) gives a party three fifths: contested ordinary bills die at cloture, and only reconciliation and the uncontested pass. From memory, most recent vetoes fell on Congressional Review Act disapprovals passed with defectors, which a bloc model cannot produce. The US risk is zero vetoes — the mirror of E4's 43 of 43 — so US-10 measures it on the record before anything is wired. The sweep is reported, not tuned.
5. **Poland's coupling.** `PresidentialVeto.Applies` keys Poland's statute-act split at simulation, UI and film-driver sites, and E2 is adding readers now. US-20 cuts them loose before the US veto exists, proved by Poland's own instruments.
6. **The midterm loss may not be in the model.** The US congressional economic vote is the weakest in Duch & Stevenson's sample, so a midterm may replay the presidential year. US-13 measures it against the record since 1946. *Measured (§840): under Table 9.1's congressional cells the model's midterm moves at most 1.60 points from the presidential year; the record's swing since 2002 runs from -8.97 to +2.26 (R-US20 asked).*
7. **The term limit and the nominee.** A REP run from 2024 plays a nominee elected in 2016. Under R-US6 as ruled (b) a REP win does not end the run: the player picks the 2028 nominee, the Vice President by default, who campaigns on the outgoing record - so a successor carries a term the player played, which is the ruling's own design.
8. **The specs conflict,** on third parties (R-US4) and on calendars (R-US5); and the standard run-up copied from Sweden opens during the primaries, before the DEM nominee of record exists (R-US2).
9. **Boundaries move.** Read at US-11 (§790): the states whose House lines changed for each election, 2016-2026, are `house_maps.csv`'s, by the Census Bureau's pages - the tool prints them; the 2026 page is revised in place, Missouri's plan in doubt. A House district's number is a label, not a place, and no vote data in the game can carry a House result onto new lines: a district swing has no prior where the map moved. (US-4's presidential districts of Maine and Nebraska are the declared exception: each stands in for its namesake across the change of lines, DECLARED there.) The 2032 electors cannot be read before 2031 (R-US13).
10. **World coherence.** No AI country votes in any run, so a Swedish, German, Polish, Italian or French game holds no US election even after PS-6 (R-US5). Two elections in one game read the same history (W-G1; R-US11).
11. **The debt limit's perimeter and the seed's offset.** The seed's gross debt has no stated perimeter, and the economy is the seed vintage's on a 2024 calendar, so 2025's dated sequence meets a debt level that is not 2025's. Unbridged, the limit binds on the wrong timetable. B7 certainly moves the sentinel, because it reaches the AI USA that the trajectory dump runs — as US-29's Fed chair would under R-US12 (a); each is its own family.
12. **A second chamber in a one-chamber codebase.** The seats, divisions, verdicts, formation, save and preview clone each hold one chamber. US-15 builds it generic for Italy's Senato, and Sweden, Germany and Poland stay byte-inert by the dump diff.
13. **The default country is the USA.** The controller falls back to it before any pick, and the film driver defaults to it. Every US change moves the most-filmed dry session's frames, and once the USA has a polling day a default world can hold one — measured at US-8.
14. **Sourcing reach.**
    - senate.gov, congress.gov's pages, gop.com and nebraskalegislature.gov refuse this machine; the FEC's 2024 page is 403, though its workbook serves; Wayback rate-limits.
    - There is no PDF renderer: the Clerk's PDFs are read through `pdftotext` (xpdf), their fonts Type1 (US-11); for a PDF not yet read the old caution stands - CID fonts have come out as glyph codes before.
    - The Senate's roll calls and dated roster may only be partly readable, so some rows will be BILLED, and the plan survives that.
15. **Data that cannot exist yet:** the 2026 results until certified; the 2026 specials; the 2032 allocation; LB3 after January 2027. Each is billed with its date, and no item waits on one.
16. **First uses.** §709's US pre-start record has never been judged; the Electoral College allocator has never run in play; W-D1's regional noise is untried live; the stance model's party-bloc premise, written for parliaments, is now read on Congress.
17. **A played game is not the record.** A game from 12 Mar 2024 reaches 5 Nov on its own economy and its own seed vintage, so its count is not the record's. RULED for Poland by F4 and applied here: a fit is held on the world that follows history, and a played game's count moves with its play.
18. **Thin branches.** A player out of the White House holding neither chamber introduces nothing until the next election. That is stated, not padded.
19. **The asymmetry, corrected.** AI-governed foreign countries write their book without a bill. The player's own country's AI government tables its budget as the government's bill (§632), so a US game's AI president's budget meets Congress (US-22); an AI USA in another country's game does not.
20. **Save churn.** The record transitions, the contest, the Senate and the nominee each step the save format. `SaveMigrationCheck` and `PreviewParityDiagnostic` catch a field only when it is added to both the save and the clone's hand-list, and the play saves must be re-staged.
21. **Films and the machine.** Three Canvas surfaces owe REAL films at both widths; only one Unity job runs at a time; the 2560 out-of-memory hit once after another Editor job.
22. **Scale and contention.** 37 records, at least ten of them reviewed — PS-5's reviews found five defects at §761 and five over two readings at §768. Each owes a simulation bar of about ten minutes. D-PL's night and SP-7 run beside this stage and share its files.
23. **Not modelled, each stated where it would show:** faithless electors, Maine's ranked-choice count for electors, NPVIC, the Senate's contingent choice of the Vice President, the pocket veto (the ten days are modelled: US-21b's Docket), the Byrd rule beyond the pension age, impoundment, impeachment, and delegated tariff powers (unless R-US8 (b)).
24. **The claim convention.** The repo copy carries references, never transcribed line numbers or code counts. The feature-list rows name no member that does not exist yet, or `DocumentClaimCheck` fails them.

---

## 8. Out of scope

- **A third-party run** (SP-6b), unless R-US4 (c).
- **US elections in other countries' games,** and record transitions for AI countries: the cross-country row of R-US5 (a).
- **The US midterms as a start point:** START_POINTS §1.1's *"later option, not planned here"*.
- **Primaries and conventions as play,** and the 2024 nominee change replayed (unless R-US2 (c)).
- **State politics:** governors, state legislatures, state budgets, and state minimum wages (29 U.S.C. 218(a) preserves them; not modelled, §756).
- **Electoral edge cases:** faithless electors, NPVIC, Maine's ranked-choice count for electors, and the Senate's contingent election of the Vice President.
- **Budget mechanics beyond R-US9:** the pocket veto (the ten days: US-21b); the Byrd rule beyond the pension age; impoundment and rescissions. (Shutdowns are in: R-US9 as ruled.)
- **Removal and succession:** impeachment and the 25th Amendment.
- **Other branches and appointments:** the judiciary, and every Senate confirmation but the Fed chair's (US-29).
- **Executive orders and regulation;** the Congressional Review Act appears only in US-10's backtest.
- **The president's delegated tariff powers,** unless R-US8 (b).
- **US state geometry on the world map:** the night's tile table only.
- **Re-seeding the US economy for a start:** one seed vintage per country (§621).
- **Senate rule changes as play** (the nominations precedents are read for US-29 only).
- **France's and Italy's stages, and SP-7 and SP-8** — though US-8's count and US-7's oath are built player-agnostic so that SP-7 and SP-8 can reuse the candidate path.
- **Ultrareview as a gate** (§649).
