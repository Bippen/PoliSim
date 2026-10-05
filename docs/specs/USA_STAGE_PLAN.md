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
| Roster | REP and DEM on GPS 2019's economic and social scales (CHES-USA is unpublished); people-versus-elite `[AUTHORED-DRAFT]`; no general left–right, EU or anti-elite-salience item; the GPS file itself is not on disk | the USA's roster in `Assets/Scripts/Data/PartySystem.cs`; `ElectionsData/positions/party_positions.md` |
| Casts | DEM grassroots, REP professional, by §695's rule (REP's on the drafted people-versus-elite value) | `CampaignCasts.Of` |
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
- **Poland's presidential machinery:** the contest store on the Country and its save; `PresidencyOfRecord` and `TwoRoundElection` (Poland's only); `PresidentialElection` (the day hook, the player's country only); `PresidentialVeto` (the Sejm's constants; `PresidentialVeto.Applies` is Poland's, **and Poland's statute-act split keys on it** — in the simulation, on the Budget page, in the controller, in `ChamberVerdicts.Veto` and in the film driver's staging); E1's eve-built `PresidentialReferenceWorld` (§772); the model card's generated block (`PresidentialVoteBacktest.WriteReadings`); E4's `Tools/veto_b1_backtest.pl` (§775).
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
An Editor instrument derives every jurisdiction and the five ME/NE districts from the previous presidential election at the TRUE national shares of record, by three methods — (i) §689's normalised uniform swing (`RegionalVoteModel.RegionalSharesByUniformSwing`, unchanged), (ii) proportional swing, (iii) plain additive swing (Poland's okręg form) — for 2012→2016, 2016→2020 and 2020→2024, then counts the electors on the catalog. DECLARED: the ME/NE districts swing with their state; minor candidates pooled per state; faithless electors and Maine's ranked-choice count not modelled. Nothing is fitted. It opens `docs/reference/US_ELECTIONS.md`, the US model card, writing its GENERATED block on `PresidentialVoteBacktest.WriteReadings`'s pattern: every state's miss, every state called wrong, the electors.
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

**US-7 — The oath: a presidency, not a formation. Size M.**
`ConfidenceProcedure.RulesOf` gains a presidential rule for the USA in place of Unsourced: a fixed term, no investiture and no confidence vote (Art. II; impeachment not modelled, stated). No formation and no Speaker's round run for the USA — the verdict path bypasses the formation for a presidency — which pays §629's debt of *"a presidency wearing a cabinet's words"*.

A contest's winner becomes president-elect and takes office at noon on 20 January (20th Amendment §1) through US-2's dated transition: a government of the presidency kind, its cabinet the president's party, Gamson's allocation not applied. The outgoing president governs until then; the Fed-chair appointment passes with the governing party, as built. The Desk chip and the role gate's reasons read PRESIDENT and PRESIDENT-ELECT — words only; D-US composes them. Tested on planted outcomes; US-8 supplies the real one. Its code lives in files a game path already reaches (`UnwiredSubsystemCheck` admits no game file nothing reaches, §646).
*Needs:* US-2, US-6.
*Done when:* a presidency diagnostic forces each outcome and steps to 21 Jan 2025 — each world seats its winner on the oath's date, and a DEM-player world whose candidate wins reads PRESIDENT; `PlayerRoleDiagnostic` and `ConfidenceDiagnostic` carry the US rows; `FormationSweepDiagnostic` re-pinned if its sweep reaches the USA; a save straddling the oath loads into the game; review row; bars; one filmed width shows the chip after the oath.

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
- `PollingDayDiagnostic`'s USA assertion inverted, as §697 did for Germany; `StartBriefDiagnostic` and US-1's words now name a polling day that is held; `PreStartRecordDiagnostic`'s US row judged; `UnwiredSubsystemCheck`'s unreachable ceiling lowered to what the bar measures, the allocator now wired; `SaveMigrationCheck` and `PreviewParityDiagnostic` green; `elec<N>` diffed;
- the default-USA paths measured — the controller's pre-pick fallback to the USA, the film driver's USA default, the warm-up's `HoldElectionWithoutTheNight` — so no default world holds a US election by accident;
- `Reviews/` report and review row before the bar (SimulationManager); cheap and simulation bars; the USA dry film; a REAL film of the start card.

### Phase D — the other proofs: Congress, the campaign, the limit

Each asks its ruling with its table. Their sourcing runs ahead from US-0; their order among themselves is free within their needs.

**US-9 — The campaign-geography proof. Size M.** *(asks R-US16)*
In an Editor harness only, stage a US campaign on the 51 jurisdictions — audience 2020's valid votes (US-3), the vote model of US-6, casts by `CampaignCasts.Of` — idle and with each AI personality, and measure three things:
- (a) where the AI's local acts land: its candidate regions are the largest audiences (`CampaignAi.LocalCandidateRegions`, `[AUTHORED-DRAFT]`) — the share in the closest 2020 states against the largest;
- (b) that a local act moves a state's derived share only through the national total: the count reads the campaign's national final shares, and `NationalElection.DeriveRegional` spreads them afterwards — *"a readout of the national result, never an input to it"*;
- (c) how far W-D1's per-region count (`ElectionDay` over the campaign's regional mobilization — pure, wired to nothing) would move one state under a maximal ground game, against the 2024 battleground margins (US-3).

*Needs:* US-3, US-4, US-6.
*Done when:* the three measurements in the record; R-US16 asked with them; nothing live; cheap bar.

**US-10 — The veto and the filibuster on the record (E4's form, before any wiring). Size L.** *(asks R-US17)*
Read, raw out of tree with extracts in the repo:
- the House Historian's *Presidential Vetoes* for 2017–2026 — Trump's first term, Biden, Trump's second to date — with every override;
- each vetoed measure's kind: a bill, a joint resolution, or a Congressional Review Act disapproval, whose Senate path is 5 U.S.C. 802's (read; from memory it needs no cloture);
- each one's passage and override roll calls — the Clerk's XML for the House; for the Senate, govinfo's Congressional Record or a Wayback capture, BILLED by name where neither serves;
- the final passage of every public law of the 118th Congress (the divided one, for the precision), others as read;
- an interpretive source for the override's base — two thirds of those present and voting, a quorum present — such as a CRS report through congress.gov's crs_external_products path.

A committed `Tools/us_veto_backtest.pl` writes `docs/generated/US_VETO_BACKTEST.md` in `VETO_B1_BACKTEST.md`'s form: per president, each candidate rule's base (F3's at-risk set transposed; either chamber; both chambers), the vetoes inside and outside it and the rate a draw would take - and B1's confusion matrix, hit rate and precision beside it. It also counts how many vetoed measures of record a party-bloc Congress under Rule XXII could have sent to the president at all.
*Needs:* US-0.
*Done when:* the document is current, and a check fails the cheap bar when it goes stale; the tool's own check passes (party sums from member votes equal each roll call's totals; each override's requirement reproduces two thirds of those present and voting); the rates go into the reply to Elias before anything goes live (E4); a rule that names far more bills than the record vetoed — or a bloc Congress that sends none — is STOPPED and re-asked.

**US-11 — The House count proof. Size L.** *(asks R-US18)*
The Clerk's *Statistics* PDFs for 2018–2024 (2022's and 2024's probed 200), the FEC's *Federal Elections* xlsx as cross-check, inflated by perl (no PDF renderer here; CID fonts without ToUnicode are a known trap). A committed `Tools/us_house_prep.pl` generates the 435 districts per cycle with the exceptions as data: California's and Washington's same-party generals, Maine's and Alaska's ranked-choice final rounds of record, Georgia's majority rule, uncontested seats flagged, and the maps in force each cycle dated, redraws named.

Plurality is then counted per district on the record's own votes, and three seat methods are measured at the true national House vote: (a) district uniform swing on the map in force; (b) state delegations by a state swing; (c) a national seats-votes rule with one fitted parameter. The cycles are 2018→2020, 2020→2022 (the redraw: (b) and (c) only) and 2022→2024, the redrawn states named. Delegations are printed by state, which the 12th Amendment needs.
*Needs:* US-0.
*Done when:* the record's own district votes give the 2022 and 2024 House exactly against `[HH-DIV]` (REP 222 / DEM 213; REP 220 / DEM 215), every exception named; each method's seat error per cycle in the card's GENERATED block; `GeneratedCatalogCheck` re-reads; R-US18 asked.

**US-12 — The Senate by state and by date, and the race proof. Size M.** *(asks R-US19; R-US10 seats the independents)*
- Extract the per-state, per-class table from the three saved class pages (G7; no fetch) with a committed `Tools/us_senate_prep.pl`.
- Date every seat change of the 118th and 119th Senates — appointments, resignations, deaths, re-registrations — from GPO's Congressional Directory on govinfo or dated Wayback captures. From memory, unverified: one senator re-registered from Democrat to independent in mid-2024, which would make the division on 12 March 2024 differ from `[SEN-DIV]`'s undated line.
- Read each independent's caucus from the Senate's own record (the saved page states it for the 117th only), and name the Senate on 12 Mar 2024 and on 3 Jan 2025.
- Read the Class I returns of 2018 and 2024 and the 2024 specials (FEC; the Clerk's statistics2024.pdf), and measure 2024's Class I at the true national vote two ways: (a) the state's presidential-level derived share that day; (b) the seat's 2018 result swung nationally — each counted against the 119th of record.

*Needs:* US-0 (the race half: US-3, US-4).
*Done when:* a roster diagnostic finds 100 seats in three classes by state on both dates, reproducing `[SEN-DIV]`'s division per Congress or naming each dated difference; the caucus sourced or BILLED; the race table in the record; R-US19 asked.

**US-13 — The midterm record, and the model's midterm. Size M.** *(asks R-US20)*
A committed `Tools/us_midterm_record.pl` reads the saved `[HH-DIV]` divisions and NARA's results pages for each president's party, writing `docs/generated/US_MIDTERM_RECORD.md`: the president's party's House seat change at every midterm since 1946, and in presidential years. An Editor instrument runs the model from the eve of 5 Nov 2024 to 3 Nov 2026 on US-5's staging, with the congressional economic-vote figures (`docs/reference/ECONOMIC_VOTE.md`, Table 9.1's US legislative row — sourced, not in code), and prints the swing the model produces.
*Needs:* US-5, US-11.
*Done when:* the record and the model side by side; R-US20 asked.

**US-14 — The debt limit measured before it runs; the budget statutes read. Size M.** *(asks R-US21)*
- **The bridge first** (PN-4: same year, same perimeter): Treasury's MSPD tables at the seed's vintage (table 2 on disk; tables 1 and 3 from api.fiscaldata.treasury.gov) set against the seed's gross federal debt, whose perimeter is unstated (`WorldFactory`); the residual stated.
- **The statute as dated data:** suspended to 1 Jan 2025; reinstated on 2 Jan 2025 by P.L. 118-5 §401(b)'s formula — the record's $36,103,995,660,231 reproduced from the record's own debt; +$5,000,000,000,000 on 4 Jul 2025 (P.L. 119-21 §72001).
- **The dates:** an Editor diagnostic reads the model's debt through the bridge on each start's no-policy path (`NoPolicyCentury.For`) and prints the day the limit is reached and the day it binds under each option of R-US21, beside CRS IF10292's *"early to mid 2027"*. It measures the AI ministry's ratio-trend "debt-limit logic", and its borrowed sequester, against the dollar limit.
- **Also read for US-22:** 31 U.S.C. 1102, 1105(a) and 1341–1342; 2 U.S.C. 631, 632 and 681 ff.; the Budget Act §904(c)–(d), the note to 2 U.S.C. 621 — the Byrd rule's three-fifths waiver, correcting G10's "§644(e)" pointer; and Treasury's letters on the 2025 extraordinary-measures episode, for R-US21's span. **For R-US9 as ruled (F8):** 31 U.S.C. 1341–1342 (the Antideficiency Act); the lapse of record - P.L. 119-4 (the full-year continuing appropriations for the fiscal year ending 30 September 2025) and H.R. 5371's actions to P.L. 119-37 (12 November 2025), both captured 2026-10-05 from govinfo's own pages and held out of tree until this item brings them in with their digests; and the excepted share of discretionary spending in a lapse (OMB's and the agencies' lapse plans), BILLED until read.

*Needs:* US-0.
*Done when:* the dates are printed per option and per start in the cheap bar, nothing live; `debt_limit.md` carries the bridge; R-US21 asked with the dates.

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

**US-16 — The House and a Senate class elected on the same day. Size L.** *(R-US18, R-US19)* — **checkpoint 2**
On the presidential polling day the game also elects the House, by R-US18's method on US-11's districts, and the Senate class whose terms expire, by R-US19's rule (2 U.S.C. §1; the specials where sourced, else BILLED — G6). Both are seated at noon on 3 January. The 12th Amendment (R-US3) reads the House-elect's delegations. The reference shows the record's 220/215 and the 119th Senate of record. "Divided" is read from the game's own chambers. US-2's Congress of record retires for each chamber the game now elects, so under §618's ruling 4 a 2024-start game elects its own 119th Congress.
*Needs:* US-8, US-11, US-12, US-15; R-US18; R-US19.
*Done when:* a Congress-election diagnostic — a fresh world on the eve of 5 Nov 2024 seats a 119th whose misses against the record are written into the card; 435 and 100 sum after every election; a save across the count loads into the game; review row (SimulationManager); bars; the dry film.

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

**US-19 — Roles in a presidential system. Size L.** *(R-US7, R-US8)*
- Per R-US7: the president's party introduces the administration's bills and proposes the budget; a party out of the White House introduces ordinary bills in any chamber it controls, and revenue bills only where it holds the House (Art. I §7 cl. 1, extracted); a party holding neither votes and introduces nothing.
- Per R-US8: the president's levers.
- `SimulationManager.PlayerMayIntroduce` and `GovernmentRecord.RoleOf` read the presidency and the chambers. The Desk chip reads PRESIDENT, THE PRESIDENT'S PARTY, HOUSE MAJORITY, SENATE MAJORITY or MINORITY — words only. No US player reads IN OPPOSITION with nothing to do.

*Needs:* US-7, US-18; R-US7; R-US8 (under (b), the delegations read).
*Done when:* `PlayerRoleDiagnostic`'s US rows re-pinned — on 12 Mar 2024 a DEM player governs as the president's party, and a REP player holds the House majority, may introduce an ordinary bill, and sees it die in the Senate; review row (SimulationManager); bars; the USA dry film; one filmed width of the Desk.

**US-20 — Poland's statute acts keyed on Poland's own rule. Size M.**
Poland's statute-act split (D4 and E2: §768, §773) keys today on `PresidentialVeto.Applies` — in the simulation (the budget act, the statute acts, the veto gate, the Finance partner's rates), on the Budget page (its slip and lines), in the controller, in `ChamberVerdicts.Veto` and in the film driver's staging. Switching it on for the USA would route US budgets through Poland's acts. The readers move onto `WorldClock.BudgetProcedureOf`, which gains Poland's statute-act procedure (and, at US-22, the US Congress's); `PresidentialVeto` holds its rule per country, Poland's unchanged: F3's at-risk test (B1 widened), the keyed draw at the generated rates (`Generated.PolishVetoRates`), B2 and the Sejm constants. Every reader is found by grep at the item, not from this list — E2 is still adding them. May land any time after E2's commit; nothing before US-21 needs it.
*Needs:* E2 committed.
*Done when:* Poland unchanged, proved by its own instruments — `PresidentialVetoDiagnostic`'s cases and the D4 and E2 cases (the no-policy dump meets no budget bill, §768, so the dump diff alone proves nothing here) — and by the dump diff; `GovernmentBudgetBillDiagnostic` re-pinned; Poland's Budget slip unchanged on Poland's dry film; review rows (SimulationManager, BudgetBill if touched); bars.

**US-21 — The US veto. Size M.** *(R-US17)*
Art. I §7 as a US rule beside Poland's. Every bill both chambers pass goes to the president of the day — the record's or the game's — who vetoes by R-US17's rule; a player-president chooses. The override needs two thirds of each House on the sourced base, voted by the same sides (Poland's premise, DECLARED — and measured and said: under it no Congress of record overrides). The ten days and the pocket veto are not modelled, stated. The signing plate draws the veto and the override in both chambers (a US frame like 89g, through the film driver's staging), and `ChamberVerdicts.Veto` projects it.
*Needs:* US-10 (its rates already in Elias's hands), US-19, US-20; R-US17.
*Done when:* a US veto diagnostic on US-10's record cases and on planted bills; a run from 12 Mar 2024 to 20 Jan 2025 prints the bills that reached the president and those vetoed, beside the record's count for the 118th; Poland's veto diagnostics unchanged; review row; bars; REAL films of the veto plate at 1280 and 2560.

**US-22 — The US budget procedure. Size L.** *(R-US9)*
`WorldClock.BudgetProcedureOf` gains the US Congress procedure, sourced at US-14:
- the president's budget request is the government's bill (31 U.S.C. 1105(a), its date read), and the fiscal year opens on 1 October (31 U.S.C. 1102, now behind `FiscalYearData`);
- per R-US9 (a): the appropriation (the spending lines) needs cloture; the rates — and direct spending and the debt limit where they move — ride reconciliation at simple majorities (2 U.S.C. 641(a), (e)); the pension age is its own statute at cloture (641(g); 644(b)(1)(F)); E2's statute-act parts are the machinery;
- per R-US9 as ruled (F8: *"if funding lapses with no continuing resolution, the government shuts down ... The game must be able to produce a shutdown."*): a failed appropriation is followed by a continuing resolution - a bill of its own, at cloture, as H.R. 5371 was on the record; where none passes, funding lapses and the government SHUTS DOWN (the Antideficiency Act, read at US-14): non-excepted discretionary spending stops until an appropriation or a continuing resolution passes, the excepted share sourced at US-14, and the shutdown's days are said on screen. Both acts may be vetoed. The Byrd rule is otherwise not modelled, stated;
- where the AI governs (the player is not the president's party), its budget is tabled as the government's bill (§632) and goes through the same procedure.

*Needs:* US-14, US-18, US-20, US-21; R-US9.
*Done when:*
- a US budget diagnostic under the 119th of record: a rates change passes by reconciliation; a contested appropriation dies at cloture and a continuing resolution carries the standing figures; where the continuing resolution dies too, a shutdown opens on 1 October and ends the day a bill passes - the game produces one; a pension-age change is its own act at cloture;
- the US film's budget frame (93b), skipped today as *"THIS COUNTRY'S BUDGET PROCEDURE IS NOT YET MODELLED"* (§694), is filmed;
- Sweden's frame decision and Poland's acts unchanged; `GovernmentBudgetBillDiagnostic` re-pinned for Germany, Poland and the USA;
- `Reviews/` report and rows for every money path touched (BudgetBill, SimulationManager, `FiscalYearData`); bars; the dry film; one filmed width of the US budget slip.

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

**US-33 — Your own nominee (SP-6a). Size L.** *(R-US4)*
The creation flow is offered for the USA as a leader at the head of the DEM or REP ticket, with positions inside the party's range (US-32's spread if read, else DECLARED); the nominee stands at factor 1.0, and the character is the person the term limit reads (US-28). The third-party run is added only if R-US4 keeps it inside PS-6, with its billed rules.
*Needs:* US-28, US-32; R-US4.
*Done when:* a US game starts with its own nominee and plays to the count, which reads that nominee's position through the poll; `StartPointsDiagnostic` and `StartBriefDiagnostic` extended; the flow's US form filmed REAL.

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

**R-US15 — Which vote the US poll is** *(US-5's table; blocks US-6)*.
- (a) The House national vote — the party's own vote, held on every federal polling day, midterms included: the history is the last two House elections, and the presidential candidates carry it by E1's factors;
- (b) the presidential popular vote as the history (2020 against 2016 at the start), midterms then reading the last two presidential votes;
- (c) two histories, one per contest.

The electorate is fitted free where it is identifiable, and otherwise with its spread held at the fitted countries' values (DECLARED). Code expects to recommend (a), unless its national miss is well above (b)'s.

**R-US16 — What "fought state by state" means** *(US-9's measurements; blocks US-31)*.
- (a) National in effect, as Germany's Länder are today, with the HQ saying so;
- (b) for the USA, a per-state count with the ground game on the derived shares, plus a DECLARED targeting rule for the AI (electors × closeness on the last published poll);
- (c) (b) for every staged country — Sweden's and Germany's backtests and films then move, the sentinel not.

Code expects to recommend (b), sized by US-9.

**R-US17 — Which bills the president vetoes, and the override's base** *(US-10's document; blocks US-21)*.
- (a) B1 transposed: a bill a majority of the president's party's House members voted against;
- (b) a bill the president's party opposed in either chamber;
- (c) a rule fitted on the record.

The override is two thirds of those present and voting, a quorum present, as the interpretive source reads it. Code will recommend on the confusion matrix — (a) only if its precision on the US record is at least B1's on Poland's — and STOPS and re-asks if every rule names far more bills than the record vetoed, or if a party-bloc Congress under Rule XXII never sends a vetoable bill. That last is the US risk, the mirror of E4's 43 of 43.

**R-US18 — The House's seat method** *(US-11's errors; blocks US-16)*.
- (a) 435 districts by uniform swing on the latest House election's district results, uncontested seats by a DECLARED rule, and mid-decade redraws entering as dated variants once each enacted map is read;
- (b) state delegations by a state swing;
- (c) a national seats-votes rule fitted on the record (one parameter).

The measurement decides; Code expects (a) where maps held.

**R-US19 — The Senate races** *(US-12's table; blocks US-16)*.
- (a) The state's presidential-level derived share that day (a straight ticket);
- (b) the seat's previous result swung by the national change;
- (c) either, with per-race factors fitted once on 2024's Class I and its specials (E1's principle; later fields at 1.0).

Code will recommend whichever misses fewer 2024 Class I seats. From memory, Democrats won Senate races in several states Trump carried in 2024, which counts against (a); it is measured, not assumed.

**R-US20 — Midterms** *(US-13; blocks US-27)*.
- (a) The model as it stands, with the congressional economic vote wired (Table 9.1's US legislative row, the weakest in the sample);
- (b) a *[FITTED]* midterm term on the president's party, read from the record since 1946 — per-term data, as E1's factors are per-candidate data;
- (c) a sourced literature model.

Code will recommend (b) if the model's midterm falls outside the record's range, and (a) otherwise.

**R-US21 — B7's open details** *(US-14's dates; blocks US-24)*.
- (i) **The bridge:** debt subject to limit = the model's debt × the ratio read at the seed's vintage *(recommended)*; or re-seed US debt on that perimeter, a family for every start.
- (ii) **A 2024-start game:** the law in force at the start — suspended to 1 Jan 2025, reinstated on 2 Jan at the game's own debt subject to limit (P.L. 118-5 §401(b)), with later raises the game's Congress's acts *(recommended)*; or the record's dated figures (P.L. 119-21's +$5.0 trillion on 4 Jul 2025) whatever the game's Congress does. A start after 4 Jul 2025 holds $41,103,995,660,231 as of record either way.
- (iii) **Extraordinary measures:** one DECLARED span read from the record's last episode (Treasury's letters), inside the CRS's *"a few weeks to several months"* *(recommended)*.
- (iv) **The cut to revenue:** pro rata across non-interest outlays *(recommended)*; or in a statute's order.
- (v) **The AI ministry's ratio-trend "debt-limit logic"** and its borrowed sequester size (`AiFinanceMinistry.UsSequesterPercentOfGdp`): replaced by the dollar limit for the USA *(recommended)*; or kept beside it.
- (vi) **An AI-governed USA at the limit:** it raises or suspends the limit, as every episode of record ended (to be read) *(recommended)*; or it takes the game's cut.

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
2. **A bill's path** House → Senate → president → override on the signing plate, the ten days and the pocket veto stated as not modelled.
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
| US-3 | FEC, *Federal Elections 2012* | not probed |
| US-3 | Maine Secretary of State tabulations and Nebraska's Canvass Book by congressional district, 2012–2024; Me. Rev. Stat. tit. 21-A §§801–802 and §723-A, Neb. Rev. Stat. §§32-710 and 32-1038 as pages | nebraskalegislature.gov refused — the Secretary of State's canvass or Wayback; else BILLED |
| US-3, US-28 | NARA, Electoral College results 2012 and 2016 (2020 and 2024 on disk); NARA's allocation page | archives.gov serves |
| US-3, R-US13 | Census Bureau, 2010 and 2020 apportionment tables; 2 U.S.C. 2a and 2c; 13 U.S.C. 141(b); 3 U.S.C. 3 (GPO, 2024 edition) | 200 (2020 table and GPO) |
| US-3, US-8 | 3 U.S.C. as rewritten by the Electoral Count Reform Act of 2022 (Pub. L. 117-328, div. P): §§1, 5, 7, 15, 21 (GPO, 2024 edition) | 200 |
| US-3 – US-19 | On disk, to extract with the quote check (no fetch): the 12th, 17th, 23rd, 25th Amendments; Art. I §3 cl. 4, Art. I §7 cl. 1, Art. I §9 cl. 7, Art. II §1 cl. 2 and 5, Art. II §2 cl. 2 | on disk |
| R-US2, US-6 | DNC releases on the 2024 nomination; President Biden's withdrawal statement (bidenwhitehouse.archives.gov); the RNC's convention record; the FEC's ballot listings per state | democrats.org and the archive serve; gop.com refused (G1) — BILLED, NARA's certificates naming the candidates |
| US-5 (2024's national summary table only), US-11 (the districts) | The Clerk of the House, *Statistics of the Presidential and Congressional Election*, 2016–2024 (PDF; inflated by perl) | 2022 and 2024: 200; 2016–2020: not probed |
| US-5, US-11, US-12 | FEC, *Federal Elections* 2018 and 2022 (xlsx) | 200 |
| US-11 | The maps in force each cycle — each state's enacted map or court order (from memory, unverified: several states redrew for 2026) | not probed |
| US-12 | The 118th and 119th Senates by seat and date — GPO's Official Congressional Directory (govinfo), or dated Wayback captures of senate.gov; each independent's caucus | govinfo serves; senate.gov 403; Wayback answered 429 once |
| US-12, US-16 | 2024 Class I returns and specials; 2018's (FEC; the Clerk's statistics2024.pdf); the 2026 specials (G6) | FEC 200; G6 BILLED |
| US-10 | House Historian, *Presidential Vetoes*; the Clerk's roll-call XML; the Senate's roll calls via govinfo's Congressional Record or Wayback; GovInfo's public laws of the 118th Congress; 5 U.S.C. 801–802 (GPO); an interpretive source on the override's base (a CRS report via congress.gov's crs_external_products path) | 200 / serves; senate.gov 403 — BILLED by name where nothing serves |
| US-13 | NARA's results pages for each president's party since 1944 (`[HH-DIV]` on disk) | archives.gov serves |
| US-14, US-22 | GPO 2024 edition: 31 U.S.C. 1102, 1105(a), 1341–1342; 2 U.S.C. 631, 632, 681 ff.; Congressional Budget Act §904(c)–(d) (2 U.S.C. 621 note). On disk: 2 U.S.C. 641(a), (e), (g) and 644(b)(1)(F) | 200 |
| US-14, US-24 | Treasury MSPD tables 1–3 at the seed's vintage and on 2 Jan 2025 (table 2 on disk); Treasury's letters on the 2025 extraordinary measures; a source and perimeter for the US debt seed (none today; PN-4) | api.fiscaldata.treasury.gov serves; home.treasury.gov not probed |
| US-32, US-33 | GPS 2019 datafile and codebook (Harvard Dataverse, doi:10.7910/DVN/WMGTNS) | 200; CHES-USA still "Coming soon" |
| US-19, R-US8 (b) | 19 U.S.C. 1862 and 2411; 50 U.S.C. 1701–1702 (GPO); the IEEPA litigation's state by date | GPO serves; the litigation not probed |
| US-29 | 12 U.S.C. 242 (GPO); the 2013 and 2017 nomination-cloture precedents (G9) via the Congressional Record or Wayback | GPO serves; senate.gov 403 |
| US-30 | A per-state electorate (EAC EAVS 2024, or the Census Bureau's citizen voting-age population); a salience wave dated before the campaign; FEC campaign finance for a war chest | not probed — BILLED until read |

**BILLED with dates — cannot be read yet:** the 2026 midterm results (certified after 3 Nov 2026; the Clerk's 2026 statistics expected in 2027) and the 2026 specials (G6); the 2030 apportionment (by 31 Dec 2030, to Congress in January 2031); Nebraska's LB3, re-checked from January 2027 (R-EL12's expiry: the 110th Legislature); each post-2024 redistricting, as a dated variant once its enacted map is read; R-K9's re-verification of every US SOURCED file (G12); a live senate.gov re-fetch (G2) from a network it serves.

**Ruled in by F8 (R-US9's shutdown):** the excepted share of discretionary spending in a lapse, at US-14. **For the later row SP-6b (R-US4 (a)):** ballot access and deadlines in 51 jurisdictions and 26 U.S.C. 9001–9013.

---

## 7. The risks

1. **The state-by-state campaign is national in disguise.** The count reads the campaign's national final shares, and `NationalElection.DeriveRegional` spreads them afterwards; the AI weighs local acts only in the largest audiences; W-D1's per-region count is wired to nothing. Staging 51 states and targeting them by electors would change no elector. US-9 measures it, and R-US16 rules it, before anything is staged.
2. **A two-party fit is underdetermined.** The electorate has more parameters than a two-party split pins, and E1's factors reproduce the 2024 national vote by construction — so the national total proves nothing, and the acceptance rests on the states and on 2020 out of sample.
3. **Uniform swing's state misses.** From memory, 2024's swing was larger in safe states than in the battlegrounds, so the map can be called right while safe states miss by points. The card lists every miss; no layer is added unasked.
4. **Gridlock by construction, and a veto that may never fire.** With each party voting as one, no Senate of record in the window (the 117th to the 119th) gives a party three fifths: contested ordinary bills die at cloture, and only reconciliation and the uncontested pass. From memory, most recent vetoes fell on Congressional Review Act disapprovals passed with defectors, which a bloc model cannot produce. The US risk is zero vetoes — the mirror of E4's 43 of 43 — so US-10 measures it on the record before anything is wired. The sweep is reported, not tuned.
5. **Poland's coupling.** `PresidentialVeto.Applies` keys Poland's statute-act split at simulation, UI and film-driver sites, and E2 is adding readers now. US-20 cuts them loose before the US veto exists, proved by Poland's own instruments.
6. **The midterm loss may not be in the model.** The US congressional economic vote is the weakest in Duch & Stevenson's sample, so a midterm may replay the presidential year. US-13 measures it against the record since 1946.
7. **The term limit and the nominee.** A REP run from 2024 plays a nominee elected in 2016. Under R-US6 as ruled (b) a REP win does not end the run: the player picks the 2028 nominee, the Vice President by default, who campaigns on the outgoing record - so a successor carries a term the player played, which is the ruling's own design.
8. **The specs conflict,** on third parties (R-US4) and on calendars (R-US5); and the standard run-up copied from Sweden opens during the primaries, before the DEM nominee of record exists (R-US2).
9. **Boundaries move.** From memory, House lines changed between 2022 and 2024 in several states and again for 2026 (read at US-11): a district swing misses where the map moved. The 2032 electors cannot be read before 2031 (R-US13).
10. **World coherence.** No AI country votes in any run, so a Swedish, German, Polish, Italian or French game holds no US election even after PS-6 (R-US5). Two elections in one game read the same history (W-G1; R-US11).
11. **The debt limit's perimeter and the seed's offset.** The seed's gross debt has no stated perimeter, and the economy is the seed vintage's on a 2024 calendar, so 2025's dated sequence meets a debt level that is not 2025's. Unbridged, the limit binds on the wrong timetable. B7 certainly moves the sentinel, because it reaches the AI USA that the trajectory dump runs — as US-29's Fed chair would under R-US12 (a); each is its own family.
12. **A second chamber in a one-chamber codebase.** The seats, divisions, verdicts, formation, save and preview clone each hold one chamber. US-15 builds it generic for Italy's Senato, and Sweden, Germany and Poland stay byte-inert by the dump diff.
13. **The default country is the USA.** The controller falls back to it before any pick, and the film driver defaults to it. Every US change moves the most-filmed dry session's frames, and once the USA has a polling day a default world can hold one — measured at US-8.
14. **Sourcing reach.**
    - senate.gov, congress.gov's pages, gop.com and nebraskalegislature.gov refuse this machine; the FEC's 2024 page is 403, though its workbook serves; Wayback rate-limits.
    - There is no PDF renderer: the Clerk's PDFs are inflated by perl, and CID fonts have come out as glyph codes before.
    - The Senate's roll calls and dated roster may only be partly readable, so some rows will be BILLED, and the plan survives that.
15. **Data that cannot exist yet:** the 2026 results until certified; the 2026 specials; the 2032 allocation; LB3 after January 2027. Each is billed with its date, and no item waits on one.
16. **First uses.** §709's US pre-start record has never been judged; the Electoral College allocator has never run in play; W-D1's regional noise is untried live; the stance model's party-bloc premise, written for parliaments, is now read on Congress.
17. **A played game is not the record.** A game from 12 Mar 2024 reaches 5 Nov on its own economy and its own seed vintage, so its count is not the record's. RULED for Poland by F4 and applied here: a fit is held on the world that follows history, and a played game's count moves with its play.
18. **Thin branches.** A player out of the White House holding neither chamber introduces nothing until the next election. That is stated, not padded.
19. **The asymmetry, corrected.** AI-governed foreign countries write their book without a bill. The player's own country's AI government tables its budget as the government's bill (§632), so a US game's AI president's budget meets Congress (US-22); an AI USA in another country's game does not.
20. **Save churn.** The record transitions, the contest, the Senate and the nominee each step the save format. `SaveMigrationCheck` and `PreviewParityDiagnostic` catch a field only when it is added to both the save and the clone's hand-list, and the play saves must be re-staged.
21. **Films and the machine.** Three Canvas surfaces owe REAL films at both widths; only one Unity job runs at a time; the 2560 out-of-memory hit once after another Editor job.
22. **Scale and contention.** 37 records, at least ten of them reviewed — PS-5's reviews found five defects at §761 and five over two readings at §768. Each owes a simulation bar of about ten minutes. D-PL's night and SP-7 run beside this stage and share its files.
23. **Not modelled, each stated where it would show:** faithless electors, Maine's ranked-choice count for electors, NPVIC, the Senate's contingent choice of the Vice President, the pocket veto and the ten days, the Byrd rule beyond the pension age, impoundment, impeachment, and delegated tariff powers (unless R-US8 (b)).
24. **The claim convention.** The repo copy carries references, never transcribed line numbers or code counts. The feature-list rows name no member that does not exist yet, or `DocumentClaimCheck` fails them.

---

## 8. Out of scope

- **A third-party run** (SP-6b), unless R-US4 (c).
- **US elections in other countries' games,** and record transitions for AI countries: the cross-country row of R-US5 (a).
- **The US midterms as a start point:** START_POINTS §1.1's *"later option, not planned here"*.
- **Primaries and conventions as play,** and the 2024 nominee change replayed (unless R-US2 (c)).
- **State politics:** governors, state legislatures, state budgets, and state minimum wages (29 U.S.C. 218(a) preserves them; not modelled, §756).
- **Electoral edge cases:** faithless electors, NPVIC, Maine's ranked-choice count for electors, and the Senate's contingent election of the Vice President.
- **Budget mechanics beyond R-US9:** the pocket veto and the ten days; the Byrd rule beyond the pension age; impoundment and rescissions. (Shutdowns are in: R-US9 as ruled.)
- **Removal and succession:** impeachment and the 25th Amendment.
- **Other branches and appointments:** the judiciary, and every Senate confirmation but the Fed chair's (US-29).
- **Executive orders and regulation;** the Congressional Review Act appears only in US-10's backtest.
- **The president's delegated tariff powers,** unless R-US8 (b).
- **US state geometry on the world map:** the night's tile table only.
- **Re-seeding the US economy for a start:** one seed vintage per country (§621).
- **Senate rule changes as play** (the nominations precedents are read for US-29 only).
- **France's and Italy's stages, and SP-7 and SP-8** — though US-8's count and US-7's oath are built player-agnostic so that SP-7 and SP-8 can reuse the candidate path.
- **Ultrareview as a gate** (§649).
