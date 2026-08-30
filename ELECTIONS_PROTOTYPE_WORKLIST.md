# PoliSim — Elections Prototype: the work list

**Elias, 2026-08-29.** Build a **playable prototype of the election and campaigning system** —
model, gameplay, and graphics — from `ELECTIONS_CAMPAIGN_SPEC.md` (44 sections, at root, verified)
and the sourced spine already on disk. This document replaces kickoffs: it is the whole list, in
execution order, every item with a **done-when**. Work down it. A session takes as many items as
its budget allows, stops at an item boundary, reports, pushes, and the next session resumes at the
first unfinished ID. No further instruction is needed between items.

---

## 0. Standing terms (read once; they govern every item)

- **0.1 Prototype target: Sweden.** The playable country is Sweden — 349 seats, 29 constituencies,
  allocation verified exact, returns sourced, and the real election on 13 September. The
  *architecture* stays country-general (every rule set, catalog and formula takes country as data),
  but only Sweden is populated to playable depth. Other countries remain backtest-only until their
  data lands. *Strikeable: if you want a second playable country, say which; it is a data item, not
  a rebuild.*
- **0.2 Playability and predictive fidelity are different bars.** The backtest gate (R-EL13)
  governs **claims about accuracy**, not whether the prototype ships. A failing gate does not stop
  this list; it is reported, the affected figures are labelled, and **the constant is never tuned
  to open it**. Prototype quality = gameplay coherence, determinism, and honesty of display.
- **0.3 R-N2 retires at W-G1, and only there.** Everything before W-G1 stays unwired and
  byte-identical. Wiring is one isolated commit whose hash is the revert handle.
- **0.4 Data classes, unchanged:** SOURCED (real-world, primary source + vintage + basis),
  `[AUTHORED-DRAFT]` (game fiction — action costs, candidate attributes, staff bonuses; permitted,
  logged one line each, calibrated later by play), DERIVED (computed). **No spec illustration
  number ships as data.** No invented demographics, ever.
- **0.5 Graphics rule.** Campaign UI is built **structurally now** in the existing v3 idiom (rail,
  one full-bleed sheet, ledger rows, instruments, stamps, existing sprites) — the R-E2 precedent:
  affordances ship, Design re-skins. Every new screen is filmed at 1280/1600/1920/2560, guards
  silent, `ScreenEdgeCheck` clean. **No new sprite is invented**: a gap becomes a line in the
  Design ask (W-H4). The five `mark_party_*` sprites currently drawn by nothing are the party
  marks — use them.
- **0.6 Determinism.** Every draw through `SimulationRandom`; `ElectionNoise` is the stream; new
  streams append only. Films seeded (`FilmSeed`), cursor parked. Any AI or event randomness gets
  its own named stream, appended.
- **0.7 Per item:** one commit, staged by path, descriptive message; harness or capture proof as
  the done-when states; suite green; R-SP1 fast-forward push at each item or coherent group.
- **0.8 Reversible forks:** decide, log one strikeable line, continue (R-N1). Irreversible or
  spec-contradicting forks stop that item, not the list.

---

## Track A — close the model (the gate's unfinished business)

**W-A1 — Derive loyalty from volatility.** Replace the global loyalty constant with per-party,
per-country loyalty derived from the previous two elections' returns (Pedersen-style volatility per
party; low volatility → high loyalty). Sourced input, zero authored constants; the derivation
documented with its formula. *Done when:* the derivation harness reproduces a loyalty value per
party for all six countries from returns already on disk, and Italy's FdI/M5S compute visibly low
loyalty from the 2018→2022 pair.

**W-A2 — Per-region priors.** §8 and §27 currently fail to compose because the regional run damps
toward the national prior. Give each region its own prior from that region's own historical
returns. *Done when:* Germany's both-layers run is no worse than its §8-only run, and the
composition is proven on at least two countries.

**W-A3 — The gate re-run, unchanged.** Same four countries, same declared parameters, same
no-regress rule. Report Day-1 → Day-2 → Day-3 MAD per country. *Done when:* the table exists with
an explicit PASS/FAIL and no parameter was re-fitted to produce it. **Either verdict continues the
list** (0.2).

**W-A4 — Tactical voting, threshold form (§23).** Sweden's 4% threshold produces real
support-voting. Model tactical switching as a function of threshold proximity in current polling ×
voter strategic awareness × bloc affinity. *Done when:* a party polling 3.5–4.5% shows measurable
support inflow, the effect vanishes in the absence of a threshold, and 2022's actual near-threshold
behaviour is reproduced no worse than without the layer.

**W-A5 — Government performance → perceived performance (§19).** The actual-vs-perceived split
already exists in `PublicationSystem`'s preliminary/revised figures (gap table: EXISTS). Wire the
vote model to read **perceived** economy, not actual. *Done when:* a run where published figures
lag reality shows the vote model tracking the published series, with the divergence visible in the
attribution ledger.

---

## Track B — the campaign layer (gameplay)

**W-B1 — Campaign clock and calendar (§3).** A campaign period on the game's existing day clock:
pre-campaign → campaign → election day. Sweden's real window (final 8 weeks) as the prototype
default. Phases gate which actions are legal. *Done when:* a harness advances a full campaign
day-by-day, phase transitions fire on their dates, and the existing turn loop is untouched
(still unwired).

**W-B2 — Resources (§9).** Money (from party funding, sourced where possible, otherwise
`[AUTHORED-DRAFT]` with the line), time budget per campaign day, volunteers. Spending with
**diminishing returns (§35)** as a declared curve, not a table of magic numbers. *Done when:* the
resource harness proves the curve's shape (first krona ≫ millionth) and no resource can go
negative.

**W-B3 — Campaign actions, the eight (§12).** Rally · town hall · door-to-door · TV ad · digital
ad · social post · interview · policy announcement. Each: cost, time, targeting (region and/or
voter group and/or issue), and an effect that flows through §42's chain — **never a direct vote
delta**. *Done when:* each action's harness shows its effect arriving via reach → salience →
exposure → relevance → persuasion/enthusiasm → preference, and a unit test proves no action writes
a vote share directly.

**W-B4 — Campaign offices (§10).** Regional offices with cost, capacity, influence, maintenance.
*Done when:* offices measurably change regional door-to-door reach and GOTV, and concentration in
few regions beats spreading thin in a measured scenario.

**W-B5 — Staff (§9, prototype depth).** A small roster (manager, media advisor, pollster, field
organizer, digital strategist) with `[AUTHORED-DRAFT]` bonuses. **§37 progression is deferred** —
record the deferral. *Done when:* hiring changes the relevant action's effectiveness and the
payroll appears in the resource ledger.

**W-B6 — Campaign strategy (§11).** The five strategies as modifiers over the whole chain. *Done
when:* each strategy's harness shows its stated trade-off (base mobilisation lifts loyal turnout
and lowers swing persuasion, etc.) with no strategy dominating in a measured sweep.

**W-B7 — Debates (§15).** Preparation choices, then in-debate decisions; performance from candidate
attributes × preparation × opponent × issue ownership × a seeded draw. *Done when:* the same seed
and choices reproduce a debate exactly, and performance moves media coverage and momentum rather
than vote share directly.

**W-B8 — Scandals (§17).** Dynamic scandals with severity and the seven responses; consequences
depend on evidence state, so denial can pay or ruin. *Done when:* a scandal's full lifecycle runs
deterministically under a seed, each response has a measured distinct outcome distribution, and
nothing is scripted as game-over.

**W-B9 — Media system and outlets (§13, §14).** Outlets with audience segments; coverage as a
derived function of campaign activity, events and performance; momentum with **diminishing
returns** so it cannot spiral. *Done when:* a coverage spike decays on the measured curve and the
same message measurably performs differently by outlet audience.

**W-B12 — DONE 2026-08-30 (four of five managed parties at zero unpaid staff-days; SD keeps 6 of 38, stated, not tuned). The campaign manager's FULL cost plan (§9; opened by ruling 2026-08-30 from the
W-C2 review).** W-B5 built the manager's `BudgetPlan` over television alone, and W-B5's finding 2
measured the consequence: offices, their operations and the payroll are fixed daily costs the
spending pace does not see, so **every party in the AI campaign goes broke before polling day** (of
120 staff-days: SD 38 unpaid, V 12, S 10, M 6). The plan must cover **every fixed cost — staff,
offices, maintenance, travel — not television alone: pay the organisation first, release the rest.**
This is a **playability requirement and must NOT inherit W-F5's data dependency** — it needs no
sourced funding figures, only a rule over costs the model already charges. *Done when:* a managed
party pays its organisation to polling day with zero unpaid staff-days in the C1 staging, the pace
releases only what remains, and C1's 2a-iii (professional / establishment) is re-measured and
reported either way — never tuned to separate.
*Order:* **after W-E8 and BEFORE Track F** — the remaining run is D3 → D4 → E5–E8 →
**B12** → F (as data arrives) → G1–G4 → H.

**W-B10 — Polling (§20–22).** Public polls with sample, MoE, field date and house effects;
purchasable internal polling at better precision; momentum as a moving average with decay. *Done
when:* polls never exactly equal the underlying truth, the MoE is honest (coverage tested over
replays), and the poll object is what the UI reads — the UI never sees the true preference vector.

**W-B11 — GOTV (§26).** Turnout as base × engagement × mobilisation × enthusiasm × salience, with
phone banking, door knocking, transport and election-day operations. *Done when:* mobilisation
spending measurably moves turnout in targeted regions only, and the national turnout stays inside
historically plausible bounds.

> **STANDING DESIGN QUESTION against W-B4 / W-B11 (opened by ruling 2026-08-30, W-C2's
> measurement; NOT a closed finding, and nothing to be pre-emptively adjusted).** W-C2 measured that
> the rational personalities scarcely campaign locally at all: over ten 60-day campaigns the
> professional made **0.9 local acts a campaign** and the establishment **0.8**, against the chaotic
> party's **59.8** — and 8 of the professional's 9 were the reaction RULE sending it, not its own
> weighing. Now that local reach exists as an absolute count (W-B4's organisation, W-B11's
> volunteer-bound doors), **re-check whether professional and establishment AIs still avoid the
> ground game, and report WHICH of the two causes it is, with the measurement:** either the MODEL
> underpowers local action (a region's audience against a national one's for the same hours) or the
> AI's EXPECTED-VALUE function undervalues local reach (§33's weighting). §34 says the design must
> not reward a single dominant approach, so this decides whether a **mechanism** or a **weighting**
> needs work. Do not adjust either before the measurement says which.

---

## Track C — opponents

**W-C1 — AI parties (§32, §33).** Every AI runs the same system the player does. Five
personalities: professional, populist, establishment, grassroots, chaotic. Action choice by
expected value − cost − risk. *Done when:* an AI-only campaign completes deterministically, the
five personalities produce measurably different action mixes, and no AI accesses hidden state the
player cannot buy.

**W-C2 — Opponent reactivity.** AI responds to the player's moves (attacks answered, contested
regions defended) within its personality's tempo. *Done when:* a measured scenario shows a
professional AI reallocating to a threatened region and a chaotic one not.

---

## Track D — election day and after

**W-D1 — Election-day simulation (§27).** Per region, per voter group: population × eligible ×
turnout × preference, aggregated, plus noise on `ElectionNoise` at its measured 1/√n behaviour.
*Done when:* the same seed reproduces a result exactly and 400 replays show the noise distribution
matching its declared σ.

**W-D2 — Vote-to-seat (§28).** Already exact for five chambers — connect it to the live vote
model. *Done when:* Sweden's 2022 returns still reproduce seat-for-seat through the live path, not
just the backtest path.

**W-D3 — Coalition formation (§29).** Sweden's bloc reality: compatibility, red lines, seat
strength; outcomes minority / majority / confidence-and-supply / new election. *Done when:* the
2022 seat distribution produces a plausible government set, and red lines demonstrably block an
otherwise-arithmetic coalition.

**W-D4 — Post-election attribution (§31).** "Why you won/lost", built as the approval attribution
ledger pointed at vote share — the existing nine-term instrument's idiom, every line derived and
summing to the total. *Done when:* the attribution lines sum to the actual deviation from baseline
within a stated tolerance, and no line is authored prose.

---

## Track E — the screens (structural, v3 idiom, filmed)

**W-E1 — Campaign HQ.** The campaign's landing sheet during a campaign period: resources, days
remaining, current polling instrument, the action queue, staff and offices at a glance. Rail gains
a campaign cell (existing icon set; a gap becomes a Design line). *Done when:* filmed at four
sizes, guards silent, edge checks clean, and every number on it is derived.

**W-E2 — The campaign map.** The existing map instrument as a regional support choropleth with the
swing index (§25) — but **swing detail is gated on polling investment** (§36): unbought regions
read as uncertain. *Done when:* the map renders 29 constituencies, uncertainty is visually distinct
from data, and buying polling visibly sharpens it.

**W-E3 — Action screen.** Choose action, target, budget, message; the live estimate in the
established idiom (the law browser's estimate convention), showing **ranges, never false
precision**. *Done when:* filmed, and the estimate's displayed uncertainty matches the model's
actual uncertainty.

**W-E4 — Polling screen.** Public polls as the graph instrument at 1l's weights, with MoE bands,
house effects, and the internal-vs-public split. *Done when:* filmed, and the PRELIMINARY/revision
conventions are honoured.

**W-E5 — Debate screen.** The debate's choices and running state; a modal on the v3 sheet.
*Done when:* filmed in at least three states (prep, mid-debate, verdict).

**W-E6 — Election night.** The Canvas screen 1h slot, reserved for this since v2: results arriving
by constituency over time, seats filling, the call. *Done when:* filmed in four states (early,
partial, called, final) and the arrival order is deterministic under seed.

**W-E7 — Results and attribution.** §30's breakdown plus W-D4's ledger: totals, seats, turnout,
regional table, gains/losses, and why. *Done when:* filmed, and every figure traces to the model.

**W-E8 — Coalition screen.** Negotiation state, red lines, arithmetic. *Done when:* filmed in at
least three outcome states.

---

## Track F — the data Sweden needs (sourced; blocks playability, not the build)

**W-F1 — DONE 2026-08-30** (all 29 valkretsar x 8 parties in absolute counts + eligible + cast, from Valmyndigheten's per-constituency backend; eleven column sums exact; nine consumers repointed, two deliberately kept on 2018 as the backtest's PRIOR; W-D2's seat-for-seat claim now rests on the real chamber and KD's fixed seats moved 10 to 13). — 2022 returns by constituency (Valmyndigheten), all 29, per party. **W-F2** — party
positions (CHES 2024, already on disk) mapped to the spec's axes, mapping stated. **W-F3** —
issue salience (Eurobarometer / SOM Institute, vintage recorded). **W-F4** — voter groups derived
from existing demographic seeds per region; only underivable marginals get sourced; nothing
authored. **W-F5** — party funding and campaign spending (party accounts / Kammarkollegiet) or
`[AUTHORED-DRAFT]` with the line. **W-F6 — DONE 2026-08-30** (eight leaders sourced to each party's own site via dated archive captures; attributes stay authored; MP's two sprakror billed as a finding). — candidates: real party leaders by name (public
figures, factual), attributes `[AUTHORED-DRAFT]` and clearly labelled as game fiction. *Done when:*
each has source, vintage, basis, and the remainder is billed.

---

## Track G — wiring (the invariant retires here, once)

**W-G1 — DONE 2026-08-30** (PartyArchetype retired for 53 real parties across six real chambers; ElectionRecord persisted in the World layer; two of six countries hold a real election and four state why not; ElectionDayReachDiagnostic ALL PASS. ⚠ THE RAIL CELL IS NOT ADDED and the election does not decide whether the player won - BOTH are blocked on one design question, "who is the player in party terms", which W-G1 is not entitled to answer. See COMPLETED.md 79.) — Wire, isolated. One commit behind a single entry point; the D0 collision map executes
(`PartyArchetype` retires, `TotalSeats = 200` yields to real chamber sizes, `ElectionSystem`'s
approval threshold yields to the vote model; `PublicationSystem` stays the polling substrate; seat
drift, bill scoring, renderers stay). *Done when:* the commit hash is printed as the revert handle
and the game reaches election day from a new game without a crash.

**W-G2 — New baselines, explained.** Trajectories change; that is what wiring means. Capture
`traj_wired_*` as a new family, keep the old, and explain every difference per country by layer.
*Done when:* no unexplained difference remains.

**W-G3 — Saves.** New state joins `SaveGame` the way `FedChairCandidates` did; pre-wiring saves
still load with state absent → defaults. *Done when:* a round-trip harness proves both directions.

**W-G4 — Full capture matrix and suite** on the wired code, four sizes, rule-15 diff (now exact,
`det_*` family). *Done when:* green, with any new label-clipping instance treated as the known
class.

---

## Track H — records, asks, and the honest close

**W-H1 — Records per item** as they land: COMPLETED sections, CLAUDE entries, the roadmap's
E-phases re-derived, the gap table's rows discharged.
**W-H2 — §V rows** for every new screen with its capture name, so the sitting is one pass.
**W-H3 — The call log:** every `[AUTHORED-DRAFT]` value and every reversible decision, one
strikeable line each, in one place.
**W-H4 — The Design ask** (one, at the end, not per screen): every screen built structurally,
every sprite gap found, drawn against the films — the eleventh request. Sending stays Elias's.
**W-H5 — The honest status line** for 13 September: what is playable, what is provisional, what
the backtest says — written plainly, in the report and in the roadmap.

---

## Execution order

A1 → A2 → A3 → A5 → B1 → B2 → B3 → B10 → E1 → E3 → E4 → C1 → B6 → B9 → B11 → D1 → D2 → E2 →
B7 → B8 → A4 → B4 → B5 → C2 → D3 → D4 → E5 → E6 → E7 → E8 → F(as data arrives, in parallel) →
G1 → G2 → G3 → G4 → H(throughout, H4/H5 last).

Rationale: the model closes first, then the minimum loop the player can *feel* (clock → resources
→ actions → polls → the three screens), then opponents, then depth, then the end-game and its
screens, then wiring last so everything that goes live has already been proven unwired.

## Stop conditions

Stop the **item**, not the list, and report: an irreversible fork; a spec contradiction; a needed
figure that is neither sourceable nor honestly authorable; a validation failure that is not the
item's own bug. Stop the **list** only if: the existing game's trajectories change before W-G1, or
W-G2 shows an unexplained difference.

## Deferred, deliberately (record; do not build)

§37 staff progression · §38 long-term capital beyond party reputation carry-over · election types
beyond national parliamentary (§2) · France's constituency model · Italy's sub-national stages ·
referendum and leadership contests · the nine sections the gap table ruled N/A.

