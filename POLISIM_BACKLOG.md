# PoliSim — the backlog register

**What this is.** THE single ordered register of open work. Built 2026-08-31 (C-0.1 of the clearance
list) from every source that held a backlog: the clearance list, `ELECTIONS_PROTOTYPE_WORKLIST.md`,
`PLAYTEST_1_WORKLIST.md`, `MISSING_PREREQUISITES.md`, `POLISIM_MASTER_ROADMAP.md`'s live-work section and
trigger shelf, `ELECTIONS_PLAY_CALIBRATION.md`, `CLAUDE_DESIGN_ASSET_REQUEST.md`, `ELECTIONS_GAP_TABLE.md`,
and the riders recorded this week that no item owned.

**The rule this file exists to enforce:** every open item appears **exactly once**, here. A source
document may describe an item; it may not also queue it. Work that is *finished* goes to `COMPLETED.md`;
work that is *waiting on a named party* keeps its detail in `MISSING_PREREQUISITES.md` but its ROW is here.

**The repo outranks this file.** Where a row says open and `git log` says done, the log wins and the row is
corrected, not re-worked. Every row marked closed cites its commit.

**Columns.** ID · what · done-when · owner · class · depends on.
**Owner:** CODE (a session) · ELIAS · DESIGN · CALENDAR.
**Class:** SAFE (no trajectory move expected) · BASELINE (moves a trajectory; needs a new explained family,
per country) · RULING-BLOCKED (cannot start until someone rules) · WATCH (a standing guard, never a task) ·
TRIGGER (real work whose trigger has not fired) · DEFERRED (recorded, deliberately not built).

---

## 0. Rulings taken 2026-08-30, before this pass ran

These four changed what the pass builds. Recorded here so no row below reads as still-open.

| ruling | what was ruled | what it unblocks |
|---|---|---|
| **R-CL1** | **The player has a party.** The player picks one of the country's real seeded parties at country selection; personal approval and party approval are separate stocks; losing office is not game over. | the rail cell, the win/lose rule, C-B5's gate, and the play-calibration list's whole premise. Executed as Track R. |
| **R-CL2** | **`eu_position` is ruled in as the openness axis** for the Trade bill's vote, recorded as a named ruling with its stretch stated — EU integration standing in for trade openness is an approximation and is tagged as one. | C-B3 |
| **R-CL3** | **§38 carry-over is BUILT**, `SaveVersion` 2 → 3, with the standing electorate gap stated rather than papered over. | C-D4 |
| **R-CL4** | **The two `StatNodeId` members go in now**, and a missing icon becomes a reported GAP rather than a check failure, on `PartyMarkCoverageCheck`'s own precedent. | §E4, C-F1 |

---

## 1. The clearance pass — live work (owner CODE unless stated)

Execution order: Phase 0 → A → B → C → D → R → E → F → G. One commit per item; stop at item boundaries.

### Phase 0 — the reconciliation

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-0.1 | This file: the single ordered register | every open item appears exactly once; every closed row cites its commit; a grep proves no open item sits in a source document without a row here | CODE | SAFE | — |
| C-0.2 | The post-wiring re-derivation — correct every document that still asserts a pre-wiring premise | no live document asserts a pre-wiring premise (grep: `PartyArchetype`, `TotalSeats = 200`, "not wired", "unreachable from any gameplay path", "VERIFIED NOTHING", "no party seeds exist on main", "UNINSPECTED") | CODE | SAFE | C-0.1 |
| C-0.3 | The stranded branch disposed — migrate its four unsuperseded pieces to `COMPLETED.md`, retire the obligation, keep the ref | no live document treats `stranded/politics-elections` as pending work | CODE | SAFE | — |
| C-0.4 | A batchmode entry for the check suite (`RunAllBatch`) — the nine currently run as nine invocations | one invocation reproduces the nine's current verdicts exactly; the per-check invocations still work unchanged | CODE | SAFE | — |

### Track A — the verifications whose blockers have landed

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-A1 | The Italy FdI standing test re-run (4.35 → 29.27 %) | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §87): UNREACHABLE.** Blended 10.31 % (dev −18.96 pp), spatial alone 17.82 % (dev −11.44 pp), gate MAD 7.14 pp REGRESSED at 53 % coverage. Momentum and media cannot move a vote at all (two call sites, both polls); the persuaded share required is 58.58 % against 17.82 % produced, ×3.29. **Ceiling: per-group loyalty → C-D1.** Original done-when: the measurement is on record either way, **the path it ran on is named**, and the standing test is closed. No tuning; the loyalty constant is not re-fitted | CODE | SAFE | — |
| C-A2 | The local-campaigning question (the worklist's standing design question, until now unowned) | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §88): the MECHANISM, not the weighting.** Rally 0 for all eight parties; door 0 for seven of eight. The grassroots personality carries door affinity 2.2 and enthusiasm value 1.6 — the strongest pro-local thumb in the roster — and still knocks zero doors, so §33 is not blind to local value. The three local actions hold the largest hour costs (4/3/5 h) and smallest reaches (0.06/0.02/0.01) while §33 scores per hour. Original done-when: the measurement names the cause — the model underpowers local action, or §33's EV function undervalues local reach. **No adjustment in this item** | CODE | SAFE | — |
| C-A3 | 2a-iv re-measured after W-B12 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §89): stays PEND at est/grass 0.269** (prof/grass 0.573), 0.031 below the line — **not 0.291**, which has been superseded since W-F1. W-B12 moved it by nothing, so the convergence is in what the two parties CHOOSE, not what they can afford. The dependency is re-pointed from the stopped W-F5 to the pool question, C-D2. Original done-when: the line carries a current number. The 0.30 threshold does not move | CODE | SAFE | — |
| C-A4 | The claim sweep — re-word every claim whose evidence has since been superseded | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §90).** The broadest claim in the repo — "five of six real chambers reproduce EXACTLY" — counted a STAGE as a chamber: it is four chambers (Sweden 2022, Germany 2025, Poland 2023 real-system, USA 2024, each deviation 0/exact) plus **Italy 2022's national proportional stage only, 245 of the Camera's 400**, with France uncovered. Every pre-W-F1 "seat-for-seat" is scoped at its two origin sections. Original done-when: no record overstates its own evidence | CODE | SAFE | C-0.3 |

### Track B — item 10 and the D0 reconciliation

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-B1 | §E2's mark accounting, recorded (the code half shipped at `a289e1e`) | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §91) with no new work, honestly reported**: W-G1 did the code half, C-0.2 re-derived §E2, C-0.4 measured 53/1/52/0, and D8-1 already quoted the check verbatim. R3 DISCHARGED. Original done-when: §E2 states the real accounting; the 52 feed D8-1's count | CODE | SAFE | — |
| C-B2 | The R5 hex exchange — Sweden's eight inks, 45 named uncoloured | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §91).** Sweden's eight seated hexes delivered into D8-2, 45 named uncoloured. ⚠ **Built `PartyInkHarness`, which `PoliSimTheme` already claimed was checking this and did not exist** — and it found the constraint BROKEN: **S and V collapse onto one ink `#753838`** (a 106-seat and a 24-seat party drawn identically), and six of eight sit inside the derived 8.7° floor. Reported as PEND, not fixed; both are D8-2's ruling. Original done-when: the exchange is a line in the Design ask, not a gate in the prerequisites file. Nothing picked by eye | CODE | SAFE | C-B1 |
| C-B3 | The trade axis for the Trade bill's vote, on R-CL2 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §92).** `EuPosition` on the party record (31 sourced values, joined by each party's existing lrecon/galtan pair, AVS and the other unscored units left NaN), `TradeStance` over §29's own `RescaleEu`, a `BillAxis` the vote and both screens share. ⚠ **Poland is the one chamber that changes its mind** (+0.150 → −0.227 on a rise); Sweden and Germany triple in magnitude without flipping; **the USA falls back to fiscal, asserted exactly, because GPS 2019 has no EU item**. Trajectories 6/6 identical — containment only, the per-country table is the evidence. Original done-when: the vote reads a trade position or documents why it cannot, **per country** (the USA has no `eu_position` and keeps the fiscal axis with the reason stated) | CODE | SAFE ⚠ vote-side evidence, not trajectory | R-CL2 |
| C-B4 | Riksbank-B merged into P-D1 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §93): MERGED into C-C7, not inherited** — P-D1 specifies the same subject, and felt verdict 2 ("still not independent") travels with it. Three documents that said it was waiting now say where it went, including the shelf line explaining its absence from the shelf. Original done-when: one item, not two, with felt verdict 2 attached | CODE | SAFE | — |
| C-B5 | Step 6 (story mode) re-gated — scope the gate, not the work | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §93).** Both old gates fired (item 10 shipped; the player-party question ruled R-CL1). ⚠ What remains is **not a ruling but two builds Step 6 does not own** — C-R2 (the party choice persisted) and C-D4 (§38 carry-over); until both land an arc can remember what the government did but not what the party is. The work is deliberately NOT scoped. Original done-when: the entry says whether it opens now or what remains | CODE | SAFE | R-CL1 |

### Track C — the Playtest-1 remainder

| ID | what | finding | class | depends on |
|---|---|---|---|---|
| C-C1 | P-B1 — yearly budget impact on drafts | 3 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §94).** `EstimateBudgetBill` — two clones, one standing and one with the draft applied through the same delegate a passed bill uses, each run through the model own boundary; a full turn IS a year so the figures are annual with no scaling. ⚠ **The honest range is a POINT** (the preview never rolls an event and is deterministic), with the scope stated instead — and the 1280 film caught the first cut printing `FormatMoneyEstimate`s **randomly rolled** ± above a caption saying "no margin". Diagnostic: no clone escape, untouched draft exactly zero, legs reconcile to 0.0000, direction right, 6 of 6. Films 4 widths, guards silent | — |
| C-C2 | P-B2 — the first-year budget window | 4 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §95). SAFE — the identity HOLDS, and was established where the change lives.** ⚠ A trajectory diff could not have shown it: `TryOpenBudgetProcess` is reached only via `AdvanceCountryDayTick`, whose callers are `GameController.Update` and the capture driver, never `AdvanceDay` — so the trajectory harness never calls the changed method and a clean diff would have passed regardless. Asserted instead by two worlds of day ticks, window forced open against never opened, no bill introduced, every `EconomyState` float: IDENTICAL. Trajectories 6/6 identical, reported as containment only. ⚠ **The measurement also found an off-by-one that cost five countries a YEAR** — the epoch is 1 Jan, five countries budget on the calendar year, and the day tick runs after the date has moved to the 2nd, so the window did not open until 2027-01-01. Now 1 tick for all six; filmed open on 31 January | — |
| C-C3 | P-F1 — the Policy Web's focus mode | first sitting's 3 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §96).** Node dimming (dimmed, never removed — the ring's shape is the structure), direction arrowheads, and the restore gesture (second click / empty space). Thickness scaling and DERIVED/DECLARED already existed. R-W2 kept: no invented edge, no new hue (dimming reuses `PoliSimTheme.Tint`), no legend; provenance survives the arrow; the head is sized FROM the line thickness so it cannot contradict the coupling weight. `PolicyWebCensus` now ASSERTS every weight is real and in [0,1]. ⚠ The 1280 film caught the heads being painted over by the nodes they pointed at. Filmed rest/focused/restored at 1280 and 2560; **rest and restored byte-identical** | — |
| C-C4 | P-G4 — enactment markers on the graphs | 9 (cheap half) | SAFE | — |
| C-C5 | P-C1 — national currency display | 5 | SAFE | — |
| C-C6 | P-C2 — the seed basis (RULED: national units, cross-country views at a sourced vintage-dated rate) | 6 | RULED — executes unless struck; a re-basing is BASELINE | ELIAS may strike |
| C-C7 | P-D1 — central bank independence **+ Riksbank-B** | 7 | **BASELINE** | C-B4 |
| C-C8 | P-E1 — the international browser | 8 | SAFE | — |
| C-C9 | P-G1 — the shadow baseline | 9 (deep half) | SAFE by construction | — |
| C-C10 | P-G2 — the impact ledger | 9 | SAFE | C-C9 |
| C-C11 | P-G3 — the responsiveness audit (**propose, apply nothing**) | 9 (honesty half) | RULING-BLOCKED | ELIAS, per line |
| C-C12 | P-H1 — the tax spec-let (document only) | 10 | SAFE | — |
| C-C13 | P-I1 — the cohort spec-let (document only) | 11 | SAFE | — |

*P-I2 builds only after C-C13 is ruled; it is a DEFERRED row below, not a Track C item.*

### Track D — the elections remainder

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-D1 | W-F4's real path — source SCB per-valkrets marginals and build the voter groups, or bill it precisely and close as billed | the groups are sourced-and-built, or the bill names the exact series. **No derivation from data that does not exist** | CODE | SAFE | — |
| C-D2 | W-F5's pool question — size a playable pool, propose, apply nothing | the finding is a design question with numbers, on record | CODE | RULING-BLOCKED (the resolution is Elias's) | — |
| C-D3 | MP's two språkrör — answer §15/§29, record the ruling, implement it | no screen can state something false about a real party | CODE / ELIAS | RULING-BLOCKED if he wants the call | — |
| C-D4 | §38 long-term political capital, **BUILT** (R-CL3) — persisted party reputation and organisational strength, `SaveVersion` 2 → 3; donor networks specified ABSENT, not invented | the carry-over crosses a save round-trip by party name, and the record states plainly that the electorate does not yet move, so two elections still return the same chamber | CODE | SAFE ⚠ save-layer | R-CL3 |
| C-D5 | V-N3 — the swing column, against the last real result | filmed with two elections behind it | CODE | SAFE | C-D4 |
| C-D6 | The deferral register — one home per deferral | each deferral has exactly one home | CODE | SAFE | — |

### Track R — the ruling executed (R-CL1)

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-R1 | The ruling recorded and its reach stated — only Sweden and Germany have a modelled election; in the other four a party is an identity, not yet a contest | the record says so and the screens say so | CODE | SAFE | R-CL1 |
| C-R2 | Party selection at country selection, persisted as world state | a new game picks a party and the choice survives a save | CODE | SAFE | C-R1 |
| C-R3 | The approval split — personal keeps the existing `ApprovalRating` and every consumer; party approval is a NEW additive stock | the trajectory dump runs and the result is explained, not assumed | CODE | SAFE if additive, **BASELINE** if not | C-R2 |
| C-R4 | The rail cell and the win/lose rule | the eight Track E screens are reachable from the running game, or the one remaining reason they are not is named | CODE | SAFE | C-R3, C-D2 (the war chest) |

### Tracks E, F, G

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-E1 | The trigger shelf re-read — FIRED rows become live, NOT FIRED rows keep their restated trigger | the shelf holds only genuinely unfired triggers | CODE | SAFE | C-B3 |
| C-E2 | The two watch items made standing guards (P4 label-clipping, `MetaTextCheck`) | no watch item sits in a work list | CODE | WATCH | — |
| **C-E3** | **The phantom-guard sweep, armed as a check of its own.** ⚠ Opened by C-B2: `PoliSimTheme`'s doc comment stated *"the desk-seated hues below are checked against the area accents by `PartyInkHarness`"* and **no such file existed** — a constraint the code asserted was enforced, enforced by nothing. C-0.3 found the same shape on the stranded branch (`ThresholdRule.CoalitionShare`, named in `ApplyThreshold`'s contract, read nowhere). Twice in one pass is a class, not a coincidence. **Sweep every doc comment and code comment that names a `*Check` / `*Harness` / `*Diagnostic` and assert each resolves to a type that exists**, then arm it so the class cannot return — a comment that names a guard is a promise the build should keep | every named guard resolves to a real type, the sweep's enumeration is stated in its own header (rule 6), and it runs in the suite. A comment naming a guard that does not exist FAILS | CODE | WATCH | — |
| C-F1 | The Design ask consolidated to ONE paste (D7 + D8 + C-B1's count + C-B2's hexes + §E4's promotion + the §A.14 chip finding + C-C8's gaps + C-D3's question if it needs Design's eye + P-F2's answer) | one ask, one annex set, one regenerated `SEND_PACKAGE` with rows numbered *n of N* and fresh digests; the stale package deleted. **Sending stays Elias's** | CODE | SAFE | C-B1, C-B2, C-C8, C-D3, R-CL4 |
| C-G1 | The document retirement — migrate, delete, and rewrite the document-set table | `ls *.md` matches the table with no orphans and no dangling reference, source comments included | CODE | SAFE | every row above |

### Track N — sized from this pass's own findings

Two measurements in Track A turned up model-level questions too large to answer inside the item that
found them. Each is sized here as its own item rather than left as a paragraph in a closed record.

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| **C-N1** | **The media system cannot move a vote.** ⚠ Established at C-A1 by enumerating call sites, not inferred: **`MomentumTracker.Apply` has exactly two call sites — `CampaignRun.cs:341` and `:451` — and both are the argument to `PollingSystem.Conduct`.** Election day counts `truePreference`, which `CurrentPreference` → `PreferenceModel.Preference` produces and which never sees momentum. So the whole chain **media (W-B9) → coverage → momentum (§22) → poll terminates before the ballot**, and momentum's own doc comment says so: *"shifts where a race APPEARS to be without changing the underlying preference."* The question this item exists to answer is **whether that is the design or an omission** — §13/§14 build a media system whose only route to a result is through a player's or an AI's *reaction* to a poll, and §39's chain gives coverage no persuasive term at all. Two exits, and they are not equivalent: **(a) it is intended** — perception-only media is a defensible model, and then the record must say plainly that no amount of coverage changes a vote, and W-B9's own claims are re-read against that; **(b) it is an omission** — coverage should reach persuasion, which is a §42 chain change with its own explained baseline family | the answer is on record either way, with the call sites cited; if (b), the chain change is sized but NOT built here | ELIAS (the ruling) then CODE | RULING-BLOCKED | — |
| **C-N2** | **The local-action mechanism defect: optimising personalities refuse the action they most prefer.** ⚠ Established at C-A2: the grassroots profile carries door-to-door affinity **2.2** and `EnthusiasmValue` **1.6** — a ~3.5× thumb, the strongest pro-local weighting in the roster — and knocks **zero** doors; both grassroots parties run zero; not one rally is held by any of eight parties; the only personality that acts locally is the chaotic one, which is not optimising. §33 is not blind to local value, so the defect is in the mechanism: the three local actions hold the set's **largest hour costs** (rally 4 h, door 5 h, town 3 h) against its **smallest channel reaches** (0.06 / 0.02 / 0.01) while §33 scores **per hour**, against a free interview (0 kr, 2 h, 0.20) and a 5 000 kr social post (1 h, 0.12). ⚠ **And the door-to-door ACTION is largely redundant with a mechanism that already runs for free** — W-B4's offices knock doors through their own daily operations, outside the eight actions and outside the AI's choice. **The fix is sized separately and is a §12 verb-set question — what is the door-to-door action FOR, given the offices already knock** — not a magnitude to nudge. §34's "no single dominant approach" bar is not met today | the question is answered as a design ruling and the verb set re-derived from it. ⚠ **NO AFFINITY, `EnthusiasmValue`, HOUR COST, REACH OR PRICE IS ADJUSTED** — tuning any of them would bury the finding rather than answer it | ELIAS (the ruling) then CODE | RULING-BLOCKED | interacts with calibration entries 3 and 10 |

---

## 2. Owner ELIAS — nothing in this pass can close these

| ID | what | why no measurement replaces it | source |
|---|---|---|---|
| E-1 | **One paste** — C-F1's regenerated package to Design, then mark §S SENT with the date | sending is Elias's by the §E2 convention | `MISSING_PREREQUISITES.md` §S |
| E-2 | **The sitting** — §V, 52 rows, `../PoliSim-captures/sv_index.html` | a capture is a harness film, not Elias's eyes (rule 3's third layer) | §V |
| E-3 | **Felt verdict 1** — decision density | a staged save, loaded and played | §P |
| E-4 | **Felt verdict 3** — the Trade bill's costs | a staged save, loaded and played | §P |
| E-5..E-24 | **The 20 play-calibration entries** | every one is a number awaiting a loop to judge it against; Track R is what makes that loop exist, the judging is still his. **Nothing here is tuned to make a gate pass** | `ELECTIONS_PLAY_CALIBRATION.md` |
| E-25 | C-C6's basis ruling — **executes as written unless struck** | — | `PLAYTEST_1_WORKLIST.md` |
| E-26 | C-C11's recalibration recommendations — strike or bless, per line | — | idem |
| E-27 | C-C12 and C-C13's spec-lets — ruled before any code | — | idem |
| E-28 | C-D2's pool resolution | a design question, not a measurement | `COMPLETED.md` §83 |
| E-29 | C-D3's språkrör answer, if he wants the call rather than mine | — | the W-F6 finding |

*Verdict 2 ("still not independent") is NOT a row here — it became P-D1 and is discharged by C-C7.*

## 3. Owner DESIGN — waiting, once E-1 is done

| ID | what | note |
|---|---|---|
| D-7 | Board 2b, the Policy Web drawn to be read (the tenth request, Annex G) | written 2026-08-28, **never pasted** |
| D-8.1 | Party identity marks — 52 of 53 undrawn; the seven remaining Swedish ones are what 13 September needs | rule 9a: original art by silhouette, never the registered logo |
| D-8.2 | Party colours for five countries — **a ruling, not art** | Sweden's eight are sourced; we will not pick 30 colours by eye |
| D-8.3 | A drawn valkrets map | the single asset that would most change the campaign map |
| D-8.4 | Election night's paper (V-N1) — a 9-sliced sprite, or a ruling that flat is correct | |
| D-8.5 | The verdict stamp (W-E5) | |
| D-8.6 | Modal or stage for the debate (V-N2) — **a question, not an asset** | |
| D-E4 | The two Society-row icons (youth unemployment, life expectancy) | the two `StatNodeId` members are ours and land under R-CL4; the icons are Design's |

## 4. Owner CALENDAR

| ID | what | date |
|---|---|---|
| K-1 | The seed refresh from Sweden's real result | **13 September 2026** — scheduled, not blocked |

---

## 5. TRIGGER — real work whose trigger has not fired

Nothing here is startable and no named party owes anything. A trigger firing moves the row into §1.

| ID | what | trigger |
|---|---|---|
| T-1 | Per-scenario term accumulation | the first scenario whose epilogue reads wrong without it |
| T-2 | Investment deepening (R-Q5e) | a capital stock ships, or I/GDP measures cyclical |
| T-3 | The identity's government-consumption block | the first mechanic that needs the level output gap to mean something. Measured gaps: USA −14.5 %, Poland −7, Italy −4.5, Germany −2.7, Sweden −0.8, France −0.5 |
| T-4 | Trade volumes indexed to GDP | pass 6's deferred set |
| T-5 | Retaliation against a base-dial hike | idem |
| T-6 | Retaliation memory / lag | idem |
| T-7 | The coupling queue Q6–Q10 | each at its own named trigger; nothing is startable until one fires and Elias rules |

⚠ **Two shelf entries have FIRED and are no longer triggers:** the trade axis for the vote (→ C-B3) and the
Compass Y implemented-average (its trigger — the first play reading the compass against six seeded
portfolios — has happened). C-E1 executes the move. **Riksbank-B was never on the shelf**; it waited on §D
and C-B4 disposes of it.

## 6. DEFERRED — recorded, deliberately not built

| ID | what | trigger or reason |
|---|---|---|
| F-1 | §37 staff progression | deferred 2026-08-30 at W-B5 |
| F-2 | §2's other election types — referendum, leadership contests | beyond national parliamentary |
| F-3 | France's constituency model (R-EL10) | UNSIZED, UNSTARTED. **No placeholder and no approximation is to be built** |
| F-4 | Italy's sub-national stages | billed as before-playable, not before-trusted |
| F-5 | The gap table's nine N/A sections | principle and illustration sections |
| F-6 | P-I2 — the cohort substrate build | builds only after C-C13 is ruled |

*§38 has LEFT this list — R-CL3 rules it built, at C-D4.*

## 7. WATCH — standing guards, never tasks

| ID | what | state |
|---|---|---|
| G-1 | The label-clipping class (P4) | open as a watch under rule 3; instance #14 fixed at `a331e82`; nothing startable until a capture shows another |
| G-2 | `MetaTextCheck` | armed as the ninth check at `1df2917`; scans `Assets/Scripts/UI`, `Assets/Scripts/Simulation/LawCatalog.cs` and `Assets/Scripts/Data` (top level only) |

## 8. Standing gaps and findings that own no item yet

Named so they are not rediscovered as surprises.

| ID | what | where it bites |
|---|---|---|
| S-1 | **The electorate does not move with the simulation.** §8 couples it to the economy; nothing does that yet, so two elections in one game return the same chamber | C-D4's carry-over rides on top of it; C-D5's swing column will show no swing from the model's own play |
| S-2 | Germany 2025 sits on a threshold cliff — BSW missed 5 % by 0.02 pp — so a model with ~1.5 pp of error lands on the wrong side and ninety seats move | reported, never tuned. The weakest point in the seat model |
| S-3 | W-B12's residual: SD keeps 6 of 38 unpaid staff-days | stated, not tuned |
| S-4 | Five of §4's eight axes are UNDEFINED and are NOT centred; `FlatIssueMatch = 0.5` stands in for per-issue positions that exist for no party anywhere | W-F2's bill |
| S-5 | Sweden's TOP issue (EB105: "threats to democracy", 26 %) is not representable in §6; the harness's four issues are Sweden's second through fifth | W-F3's bill |
| S-6 | Sweden 2014 does NOT reproduce through the same allocator (6 seats absolute error) — the reason every "reproduces" claim is scoped to 2022 | C-A4's rule; migrated from the stranded branch at C-0.3 |

---

## 9. Corrections — rows a source document states as open that the repo says are closed

The repo outranks the document. These are recorded, not re-worked.

| what a document says | what the repo says | commit |
|---|---|---|
| `PartyMarkCoverageCheck` reports "PARTY SYSTEM NOT PRESENT — VERIFIED NOTHING" (`MISSING_PREREQUISITES.md` §E2) | real accounting: **53 seeded, 1 resolving, 52 gaps, 0 errors**; R3's verification obligation DISCHARGED | `a289e1e` |
| "no party seeds exist on main"; "count unknown until the seeds land" (§E2) | 53 parties on main; the count is 53, of which 52 are undrawn | `a289e1e` |
| item 10 is the unbuilt spine; the stranded branch is "preserved UNINSPECTED" (§D0) | item 10's core shipped; the branch is inspected and disposed at C-0.3 | `a289e1e`, C-0.3 |
| "nothing was wired and the election system remains unreachable from any gameplay path" (the roadmap's 13 September minimum) | wired; what is true on 13 September is W-H5's status line | `a289e1e` |
| 2a-iv PEND "at 0.291" (the clearance list) | the harness prints est/grass **0.269**, prof/est 0.306 post-W-F1; 0.291 was the pre-W-F1 reading | `806fb17` |
| "measure what pool a playable field needs" (C-D2 as listed) | already measured: the *mandatbidrag* split clears both PEND lines (0.430 / 1.405) **and bankrupts five of eight**; the pool is 2 400 000 kr, one party's budget | `cc95e03` |
| "produce the hex set for every seated party" (C-B2 as listed) | party colour is not a field on `PoliticalParty`; ink lives in `PoliSimTheme` and only Sweden has it | `a289e1e` |
| §E5 is an open ask (the 1i–1n note) | CLOSED end-to-end 2026-08-28, both sides | `COMPLETED.md` §46 |
| §E6's boards are pending | LANDED 2026-08-28 | `COMPLETED.md` §41 |
| the roadmap's document-set table lists eleven root files | the root holds **21** | C-G1 corrects it |
| the gap table's class column: §3, §12, §17, §21, §22 read unbuilt | built at W-B1, W-B3, W-B8, W-B10, W-B10. The honest count is **3 truly unbuilt of 44** — §5, §37, §38 (§38 now built at C-D4) | C-0.2 re-derives |
| `SEND_PACKAGE_2026-08-28.md` states the request doc at 65 004 bytes, digest `85690abf…` | the file on disk is 69 753 bytes — D8 was appended after; **the readback glance it prescribes would fail** | C-F1 regenerates |
| W-F4 and W-F5 are closed items | closed **by STOPPING**, and their real work is live here as C-D1 and C-D2 | `cc95e03` |

---

## 10. Findings opened by this pass

| ID | what | owner | where |
|---|---|---|---|
| S-7 | **`#753838` is drawn for two parties — Sweden's S and V.** Their published `fargkod` values differ only in darkness and the desk seating keeps hue while replacing saturation and value, so a 106-seat party and a 24-seat party render identically in a hemicycle arc, a legend swatch and an election-night row | DESIGN (D8-2) | `COMPLETED.md` §91 |
| S-8 | **Six of Sweden's eight party inks sit inside the derived legibility floor** — closer to an area accent (8.7°, Political/SovereignWealth) than two area accents sit to each other. KD 0.9° from Fiscal, SD 2.8° and L 4.6° from Global. Only C and MP clear it. The constraint the four replaced archetype inks were designed around does not hold for the sourced hues | DESIGN (D8-2) | `COMPLETED.md` §91 |
| S-9 | ⚠ **SIZED AS ITS OWN ITEM 2026-08-31 — see register row C-N2.** **Local campaigning is a bad bet by construction** — the three local actions hold the largest hour costs and smallest channel reaches in the set while §33 scores per hour, so only the non-optimising personality does any. The door-to-door ACTION is also largely redundant with the offices' own daily operations. A fix must answer what the action is FOR, which is a §12 verb-set question | ELIAS / CODE | `COMPLETED.md` §88 |
| S-10 | ⚠ **SIZED AS ITS OWN ITEM 2026-08-31 — see register row C-N1.** **The media system cannot move a vote.** `MomentumTracker.Apply`'s only two call sites (`CampaignRun.cs:341`, `:451`) are both the argument to `PollingSystem.Conduct`; election day counts `truePreference`, which never sees momentum. Media → coverage → momentum → poll terminates before the ballot | ELIAS then CODE | `COMPLETED.md` §87 |
| S-11 | ⚠ **SIZED AS ITS OWN ITEM 2026-08-31 — see register row C-E3.** **A doc comment can name a guard that does not exist.** Twice in one pass: `PoliSimTheme` cited `PartyInkHarness` (no such file, C-B2) and the stranded branch's `ApplyThreshold` named a `CoalitionShare` rule it never read (C-0.3). A comment naming a check is a promise the build should keep, and nothing was keeping it | CODE | `COMPLETED.md` §§86, 91 |
| S-12 | ⚠ **The Estimated Effects panel's "±5-10% margin of error" is AUTHORED and re-rolled at random.** `FormatMoneyEstimate` draws a margin from `_previewRandom` between `MinPreviewMarginPercent` and `MaxPreviewMarginPercent`, multiplies the model's figure by it, and prints the result as `(±$X)` — a random number generated for display, presented beside a measurement. Found at C-C1, which removed it from its own three rows and filed this rather than silently re-cutting a long-standing convention across four macro rows it does not own. Either derive the margin from something the model holds, or state the scope the way the poll's ± states "SAMPLING ERROR ONLY" | CODE (needs a display ruling) | `COMPLETED.md` §94 |
| S-13 | **On a focused STAT node the Policy Web's ~40 incoming edges converge on one point and their arrowheads stack at that node's rim.** Direction is honest there; suppressing it would be deciding the diagram reads better without the model's own direction. A comprehension judgement, so it belongs to board 2b | DESIGN (D7) | `COMPLETED.md` §96 |
