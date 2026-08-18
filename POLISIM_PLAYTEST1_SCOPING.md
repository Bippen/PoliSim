# Playtest 1 — five items, scoped only (2026-08-18)

Five of the seven playtest findings, per instruction: **scope only, nothing built**. (The other
two — the rejected-bill seal, the Budget tab's dead nested scroll — are fixed and recorded in
`CLAUDE.md`'s "First real playtest session" entry.) Each section below is backed by a dedicated
research pass — file:line citations throughout are from that research, not assumption.

## Recommended order

**Turn → Year, Calendar, Decision density, Portraits, Law system** — matching the order argued
in the instruction, which the research below confirms rather than revises:

1. **Turn → Year first.** It is a display-layer change (below) that touches almost every screen
   the other four will also touch (the calendar's own date furniture, decision-count language,
   portrait captions, every law's cost/duration text). Doing it after any of the others means
   re-touching the same surfaces twice.
2. **Calendar second.** Reads data that already exists (division log, publication release rules,
   the approval ledger); its own cost is a fresh widget plus a placement decision, not a data
   layer — cheap relative to the remaining three, and its "what day is it" furniture is exactly
   what Year-vocabulary work will have just touched.
3. **Decision density third.** A measurement plus a ruling, not a build. Below: the automatic
   (non-bill) prompt rate is derived from the model's own constants at roughly 5/year — see the
   ruling for what that implies. It is genuinely informed by item 5: if the diagnosis is "content
   inside each decision, not frequency of decisions," the law system is the answer, and this
   section's ruling should say so rather than propose a separate pacing fix.
4. **Portraits fourth.** Not really schedulable — it is gated on one Editor session of Elias's
   own, zero engineering work either side of that verdict. Listed here so it isn't dropped, not
   because it competes for build order with the other four.
5. **Law system last, and largest.** 50+N laws per category is a marathon; its own section ends
   with a minimum viable slice, because the architecture has to be proven on one category before
   any of that marathon starts. Everything above it should land first specifically because the
   law system's own UI (a law's cost/duration text, its place on a calendar of pending bills) will
   read those surfaces' finished vocabulary rather than being built against a moving target.

---

## 1. Turn → Year — display truth-telling, confirmed cheap

**The premise is exact, not approximate.** `DaysPerTurn = 365` (`SimulationManager.cs:117`), and
`YearsPerTurn` (`MacroSystem.cs:1345`, `= 4f / ElectionSystem.ElectionCycle`) evaluates to exactly
`1.0` — the model's own two independent statements of a turn's length agree exactly, per
`Phase4YearsPerTurnDiagnostic`. **One turn is one year, precisely, not "close enough to round."**

**41 distinct player-facing "turn" sites, enumerated project-wide** (28 `GameController.cs`, 2
`GraphRenderer.cs`, 7 `PolicyWebRenderer.cs`, 1 `StatTracePanel.cs`, 1 `ScenarioLibrary.cs`, 1
`ScenarioEvaluator.cs`). **~39 are pure display-layer swaps** — a format-string substitution, no
numeric transformation, because turn already equals year: dashboard header
(`GameController.cs:2474`), scenario verdict text (`ScenarioEvaluator.cs:167-169` — `EndTurn` IS
shown to the player raw, via `VerdictReason`, exactly once), election/game-over banners, graph
titles, save-menu rows, PolicyWeb node detail lines, the one StatTracePanel empty-state line.

**Exactly two sites need more than a formatter**, both small and contained:
- `ScenarioLibrary.cs:248`'s `keep_the_room` Description hand-authors "10 consecutive turns" as
  English prose — a content edit (to "10 consecutive years"), not a formatter, though the number
  itself (`RequiredTurns = 10`) does not change.
- `GameController.cs:1026`'s default save-file-name suggestion bakes `turn{N}` into a filename
  players may keep — a naming-convention decision, not a logic risk (`SanitizeSaveName` never
  parses the number back out of the string).

**No structural landmine found.** Turn is never used as an array index, a save-slot key, or an ID
in production code (the one `a[turn]` site is an Editor-only diagnostic snapshot array). Nothing
computes a year from a turn expecting turn to be a sub-year unit — every derived constant is built
*from* `DaysPerTurn`/`ElectionCycle`, never the reverse.

**Proposal: the boundary, not a rename.** Player reads years; every instrument (fields, save
schema, `CurrentTurn`, `EndTurn`, diagnostics, `CLAUDE.md`'s ~1019 "turn" mentions) keeps its name
exactly as it is. A full rename would touch the internal vocabulary this whole project's design
record is written in, for zero functional gain — the value here is entirely in what the player
reads, and that is roughly 39 single-line formatter swaps plus the two small edits above. Wrap
`CurrentTurn`/`EndTurn`/`RequiredTurns`/`ConsecutiveTurns` display sites in one shared "Year N"
formatter (a single new helper, called from ~39 sites) rather than hand-writing the string at each
call site — cheap to get consistent, cheap to revisit if the wording changes once.

**One small fork worth a ruling, not resolved here**: "Year N" as an ordinal (Year 12 of your
term) or the real calendar year (`CurrentDate.Year`, already computed and shown on the calendar
pad next to the turn number)? Duration/streak text (`"held for N of M required turns"`) clearly
wants the ordinal form regardless. The dashboard header could read either way. Worth Elias's call,
not a blocker — both forms use data the model already has.

---

## 2. Calendar panel — mostly readable from data that already exists, with one real cost

**What the current left column draws, precisely** (`GameController.cs:1822`–1870, guarded off
entirely on the Budget tab already — a real precedent for "this space can be conditionally
something else"): the event banner (`DrawTopBanner`), the dashboard header + headline stat-tile
grid (`DrawDashboard`), and the policy-preview panel (`DrawPolicyControls`) — all inside the
scrollable region — plus, pinned OUTSIDE the scroll so it's visible regardless of scroll position:
`DrawCalendarPad` + the pause/speed status strip. **A calendar replacing this column would
displace three real, actively-used panels**, not empty space — this is the section's one real
cost, and it is not small.

**`DrawCalendarPad` itself is a dead end to extend** (`GameController.cs:3629`–3654): it renders
`CurrentDate.Month`/`.Day`/`.Year` fresh every frame — no grid, no memory of any other day, no
marking, no events. A month-view calendar needs a genuinely new widget, not a grown version of
this one.

**What a calendar's "events" layer CAN read directly, without a new store:**
- `DivisionLog`/`DivisionRecord.Date` — a real `DateTime` per resolved bill, already written at
  8 call sites (`ParliamentSystem.RecordDivision`).
- `ApprovalEventRecord.Date` — every labeled approval-moving event (bills, cabinet, foreign
  policy, reshuffles), already dated.
- `ReleaseCalendar.IsReleaseDay`/`GetGdpRevisionStage` (`ReleaseCalendar.cs:23`–107) — genuine,
  forward-computable calendar arithmetic (first Friday of month, t+30/60/90 days, fiscal-year
  start), explicitly "pure date arithmetic... no simulation state read or written" per its own
  doc comment. This is the richest source found — it can name *future* dates, not just past ones.
- `FiscalYearData.GetFiscalYearStart` — a recurring `(Month, Day)` per country, for marking the
  annual budget-process date.

**What it CANNOT show, and this is worth stating plainly rather than discovering it mid-build**:
Cabinet decisions and Foreign Policy meetings carry **no date field of any kind** — they are pure
probability-per-turn/per-day rolls with nothing stamped when they fire
(`CabinetSystem.TryRollDecisions`, `SimulationManager.TryRollForeignPolicyMeeting`). A calendar can
show *when a past one happened* (via the approval ledger) but can never show *when the next one
is coming* — the model itself does not know that in advance. Any calendar mock that implies
otherwise is promising something the mechanism can't deliver.

**Proposal: grow `DrawCalendarPad` itself, not the whole column.** A month-page widget in the same
pinned slot the pad already occupies (bigger, but still outside the scroll, still visible on every
tab except Budget by the same existing precedent) sidesteps the three-displaced-panels cost
entirely, while still delivering the ask (days marked as they pass, real dated events layered on
top from the sources above). Replacing the whole left column is a larger, separate proposal this
research does not recommend defaulting to — it would need its own relocation plan for the three
panels first.

---

## 3. Decision density — measured from the model's own constants, ruled

**Measured, not assumed** (analytically, from the constants themselves — a live simulated count
would sharpen the Foreign Policy figure specifically, noted below, but the order of magnitude is
not in question):

| source | rate | derivation |
|---|---|---|
| Foreign Policy meeting | ≈3.65/year | `MeetingChancePerDay = 0.01` (`ForeignPolicySystem.cs:32`), rolled daily, 365 days/year |
| Cabinet decision | ≈0.12 × (appointed ministers)/year | `DecisionChancePerTurn = 0.12` (`CabinetSystem.cs:48`), per appointed minister, once per turn |
| Annual budget process | 1/year, certain | `TryOpenBudgetProcess` fires on the country's own fiscal-year-start date, not probabilistic |
| Fed Chair term | 0.25/year, USA only | `ElectionCycle = 4` turns; the Eurozone three share the ECB, no event at all |

**≈5 automatic (non-bill) prompts per simulated year** for a USA game with a handful of appointed
ministers — roughly one every 2.4 months. Bills are excluded from this count deliberately: they
are pure player agency (up to 8 categories, no fixed rate, gated only by `BillDurationDays = 21`
per active bill), not something that "fires" on its own.

**Ruling: the gap reads as content, not pacing — and the law system is positioned to answer it
without touching this table at all.** ~5 unprompted interrupts a year is not a thin cadence, and
none of the four found sources are obviously under-tuned enough to justify a blanket frequency
multiplier (the per-scenario `ForeignPolicyCadenceMultiplier` already exists exactly for the one
case that's ever needed adjusting, per Step 3). What every one of these four sources shares is a
SMALL, FIXED option set once it fires — Cabinet/Foreign-Policy offer a handful of pre-authored
choices per meeting, and a bill is one dial-bundle vote. **A law system does not increase how
often the game interrupts the player — it increases how much is worth doing at each of the
interrupts already there** (a browsable menu the player visits by choice, not a new probabilistic
source), which is a different lever than pacing and the one this data says is more likely to be
the actual gap. No pacing change recommended pending a live-measured cross-check of the Foreign
Policy figure specifically, if this becomes live work.

---

## 4. Portraits (D1) — status only, not schedulable against the other four

**1 of 9 delivered and already imported/wired** (`portrait_cabinet_defense_katarzyna_ekelund`, a
512×640 opaque painted-plate proof). **8 remain, withheld on purpose** pending one verdict:
whether that register (opaque plate) reads as an upgrade or a mismatch next to the sixteen
existing 256×256 transparent-bust portraits, judged inside `ui_portrait_frame` at two window
sizes. The two comparison shots are fully specified (`CLAUDE.md`'s "THIRD ENTRY... the §5 portrait
REGISTER side-by-side") and need zero prep — the PoC resolves automatically through
`IconLibrary.GetCabinetPortrait` the moment Defense's Reformist is appointed via the Cabinet tab.

**Unblocks on**: Elias, in a real Editor session, entering Play mode and running that one
comparison. **Nothing on the engineering side is missing** — `GetCabinetPortrait`/
`GetFedChairPortrait` (`IconLibrary.cs:140`–161) already degrade gracefully to the existing
procedural silhouette for any name without art, and every draw call site (roster, candidate list,
Fed Chair picker) already reads through them.

**If more than nine are wanted**: no process for that exists in any of the four standing
documents (`CLAUDE.md`, `POLISIM_MASTER_ROADMAP.md`, `CLAUDE_DESIGN_ASSET_REQUEST.md`,
`MISSING_PREREQUISITES.md` — searched directly, no hits). It would be a fresh Design request
behind the same access gate, not an extension of the current one.

**Why it's listed fourth rather than genuinely ordered against the other three**: it isn't
buildable work at all right now — it's a single verdict Elias has to render himself. Nothing about
sequencing Turn→Year/Calendar/Decision-density before or after it changes when that verdict
happens.

---

## 5. The law system — the big one

### What a law needs to be, as data, to not cost 50×N bespoke simulation effects

**The existing dial space is the load-bearing insight here.** Today's model already has 19 real
0–100-ish dial fields (Labor 6, Crime 6, Sectors 5×8 instances, Trade 2) plus `TaxRateOverrides`/
`WelfareGenerosityOverrides`/`SpendingLineChanges` — a continuous policy surface that already
covers most of what a "law" would want to move. **Recommended shape: a law is a NAMED, CURATED
PRESET over that existing surface** — "Three-Strikes Sentencing" sets `SentencingSeverity` to a
specific value with its own name, flavor text, and cost, rather than a bespoke new simulation
effect written once per law. This is the Democracy 4 idiom too, underneath its own UI: named
policies moving an existing stat space, not a parallel mechanism per policy. **This is the
question the MVP slice below exists to prove** — if most of a category's first handful of laws
can be expressed this way, 50×N is an authoring-cost problem (data entry), not an engineering
one; if a meaningful fraction need genuinely new simulation surface each time, that changes the
whole cost estimate and is worth knowing on ONE category rather than after fifty.

### How a law reaches Parliament — reuse, not a new mechanism

The full Crime & Justice bill path was traced end-to-end (draft → `BuildXBillFromDrafts` →
`IntroduceXBill` → daily countdown → `WouldBillPass`/`GetSeatWeightedAlignment` →
`RecordDivision` → `ApplyXBillResult` → the `ApplyXBillEffects` delegate). **A law's own vote path
is the same shape**: a `WouldBillPass`-scored direction, `RecordDivision`, an
`ApplyLawBillResult`/`ApplyLawEffects` pair reusing the EXISTING clamp-owning `Apply*Changes`
methods the sliders already call. The `Apply*BillResult` methods average 17 lines each; the
`Apply*BillEffects` delegates average 15 — this is genuinely cheap plumbing per category, already
proven eight times over.

**One real fork, not resolved here**: today's model allows exactly ONE pending bill per category
at a time (`_pendingCrimeJusticeBillByCountry` is a single-bill dictionary keyed by country). A
browsable menu of 50+ laws implies a player might want several introduced/pending at once within
one category — a genuine divergence from the current "one bill per tab" constraint that needs its
own ruling before the MVP is built, not assumed either way here.

### How a law reaches the legibility ledger

Trivial, and already proven eleven times: snapshot `ApprovalRating` before applying the law's
effect, apply it, call `ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, "<label>", delta)`
— zero-delta calls are silently dropped, so a law with no approval cost needs no extra guard, and
no political-system signature needs to change. Every law that moves approval is legible for free
by following the pattern already in `SimulationManager.cs` at all 8 bill-resolution sites.

### What happens to the sliders

**Recommended: coexist, not retire.** Continuous-rate levers (tax rates, subsidy percentages)
don't obviously want a curated-menu framing — a rate is a rate. The sliders stay the fine-tuning
layer; laws become curated, named PACKAGES that set one or more dials at once with their own
flavor, cost, and (eventually) prerequisites. Retiring sliders in favor of laws-only is a larger,
separate design decision this scoping does not recommend defaulting into.

### Save-shape and authoring cost

**Additive, no version bump** — one new `List<EnactedLaw>` field directly on `Country` (mirroring
`Divisions`' own shape: an id, a `DateTime`, whatever magnitude the law's own effect needs),
following the project's own already-ruled convention that additive model changes need no
migration. **Scale for comparison**: `ParliamentSystem.cs` is 592 lines, `SimulationManager.cs` is
3288; today's 8 bill types each bundle a HANDFUL of dials behind ONE vote — the authoring cost so
far has been paid once per *category*, not once per *dial*. A 50-laws-per-category browser instead
pays a per-*law* cost (id, name, description, target dial(s)+value(s), cost, prerequisites) — a
genuinely different granularity, which is exactly why the MVP has to measure it on real laws
rather than extrapolate from the bill-type cost above.

### The minimum viable slice — the actual deliverable before any content marathon

**One category (recommend Crime & Justice — its full path is already traced above, and named laws
read naturally there: "Three Strikes," "Body-Worn Cameras," "Decriminalize Cannabis" — more than a
raw tax-rate slider ever will), a handful of laws (5–8), end-to-end:**
- Laws authored as data: a `LawId` enum plus a small static table (name, description, target
  dial(s)/rate(s) and value(s), approval cost, one-time fiscal cost if any).
- A browsable UI screen (Democracy 4's list idiom) presented alongside or in place of Crime &
  Justice's current 6 dials.
- Wired through the EXISTING `WouldBillPass`/`RecordDivision`/`ApprovalLedgerRecorder.RecordEvent`
  chain, reused verbatim — no new political-system code.
- The save-shape addition (`List<EnactedLaw>` on `Country`), proven through one real save/load
  round trip.

**This slice's job is answering the open question above** (how much of a real law-set fits the
preset-over-existing-dials model vs. needs bespoke effects) **before fifty more laws are
authored on the strength of an assumption.** Recommend this is where the law system's own ruling
package gets written, once Turn→Year/Calendar/Decision-density have landed and this slice's own
architecture question has a measured answer.
