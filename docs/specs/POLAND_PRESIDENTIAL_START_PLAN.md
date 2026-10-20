# PoliSim — SP-7, Poland's 2025 presidential start: the player as a candidate (design + work plan for Code, measured before any code)

**Status: DESIGN, put to Elias 2026-10-16; nothing built; four rulings asked (§5).** Written to the repo before any code, by the standing rule
that a design a later session depends on lives here (`COMPLETED.md` §643). It extends `START_POINTS_AND_PARTY_CREATION_SPEC.md` (§1.1, §3, §4 S7,
§7 decision 11) and `POLITICAL_SYSTEM_SPEC.md` (§3, §4's start rule, §5.2, §7, §10). Every date, rule and figure is sourced before use: where this
plan names a fact it names where it was read. A bare § number is a `COMPLETED.md` record; a spec's own section is named with its document. The
code facts in §2 and §4.2 are **a reading of HEAD `43ef9ff9` on 2026-10-16 (TRACKING: they go stale as SP-7, US-7 and US-8 land)**; this plan
carries pointers, never line numbers or counts of code.

**Built on:** the SP-7 row of `POLISIM_FEATURE_LIST.md`; §618 (the world clock and its start rule), §619 (the election is a date), §720 (the
two-round rule), §761 (the veto live), §770 (the game elects Poland's president), Elias's rulings B4/B5 (§764, §765), E1 (§772), E2 (§773), E3
(§774), F3 (§778), F4 (§779) and F8 (§781, the USA's role words); the sources in `ElectionsData/poland/` (`presidential_returns.md`,
`presidential_nomination.md`, `presidential_calendar_2025.md`, `records_by_date.md`, `veto.md`).

---

## 1. What SP-7 delivers

### 1.1 The row

`POLISIM_FEATURE_LIST.md`, **SP-7** (Code; needs PS-5; done when *"playable as a candidate"*): *"The two-round presidential model; **Poland's 2025
presidential start**; the president's veto and cohabitation"* - the model and the veto built (§720, §761, §770); **the presidential START - the
player as a candidate - not built.**

### 1.2 The specs

`START_POINTS_AND_PARTY_CREATION_SPEC.md`:

- §1.1: *"Where a country has both a presidential and a parliamentary election — Poland, France, the USA — the player chooses which contest to
  fight."* Poland's row: *"Sejm, October 2023 · **presidential, May–June 2025**"*, the player's role *"a party · **a presidential candidate**"*.
- §1.2: *"Select opens the party screen (§2) with the world set to that start's date."*
- §4, S7: *"A national two-round popular vote; the president's role in a parliamentary republic - the veto and its override, and cohabitation with
  the Sejm of record."*
- §7, decision 11: *"The two-round presidential model unlocks Poland's and France's presidential starts; France's legislative start stays locked."*

`POLITICAL_SYSTEM_SPEC.md`:

- §4, the start rule: *"a scheduled election starts at the standard run-up (Sweden's 26-week run-up plus 8-week campaign, applied to the country's
  own polling day); a snap election starts on the day it was triggered"*; §10 decision 2 the same.
- §5.2: the four roles of a parliamentary party; §10 decision 4: *"Losing office never ends a run in a parliamentary country"*.
- §7: *"A run ends when the player's party falls out of parliament, when the player ends it"*.

### 1.3 Done when (the work card)

**Playable as a candidate:** Poland's presidential card opens a world where the player is a 2025 candidate, the two rounds are held on their
dates, and the result is governed by the ruled post-election role. Verified by a diagnostic - a fresh presidential-start world taken through
18 May and 1 June 2025 holds both rounds once with the player's candidate in the field, and a save across the rounds and across the oath loads
into the GAME, not only the manager (§557); `StartPointsDiagnostic` pins the unlocked card; a review row (`SimulationManager.cs`); the cheap and
simulation bars; the dry film; REAL films of the start panel and the President row at 1280 and 2560 (a Canvas surface, §578).

## 2. What the model and the veto already provide

A Polish game today crosses the 2025 election from the Sejm start and holds it itself. Each piece, where it lives:

| piece | where | ruled / recorded |
|---|---|---|
| **The two-round rule as data** - more than half elects; otherwise the two meet on the 14th day; the term five years, two at most; the window 100 to 75 days before the term ends | `TwoRoundElection`; the presidents of record by date in `PresidencyOfRecord` (Duda to 6 Aug 2025, Nawrocki from it) | §720 (Konstytucja Art. 127-128) |
| **The game's own election** - its days (the record's where the record holds one, else the window's last Sunday), held by the day loop for the player's country (`SimulationManager.HoldPresidentialRound` inside `SimulationManager.AdvanceDay`), the winner taking office on the predecessor's term end; the contests saved with the country (`Country.PresidentialElections`) | `PresidentialElection` | §770; the days and the oath RULED by E2 (§773) |
| **Who stands** - in 2025 the whole field of record: five backed by a roster party (Nawrocki PiS, Trzaskowski KO, Mentzen Konf, Hołownia TD, Biejat NL) and eight backed by none; in a later field one candidate per roster party clearing the signatures, a created party's leader included | `PresidencyOfRecord.CandidatesOf`; the gate in `PresidentialElection` | E1 (§772), B5 (§765), E2 (the player's own leader standing) |
| **The first round** - the party's poll × the candidate's factor, [FITTED] once on the reference world (the record's state on the eve); a candidate with no roster party carries its share of record; a later field at `PresidentialElection.FutureFactor` | `PresidentialElection`, `PresidencyOfRecord.CandidateOfRecord` | E1; F4: a played game's first round moves with its play, by design |
| **The run-off** - each eliminated electorate splits by exp(−d²/τ) in the sovereignty space, each candidate at their own party's CHES unit | `PresidentialElection.RunOffShare` | B4 (§764), E1 |
| **The veto** - a statute is at risk where the president's backing party did not vote for it; a seeded draw at the president's own rate on the record decides (`PresidentialVeto.Draw`), in one gate (`SimulationManager.PresidentialVetoGate`); the override 3/5 of those voting; the budget act and an amendment exempt; the slips show the risk; the signing plate stamps VETO STANDS / OVERRIDDEN | `PresidentialVeto` | B1/B2 (§761), widened by F3 (§778) |
| **The surface** - Politics › Parliament's THE PRESIDENT row (the backing party's mark, OF RECORD / ELECTED IN THE GAME, the president-elect and the oath's day, the next first vote) and its slip | `GameController.ParliamentRows.cs` | §770 |
| **The presidential night** - folded into D-PL (asked, returned, not built); until it is built a verdict lands at once on the President row | D-PL | E3 (§774) |
| **The locked card** - Poland's presidential start card, its brief the record read through the rule (`TwoRoundElection.RecordBrief`), its reason `StartPoints.PresidentialStartReason` | `StartPoints` | §720, §770 |

**What is not there:** a start that opens on the presidential contest; a way to be a candidate rather than a party; the player's candidate
marked in a contest; a verdict for the player; a President's verb (every president, the game's or the record's, decides by the draw); a
PRESIDENT office in the role gate; a Polish campaign of any kind.

## 3. What a presidential START needs

Each piece names the file it changes and, in brackets, the ask of §5 it waits on.

1. **The start point** (`StartPoints`): Poland's presidential card PLAYABLE, its `Opens` the ruled start date [ask 4]; `StartPoints.PresidentialStartReason`
   retired for Poland (France keeps it until S8). Its mode line and its brief read the card, not the country.
2. **The epoch keyed by start point** - the structural blocker: the world's start is keyed by COUNTRY today (`WorldClock.StartDate`), so a second
   playable start in one country has no date to open on. The measured cost and the shape proposed are §4.2; Code's to build, no ruling needed.
3. **The candidate picker** in place of the party panel (`CountrySelectorScreen`, Canvas): the candidates offered [ask 1], each with the backing
   party's mark, the role that party holds at the start, and why a candidate is not pickable where one is not.
4. **The player's candidate in the field** (`PresidentialElection`): the contest marks which candidate is the player's (a field on
   `PresidentialElection.Contest`, saved); the field of record and its factors unchanged, so a passive player on the eve reproduces the PKW's first
   round as E1 fitted it [the side question of ask 1].
5. **The verdict for the player** (`GameController`): a line - and, once D-PL builds the night, the night - saying what the result means for the
   player; the President row as built.
6. **The post-vote role** [ask 2]: what a win and a loss make of the player; if a win makes the player President, a PRESIDENT office beside the
   party's role (the role words shared with US-7's), a veto verb at the gate in place of the draw for a player president, the signing plate's
   two answers (`SigningScreen`, Canvas), and the save step that carries the office.
7. **The brief** (`StartBrief`): a presidential brief - the president of record and his term's end, the government of record and its seats, the
   first vote's day and the run-off's, the field - every clause traced, as SP-2's (`StartBrief.Clauses` already reads a start point, not a country).
8. **The campaign** [ask 3]: none is staged in Poland.
9. **The save**: one format step (`SaveGameService.CurrentSaveVersion`) carrying the player's candidate, the start's key and, if ruled, the office;
   an older save loads as it does today (every save cut before the step is a Sejm-start game).
10. **The checks**: a presidential-start diagnostic (the done-when's); `StartPointsDiagnostic` and `StartBriefDiagnostic` re-pinned (Poland offers two
    playable starts); `WorldClockDiagnostic` (the new start follows its ruled rule); `PresidentialElectionLiveDiagnostic` byte-identical (the AI path
    is not touched); `SaveMigrationCheck`; the review ledger.
11. **Baselines**: the no-player dumps should not move - the presidential start is a path only the player's choice opens, and the AI's 2025
    election already sits in the trajectory family; proved by the dump diff, not by reading.
12. **Order**: after US-7, so the PRESIDENT and PRESIDENT-ELECT words are written once (the US plan builds US-7's oath and US-8's count
    player-agnostic for this reuse, `USA_STAGE_PLAN.md`). **US-7 supplies the words only** - its PRESIDENT is the head of a presidency whose party
    governs. A Polish player-president whose party is in opposition or a junior partner needs an office that sits beside the party's role
    (`PlayerRole` holds None and the party's four roles) and a veto verb at the gate: that is SP-7's own new work whichever order is taken. SP-7
    does not wait on D-PL: the verdict lands at once until the night is built, as every Polish verdict does today.

## 4. Measured (without Unity)

### 4.1 The start date the statute and the record give

Read in `ElectionsData/poland/presidential_calendar_2025.md` (the Marshal's order, Dz.U. 2025 poz. 48, and the three backing parties' pages not
held before, fetched byte-exact; the Electoral Code and the Konstytucja as held); the day counts are DERIVED against the first vote of record,
18 May 2025. Every nomination date is the party's own page's (§621), never press reporting.

| the day | what it is | source | days before 18 May |
|---|---|---|---|
| 20 Aug 2024 | Konfederacja's Council of Leaders names Mentzen - the party's own page, which names the decision's day | `presidential_calendar_2025.md` §5 | 271 |
| **22 Sep 2024** | the standard run-up's first day - 26 + 8 weeks before polling day (§618's rule for a scheduled election; `CampaignCalendar.PreCampaignStart`, the rule `WorldClock.StartDate` cuts every scheduled start on) | DERIVED | 238 |
| 13 Nov 2024 | Hołownia announces his candidacy at Jędrzejów - Polska 2050's own pages (as *"kandydat niezależny"*; Trzecia Droga's backing is the game's reading, §770) | §5 there | 186 |
| 18 Nov 2024 | KO's primary still has two contenders, Sikorski and Trzaskowski - the PO site's own page of that day | §5 there | 181 |
| 30 Nov 2024 | PiS's Political Council endorses Nawrocki - the party's own page, its post dated that day | §5 there | 169 |
| by 7 Dec 2024 | KO's candidate is Trzaskowski - the PO site's page on the Gliwice convention (*"jeszcze raz"*: said again); KO's exact day is not on a held page | §5 there | 162 |
| 15 Dec 2024 | Lewica's National Council names Biejat - the party's own page | `presidential_returns.md`; §5 there | 154 |
| 6 Jan - 6 Feb 2025 | the window the Marshal may order the election in - 7 to 6 months before the term ends (Kodeks wyborczy art. 289 § 1; Duda's term ends 6 Aug 2025) | art. 289 § 1 | 132 - 101 |
| **15 Jan 2025** | **the Marshal orders the election** for 18 May, published and in force that day; **the campaign begins on its publication** (art. 104); the PKW's opinion on the calendar came before it (art. 290 § 2) and is not held | Dz.U. 2025 poz. 48; art. 104 | 123 |
| 23 Mar 2025 | the standard 8-week campaign's first day (`CampaignCalendar.CampaignStart` for 18 May) | DERIVED | 56 |
| 24 Mar 2025 | the committees' last day to notify the PKW (art. 299 § 4, the 55th day) | the calendar | 55 |
| 4 Apr 2025, 16:00 | the candidates filed with 100,000 signatures (art. 303 § 1 as amended after 2011 - the 2011 text read 24:00 on the 45th day; the 2025 calendar applies the amended 16:00 on the 44th) | the calendar | 44 |
| 16 May 2025, 24:00 | the campaign ends | the calendar; art. 104 | 2 |
| 18 May, 1 Jun 2025 | the two rounds | the PKW's notices (§720) | 0 |

**What every candidate start seats** (read in `WorldClock`, not a choice between them): the 10th Sejm (the 2023 election's lists, convened 13 Nov
2023), Tusk's KO+TD+NL government of record (from 13 Dec 2023, its cabinet sourced), Duda the president of record. **This is the reference world's
basis**: E1's factors were fitted on a world built on the eve, which seats the same chamber and the same government. A game from the Sejm start
reaches 18 May 2025 on another basis - its prediction reads the history of the chamber seated at its epoch (2019's) and its government is
the one its own 2023 formation installed (§772's premise, ruled by F4) - so **a presidential start, whichever date, keeps E1's basis; only the run-up's own drift separates it from the reference world.** How
large that drift is for each candidate date is the Unity measurement of §4.3.

**What the game's machinery offers a presidential start today** (read in `SimulationManager`, `WorldClock`, `LiveCampaignSetup`): the run-up
and the campaign are keyed on the player's country's next polling day (`SimulationManager.CurrentCampaignCalendar` reads
`WorldClock.TryNextPollingDay`), which from any 2024-25 date is the Sejm's of 17 Oct 2027 - **no presidential campaign calendar exists**; and no
Polish campaign is staged at all (`LiveCampaignSetup.TryFor` stages Sweden's and Germany's).

### 4.2 The cost of re-keying the epoch from country to start point

**The epoch itself is a date and survives unchanged.** `SimulationManager.EpochDate`, set by `SimulationManager.SetEpoch` before a world is
created, is read as a date everywhere - the turn arithmetic, the campaign windows (`CampaignCalendar.FromWorldStart`), who governs at the start
(`GovernmentRecord.AtStart` in `WorldFactory`, `SeatedGovernment.TryFor`, `WorldClock.Resolve`), the publication system, the cohorts, the
Desk's dates, the save (`SaveGame.EpochDate`, restored on load before the clock). **None of those readers knows which start opened the world,
so none changes.** What is keyed by country is the MAPPING to that date, and the selection path that applies it.

The reading (HEAD `43ef9ff9`; re-derive with `grep -rn` on each name below under `Assets/`):

| class | readers | what a second Polish start costs there |
|---|---|---|
| **A. The country → date mapping** | `WorldClock.StartDate` (defined by the start rule; read by `StartPoints` for the ruled cards, `WorldClock.StartLine`, `WorldClock.ApplyStart`, `WorldClock.SeedVintageLine`, `WorldClock.PickerViewOf`, `GameController.OpenWorldAt`, `CountrySelectorScreen`'s seat-bar caption, and `LiveCampaignSetup`'s German staging) | none if it stays **the country's default start** (Poland's Sejm start) - the presidential start's date lives on its own start point (`StartPoints.StartPoint.Opens`, already carried) |
| **B. The selection path** | `CountrySelectorScreen.ShowStartPanel` → its SELECT, which drops the chosen card and opens the party panel → `GameController.SelectPlayerCountryAndParty` → `GameController.OpenWorldAt(country)`, which reads the country's date | **the real cost**: the chosen start point travels to the controller, `OpenWorldAt` opens on the start's own date (the default overload keeps the country's), and a presidential card's SELECT opens the candidate picker instead of the party panel |
| **C. Start predicates keyed by country** | `WorldClock.LatestElectionDay` (the start's polling day - the Sejm's for Poland), `WorldClock.IsSnapStart`, `WorldClock.GoverningModeOnly`, `WorldClock.NoElectionYet`; `WorldClock.StartLine` (the folder card) | the presidential card's lines read the start point (`StartPoints.ModeLine` and the brief already do); the folder card keeps the country's default line, or names both starts - a Design note, not code; `WorldClock.TryNextPollingDay` keeps reading the Sejm (the presidential days are `PresidentialElection`'s) |
| **D. The picker's view at the start** | `WorldClock.PickerViewOf` (read by `CountrySelectorScreen` and the IMGUI picker in `GameController`) | evaluated at the start's date instead of the country's; on Poland's two starts it reads different chambers (2019's at the Sejm start, 2023's at the presidential) |
| **E. "One playable start per country"** | `StartPoints.TryPlayable` (read only by Editor tools: `AuditDriver`, `StartBriefDiagnostic`, `StartPointsDiagnostic`); the start sheet pre-selects the first playable card | those tools re-pinned to name the start; the sheet keeps date order and pre-selects the first playable (the Sejm's) - DECLARED, Code's |
| **F. The Editor's world-openers** | `WorldClock.ApplyStart`'s callers across the diagnostics (each opens "the country's start") and `SimulationManager.EpochScope` around them | none: they keep the default start; a presidential-start diagnostic sets the start point's own date inside its own `EpochScope` |
| **G. The save** | `SaveGame.EpochDate` carries the date; `SaveGameService` restores it | no field is needed to IDENTIFY the start - Poland's two starts open on different dates, so (country, epoch) names it - but SP-7 steps the format anyway for the player's candidate, and carries an explicit start key in the same step so no later start can collide |

**The shape proposed (DECLARED, Code's):** `WorldClock.StartDate` stays the country's default start and every reader in A, C and F keeps its
meaning; the start point - which already carries its own `Opens` - is what the selection hands over (B); the picker's view takes a date (D);
the save's step carries the start's key (G). The edits fall in `StartPoints`, `WorldClock`, `CountrySelectorScreen`, `GameController` and the
save, plus the Editor tools that pin one playable start per country - the files the card names, and none of the Editor's world-openers.

### 4.3 What needs a Unity run, and the instrument written for it

**`PresidentialStartMeasure`** (`Assets/Editor/PresidentialStartMeasure.cs`, in no bar; run by name:
`-executeMethod PoliSim.EditorTools.MultiRun.Run -methods=PoliSim.EditorTools.PresidentialStartMeasure.Run`). It opens a fresh Polish world as a
new game opens one on every start ask 4 offers - the standard run-up (22 Sep 2024, 4a), the Marshal's order (15 Jan 2025, 4b), the committees'
deadline (24 Mar 2025) and the candidates' (4 Apr 2025), both 4c - seats the player's party as KO and as PiS, runs each at the trajectory
sentinel's two seeds (777 and 424242: the model's own releases over a run-up carry seeded revision noise), and steps the play loop's simulation
calls through both rounds. It prints, per world: the role the seat gives, the government and the president of record at the start, the poll on
18 May and the term reading it carries, every candidate's first round against the PKW's, the run-off and the verdict; then a summary table; and
the CAMPAIGN lines of §4.1.

- **What a seat is here.** KO seated is the prime minister's party, so the government is the idle player's and no ministry acts for it. PiS
  seated is opposition, but **not the whole AI government**: no turn boundary falls in any window, so the AI finance ministry (which acts at the
  boundary) never acts either. What differs by seat is the day tick's - the budget the AI government lays as a bill where the player does not
  govern, and the foreign-meeting rolls, which go to the government.
- **The eve rows check the wiring, nothing more.** The control is the eve with no party seated - E1's reference world - at both seeds; beside it
  the eve seated KO and PiS. On the eve the first round is held inside the one day stepped, before anything a seat reaches, and no vote path reads
  the player's party or its approval, so those rows equal the control by construction and the instrument asserts they do. **Only the worlds opened
  earlier - 22 Sep, 15 Jan, 24 Mar, 4 Apr - can show an effect of the seat or of the run-up.**
- **What it fails on:** the control not reproducing the PKW's first round within the live diagnostic's tolerance at either seed, the two seeds'
  controls differing, or an eve row with a seat differing from the control (the instrument would not be measuring what it says); a world not
  seating the government of record, or not holding exactly one contest on the record's days; any exception; and any error the simulation logs
  while it steps - the log fold is armed, so `CheckExit` turns it into a non-zero exit naming the count and the first line, and `MultiRun` shows a
  FAILED row. The figures print either way.
- **Determinism.** The control is seeded and the bar's reference world is not; the instrument asserts the control's first round and run-off are
  identical at both seeds. That is expected from the code: on the eve the poll reads the government's record from the sourced pre-start record
  (no revision noise is drawn before the model publishes its own releases), so the comparison with E1's stored factors does not depend on a seed.
  Over a run-up the model's releases do draw noise, which is why every earlier start runs at both seeds.

**Ask 4 rides on its summary table**: for each start, at each seed and seat, how far a passive game's first round lands from the record and whom it
elects.

## 5. The four asks (Elias's)

### Ask 1 - Who the player is

| option | what it is | measured |
|---|---|---|
| **(a)** | **A candidate of record backed by a roster party** - Nawrocki (PiS), Trzaskowski (KO), Mentzen (Konf), Hołownia (TD), Biejat (NL). The player's party is the backing party, so the player also holds its role in the Sejm from the first day: KO the prime minister's party, TD and NL junior partners, PiS and Konf opposition. | The candidate's first round is the party's poll × the factor of record (E1), so the player moves it through the party - governing, or voting - and the government's record; a passive player on the eve reproduces the PKW's first round by construction - the instrument's eve rows (§4.3) equal the control by construction too and only check its wiring; what a seat does over a run-up shows only in the worlds opened earlier. Every number traces to E1's fit. |
| (b) | (a), and also **the eight of record whom no roster party backs** - Braun, Zandberg, Stanowski, Senyszyn, Jakubiak, Bartoszewicz, Maciak, Woch. | No Sejm party, so no role - and an empty party is today the tools' case, which `SimulationManager.PlayerGoverns` reads as governing (a partyless role is new code). Their first round is their share of record, which no verb moves (E1). Stanowski, Senyszyn, Jakubiak, Bartoszewicz, Maciak and Woch stand at no CHES unit, and an unplaced finalist leaves the run-off uncountable (*"nothing is decided"*, `PresidentialElection`). After the vote there is no party to continue as. |
| (c) | **The player's own candidate through a voters' committee** (B5: 15 citizens, 1,000 then 100,000 signatures) - (c1) a created party's leader; (c2) an independent with no party. | (c1) already stands in a later field (E2) at `PresidentialElection.FutureFactor`, and in 2025 too if the party's predicted voters clear the signatures (the gate's reading) - it needs Poland's created parties (SP-5) and the ballot-access run-up task SP-3 owes. (c2) has no poll to inherit and no share of record, so the model holds no support for it at all - a new model, Elias's to specify. |

**Side question:** does the player's candidate of record keep the factor of record? The card's question - *whether the player's own candidate in
2025 counts as a future field* - read for each option: (a) no, a candidate of record is the field of record; (c1) yes, a created party's leader
is a future field at factor 1.0, as E1 rules. **The alternative's size** - the player's candidate of record moved to factor 1.0, every other
candidate as fitted - DERIVED from the stored factors (`PresidencyOfRecord.CandidateOfRecord`, as stored at HEAD `43ef9ff9`; a refit moves the
table) and the PKW's first round as generated, on the reference world (where each factor is the share of record over the party's poll, so the
poll is recovered exactly):

| the player's candidate | factor of record | support × at 1.0 | first round of record | that candidate's first round at 1.0 |
|---|---:|---:|---:|---:|
| Hołownia (TD) | 0.2439 | ×4.10 | 4.99 % | 17.73 % |
| Biejat (NL) | 0.4703 | ×2.13 | 4.23 % | 8.59 % |
| Nawrocki (PiS) | 0.8799 | ×1.14 | 29.54 % | 32.27 % |
| Trzaskowski (KO) | 1.0437 | ×0.96 | 31.36 % | 30.45 % |
| Mentzen (Konf) | 2.1781 | ×0.46 | 14.81 % | 7.39 % |

So at factor 1.0 the choice of candidate would hand a Hołownia player more than three times the record's first round and cost a Mentzen player
half of his - before the player has done anything. That is the ground for keeping the factor of record for (a).

**Code recommends (a)** - with (b) shown on the picker and not pickable, the reason in one line (*no party in the Sejm: nothing a player could
move*); (c1) when SP-5 lands Poland's created parties and the ballot-access task; (c2) not planned. **And the factor of record stands for (a).**
**Grounds:** (a) is what the model holds honestly - support from E1's fit, verbs from the role machinery (`POLITICAL_SYSTEM_SPEC.md` §5.2), a
loss that continues by decision 4 - and it needs no new model of support; (b) and (c2) would put the player where no verb reaches the outcome.
**The measurement it rides on:** the field and its backing (`PresidencyOfRecord.CandidatesOf`), the roles at the start (the government of record,
§4.1), the empty-party reading (`SimulationManager.PlayerGoverns`), the unplaced finalist (`PresidentialElection`), the factor-1.0 table above;
and, in Unity, the KO and PiS worlds of §4.3 opened before the eve - what a passive candidate's party seat does to the first round over a run-up.

### Ask 2 - What follows the vote

| option | a win | a loss |
|---|---|---|
| **(2a)** | **The player becomes the President** on the oath's day (6 Aug 2025: art. 291 § 1, E2). The office's verb the model can hold: **signing or vetoing each statute the Sejm passes** - the President's own act, needing no countersignature (Konstytucja Art. 144 ust. 3 pkt 6) - the player's answer replacing F3's draw for a player president (the draw stays for every AI president); the 3/5 override as built (B2). **Cohabitation** - with whatever government the Sejm carries (Tusk's of record, open-ended in `WorldClock`) - is the state the game already holds. The party's levers pass to the AI while the player holds the office - READING, of **Art. 132** (the President holds no other office and performs no other public function): a party role is not an office, but a player directing both the presidency and a governing party would sign its own bills. | **(L1)** the player continues as the backing party - decision 4, the party still sits in the Sejm |
| (2a′) | (2a), but the player keeps directing the party as well | (L1) |
| (2b) | The player stays the party; the candidate elected is an AI president, deciding by the draw as built (§770: the veto reads the game's president) | (L1) |
| (2c) | The run ends at the verdict - a one-contest scenario. **A change to `POLITICAL_SYSTEM_SPEC.md` §7** (a run ends when the party leaves parliament or the player ends it) **and its decision 4**, Elias's to make if he takes it | the run ends - the same change |

**And when the presidency ends** (after two terms, or a re-election lost in the game's own 2030 election) - a choice of its own, with these
options: **(end-i)** the run continues as the backing party, which the AI held meanwhile; **(end-ii)** the run ends with the presidency - a change to
§7 and decision 4, as (2c) is.

**Code recommends (2a) with (L1), and (end-i).**
**Grounds:** the spec's S7 is *"the president's role in a parliamentary republic - the veto and its override, and cohabitation"*, so a win should
make the player play that role; the veto is the one presidential power the model holds whole (§761, F3); cohabitation costs nothing (the
government is the Sejm's); the reading of Art. 132 keeps the veto a check on someone else's bills, which (2a′) would not. (2b) is the smallest build and the
fallback if the verb waits. (L1) and (end-i) are decision 4 already: losing an office never ends a run while the party sits in the Sejm.
**What (2a) costs (measured), honestly sized:** it is the largest of the options, and almost all of it is SP-7's own new code -

- **an office beside the party's role.** `PlayerRole` holds None and the party's four roles; a President is not one of them, and a Polish player-president's
  party can be in opposition or a junior partner, so the office is a second axis (who the player is), not a fifth role - with the role gate's reasons,
  the Desk's chip, the save, and the party's levers passing to the AI while it is held;
- **a veto verb.** No President's verb exists: `PresidentialVeto.Draw` decides for every president, in `SimulationManager.PresidentialVetoGate`. The
  gate must hold an at-risk statute for the player's answer instead of drawing, and the signing plate must offer the two answers. Art. 122's 21 days
  are not modelled (§761's premise: the veto is taken on the passage's day), so the player's answer holds the passage until given, as a cabinet
  decision holds the clock today;
- **from US-7, the words only** - PRESIDENT and PRESIDENT-ELECT (`USA_STAGE_PLAN.md`, US-7 and D-US part two's role words), written for a presidency
  whose party governs; the order of §3 item 12 saves writing the words twice and nothing more.

The record's own rates and the at-risk share on the 10th Sejm, which say how often the verb would come up: §778.

### Ask 3 - The campaign

| option | what it is |
|---|---|
| **(3a)** | **No campaign**: the candidate plays through the backing party's role (ask 1 (a)) until 18 May - its poll moving the candidate's share by E1's product; the start opens close to the vote (ask 4) so the wait is short; the card and the brief say plainly that no campaign is staged. |
| (3b) | Stage a Polish presidential campaign first - the campaign engine on Poland (the 16 województwa as regions, an electorate per województwo - BILLED, not on disk - the casts derived from CHES by decision 5, a presidential calendar keyed on the first vote) - and land SP-7 after it. |
| (3c) | A national campaign only - the campaign verbs with no regional layer. |

**Code recommends (3a)**, with **a Polish campaign filed as a row of its own, PL-C**, serving both Polish elections - no row of
`POLISIM_FEATURE_LIST.md` owns one today, though `POLITICAL_SYSTEM_SPEC.md` §4 lists Poland's campaign as *"districts and cast owed"* - the
presidential start taking it up when it lands. **PL-C** (proposed, to be filed with the ruling): the campaign engine on Poland for the Sejm's and
the President's elections - the regions (the 16 województwa; the 41 okręgi for the Sejm), an electorate per region (BILLED until sourced), casts
derived from CHES (decision 5), and a presidential campaign calendar keyed on the first vote, not on the Sejm's polling day; done when *a Polish
campaign plays*.
**Grounds:** SP-7's done-when does not ask for a campaign; a Polish campaign is a sourcing bill and a row of its own, and both Polish starts need
it; under (3a) the candidate still has verbs and every figure traces to the model. (3c) would be a campaign the engine does not have.
**The measurement it rides on:** `LiveCampaignSetup.TryFor`'s stated reason and the 2027 polling day the run-up keys on - printed by the CAMPAIGN
lines of §4.3, and read in §4.1.

### Ask 4 - The start date

| option | the day | days to 18 May | what it rests on |
|---|---|---|---|
| (4a) | **22 Sep 2024** | 238 | §618's rule for a scheduled election (the standard run-up), as Poland's Sejm start and Sweden's are cut |
| **(4b)** | **15 Jan 2025** | 123 | **the Marshal's order** (Dz.U. 2025 poz. 48), on whose publication **the statute opens the campaign** (art. 104) |
| (4c) | 24 Mar or 4 Apr 2025 | 55 or 44 | the committees notified, or the field filed with its signatures |

**Code recommends (4b).**
**Grounds:** (1) **art. 104 opens the campaign on the order's publication** - the statute's own start of the contest, dated by the act itself and
sourced byte-exact; it is the test §618's snap rule applies (*"the first day a primary source records that an early election was set in motion"*)
read through the statute's campaign rather than through the first act on file (the PKW's opinion on the calendar came before the order, art. 290
§ 2, and is not held - nothing rests on it); (2) at 22 Sep 2024 **four of the five backing parties had not yet named the candidates the picker would
offer** - Polska 2050's Hołownia on 13 Nov, KO's Trzaskowski after 18 Nov and by 7 Dec, PiS's Nawrocki on 30 Nov, Lewica's Biejat on 15 Dec 2024;
only Konfederacja had (Mentzen, 20 Aug 2024) - each date from the party's own page (§4.1); by 15 Jan 2025 all five had; (3) with no campaign
(ask 3) a 238-day run-up is 238 days in which a candidate plays only the party - 123 halves it; (4) every option seats the same chamber and
government (§4.1), so the date changes only the run-up's length and its drift.
**Against it:** it departs from the scheduled-election rule ruled at §618 and written as `POLITICAL_SYSTEM_SPEC.md` decision 2, so Poland's two
starts would follow two rules - a deviation that is Elias's to allow. (4c) opens with the committees notified (24 Mar) or the field filed
(4 Apr), but leaves the candidate almost no run-up.
**The measurement it rides on:** the calendar and the parties' dated pages of §4.1 (sourced, done); and, in Unity, **`PresidentialStartMeasure`'s
summary table** - for every option, 4a, 4b and both days of 4c, at two seeds and two seats: the first round's drift from the PKW's and the verdict
for a passive game from that date (F4 makes the drift the game's own, so the table informs the date, it is not a fit).

## 5a. RULED (Elias's ruling I14, 2026-10-18; installed `COMPLETED.md` §857; verbatim in `docs/RULINGS_2026-10-18.md`)

- **Ask 1: (a)**, a candidate of record backed by a roster party, **with the factor of record standing** (the side question).
- **Ask 2: (2a), with (L1) and (end-i).** The player becomes the President on the oath's day, signs or vetoes each statute the Sejm passes, and continues as the backing party after a loss and when the presidency ends. **I12 (i) adds the Docket:** a statute the player-president leaves unanswered after Art. 122's 21 days is signed, DECLARED.
- **Ask 3: (3a)**, no campaign staged, said on the card. **PL-C is filed** (`POLISIM_FEATURE_LIST.md`): a Polish campaign serving both Polish elections.
- **Ask 4: (4b), 15 Jan 2025**, the Marshal's order, as **a stated deviation for presidential starts only**. The Sejm starts keep §618's rule (`POLITICAL_SYSTEM_SPEC.md` decision 2). The start card and the brief say so.

The work plan below is now ruled, every option on the recommendation. Its place in the order of 2026-10-18 is the feature list's.

## 6. The work plan, once ruled

On the rulings as recommended (the order changes nothing else if another option is ruled):

1. **The start point and the epoch** (`StartPoints`, `WorldClock`, `CountrySelectorScreen`, `GameController`): the presidential card playable on
   its ruled date; the start point handed through SELECT to `OpenWorldAt`; the picker's view at the start's date; the folder card's line. Checks:
   `StartPointsDiagnostic`, `WorldClockDiagnostic`, `StartBriefDiagnostic` re-pinned. Tier UI + simulation (the card is Canvas: a REAL film).
2. **The candidate** (`PresidentialElection`, the candidate picker, the save step): the five of ask 1 (a) pickable, the eight shown with their
   reason; the player's party seated as the backing party; the contest marking the player's candidate; the presidential brief. Check: the
   presidential-start diagnostic - a fresh world from the start through both rounds, one contest with the player's candidate in it, a save across
   the rounds loading into the game (§557).
3. **The verdict and the office** (after US-7): the verdict's line; on a win the PRESIDENT office from the oath's day, the party passing to the AI;
   the veto as the player's verb at the gate (the draw kept for an AI president) and the signing plate's two answers; on a loss the party plays
   on. Checks: the diagnostic forces both outcomes and steps past 6 Aug 2025; a save across the oath loads into the game; review row
   (`SimulationManager.cs`, a money path); `PresidentialElectionLiveDiagnostic` unchanged; the dump diff shows the no-player family unmoved.
4. **The close**: the cheap and simulation bars; the dry film of the Poland session; REAL films of the start panel, the candidate picker, the
   President row and the signing plate at 1280 and 2560.

**Built** (SP-7b, `COMPLETED.md` §886, 2026-10-20): steps 1-4 as ruled - the card on the Marshal's order, the candidate picker, the contest's mark, the office from the oath (the party's verbs refused, the budget window closed), the player's veto on the docket at J6 (iii)'s terms (an answer at once), the plate's answers with LATER and the Docket page's answer row, format 50; the bars and the REAL films at 1280 and 2560.

## 7. Stated premises

- The start date's options all lie in Duda's term and Tusk's government; none crosses a Sejm election, so no formation runs before the vote.
- The presidential night is D-PL's (E3); until it is built, the verdict lands at once - SP-7 does not wait on Design.
- A presidential start's later elections run as a Sejm-start game's do: the Sejm on 17 Oct 2027 (D3), the next first vote in 2030 (E2), each
  on the game's own model.
- France's presidential start (SP-8) reuses the start point and the candidate path; its own model (the two-round French system, the pre-2022
  record) is its row's.
