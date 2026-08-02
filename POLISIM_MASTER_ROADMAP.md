# PoliSim — Master Roadmap

This replaces three previously-separate standing documents (`ROADMAP_BRIEF.md`, `CONTINUOUS_TIME_MIGRATION.md`, `POLITICAL_SYSTEMS_OVERHAUL.md`), which had grown real dependencies on each other without being coordinated in one place. Read this in full before starting anything.

---

## Non-negotiable working discipline (applies to everything below, no exceptions)

1. **Real Unity is the standard of truth, not the standalone harness.** It has been wrong about project state multiple times this project (a stale swing threshold, an interest-rate crash mischaracterized as noise, a debt trajectory that flatly contradicted real Unity). Use it for fast iteration only. Before considering *anything* done, validate via `BatchSimulationRunner` against real Unity (`G:\UNITY\Unity Hub\6000.5.6f1\` - migrated from `6000.5.4f1` on 2026-08-01 after the older install became corrupted; see CLAUDE.md's "Real-Unity Validation is the Standard Path" for the full story) at both 100 and 500 turn horizons (or their day-equivalent, once the continuous-time migration changes the unit).
2. **Watch for the six failure patterns already seen repeatedly**: turn-1 discontinuities, oscillation, unbounded/compounding growth, bimodal attractors, and two new ones (both new as of Continuous Time + Parliament + Cabinet/Foreign-Policy coexisting, both surfaced investigating the SAME reported live-play freeze):
   - **Background/timed state mutation vs. active UI interaction** — a background system (a bill resolving, or any future timed/probabilistic mechanic) mutating live state that a GUILayout control is reading, on a day/frame the player has an active multi-frame drag in progress on that exact control. GUILayout allocates control IDs positionally, not by a stable key, so a control disappearing or a preceding control's count changing mid-drag is a documented Unity IMGUI hang/desync trigger, especially inside a ScrollView — and it's invisible to `BatchSimulationRunner`, which applies policy decisions programmatically and never drives real OnGUI/mouse-drag events, so no batch run can ever catch it. First hypothesized in the Tax Policy tab (Master Sequence step 4 pilot) when a pending TaxBill could resolve while the player was mid-drag on a rate slider; hardened there via the stable-control-layout pattern (see `GameController.DrawTaxPolicy`'s doc comment, commit `adb34ae`) regardless — every control a gated tab can ever draw renders every frame, in the same order, with "not currently applicable" expressed via `GUI.enabled = false` (composed with, not clobbering, any ambient enabled state) rather than by omitting or swapping the control. **Caveat, recorded honestly**: this fix did NOT resolve the reported freeze — Elias reproduced it again under the same conditions after commit `adb34ae`. The pattern and fix are still real and worth keeping (every one of the seven remaining tabs gains this exact same theoretical exposure once Master Sequence step 5 wires them into the draft/bill/vote model), but it was not the actual trigger of the original report. See the next pattern for what the investigation found instead.
   - **A legitimately time-blocking decision with no globally-visible indicator** — Fed Chair term appointment, a Cabinet decision, and a Foreign Policy meeting all correctly pause `GameController.Update`'s day-loop (every gate is checked correctly - this is NOT a simulation bug), but each one's actual resolution UI (the Fed Chair candidate picker, `DrawCabinetDecisionModal`, `DrawForeignPolicyMeetingModal`) renders ONLY inside its own specific tab's draw call - never globally. A player on any other tab (e.g. Tax Policy) when one of these fires sees simulated days silently stop advancing with no visible cause - indistinguishable from a hang. Before the fix, `DrawCalendarAndSpeedControls`'s always-visible status line (the one piece of UI pinned outside the scroll view on every tab) named the reason for Fed Chair and Cabinet only, in a modest, easy-to-miss label style, and said NOTHING for a pending Foreign Policy meeting - the one of the three statistically most likely to fire early in a fresh session, since it rolls per DAY (~1% chance) rather than per 121-day TURN like the other two. Fixed by escalating that line to the same bold/orange `_eventBannerStyle` used for the dashboard's own BREAKING banner whenever ANY of the three is pending, always naming which one and which tab resolves it - still exactly one Label control either way, per the stable-control-layout pattern above. This is a genuine UX gap, not a code crash: every future interrupt/decision system (gated legislation on the remaining seven tabs very much included) needs its "something needs your attention" state represented somewhere visible from every tab, not only on the tab where it originated.

   Assume a new mechanic is guilty of all six until the full-horizon batch run (for the first four) and direct live-Editor confirmation (for the last two, which batch runs cannot exercise) prove otherwise.
3. **Commit per unit of work.** One feature, one commit, descriptive message. Confirm staged contents match the message before committing.
4. **Escalate, don't guess, on genuine design decisions.** Add to Open Questions with a recommendation and reasoning; move to the next item rather than blocking.
5. **Ground new mechanics in real data.** Label anything stylized honestly — never let a placeholder look like real data.
6. **Scope every new system small on the first pass.** Plumbing plus a few clearly-justified effects, not full theoretical richness.
7. **Update CLAUDE.md after every item**, including validation results, so history stays traceable.
8. **Verify Unity processes actually exited** (`Get-Process Unity*,UnityPackageManager`) before trusting that a closed window means it's safe to run a batch validation — confirmed to cause false failures more than once.
9. **All new named entities (cabinet ministers, party names, legislators) are original and fictional** — never real people or real political parties. Same rule the Fed Chair mechanic already established, extended to every new character/entity going forward.
10. **REVERSED (2026-07-31), was a hard rule through Master Sequence step 5d**: visuals are now a MIXED procedural/sprite model, not "all procedural." Elias has explicitly approved imported sprite art for **icons, portraits, and background/menu textures specifically** — see `CLAUDE_DESIGN_ASSET_REQUEST_5E.md` for the concrete Master Sequence step 5e asset request this decision unblocks. **Stays procedural, unchanged, no exception**: all UI chrome/layout (`PoliSimTheme.cs`'s `RoundedBox`/`RoundedCard`/`Pill`/`Rule`/`TopAccent`/`LeftSpine` — pure `GUI.DrawTexture` rounded-rect/line geometry, no art asset, no reason to change) and every existing DATA visualization (`GraphRenderer`, `MapRenderer`, `PolicyWebRenderer`, `PoliticalCompassRenderer`, `HemicycleRenderer`) — none of these draw a "picture," they render real tracked simulation data, which is exactly what rule 5 ("ground new mechanics in real data") already protects; nothing about the icon/portrait decision touches that. **Becomes sprite-based**: one icon per `UiPalette.SystemArea` (policy area), one portrait per Cabinet minister candidate, one emblem per `PartyArchetype`, and background/menu textures — all sourced from Claude Design with the same origin-verification and security-review discipline already established for the first pack (Zone.Identifier mark-of-the-web check, full code/asset read-through before treating anything as trusted). This is a real, deliberate policy reversal, documented as such per this same working-discipline section's own precedent for recording a caveat/correction honestly rather than letting it look like silent drift - any FUTURE reversal of a standing rule must be recorded the same explicit way.
11. **Any new mechanic that nudges an existing tracked variable must fold into that variable's existing combined ceiling**, not add an uncounted new source — audit the actual ceiling code before adding a contributor, don't assume there's room.

---

## Where things stand right now

**Completed work has moved to `COMPLETED.md`.** This document holds only live work. Finished items move
there; this file should shrink over time, not grow. `CLAUDE.md` remains the detailed technical record for
both and is never superseded.

- **DONE** — Master Sequence steps 1, 2, 3, 4, and 5a–5d; Roadmap Rounds 1–3 (15 items); macro overhaul
  Steps A1–A3, B1 and D. See `COMPLETED.md`.
- **AWAITING ELIAS'S VISUAL REVIEW** — 10 built-but-unconfirmed items, including 5e Phase C batches 4–6
  and (new 2026-08-02) B2's contextual stat row. See `VISUAL_REVIEW_BACKLOG.md`. Step 5 cannot close
  until items 1–9 are confirmed; item 10 belongs to step 9, not 5, so it does not gate that closure.
- **BLOCKED ON ELIAS** — 3 live Open Questions (bottom of this document), plus Step C1's three
  OECD-basis housing figures. *The five macro-overhaul questions A1–A5 are all resolved (`8291662`).*
- **BLOCKED ON EXTERNAL** — macro Steps C2 and C3 need `[GAP]` figures requiring database access;
  `icon_stat_interestrate` needs Claude Design.
- **NOT STARTED** — Master Sequence items 6 (Round 4) and 7 (Continuous Time Phases 1–5), both **weeks**
  of work; item 8 (save/load, scoped only); macro Step C1–C3 and C5.

**Built 2026-08-01/02, all four now reachable from the UI**: macro Step A4 (`70798e9`, Tier 0 derived
stats — *still not trajectory-validated*), Step C4 (`76a8f35`, sovereign credit rating), and B2 rendering
(`5701a04`) wired at sub-screen granularity (`4869476`).

⚠ **`CreditRatingSystem` has no caller.** C4 computes correctly and is unit-anchored against 5 of 5
verifiable real-world ratings, but nothing displays it — it is built, not surfaced. Placing it is a real
design question (Statistics tab as a published series? the Budget screen, next to debt?) and is
deliberately left open rather than guessed at, per working-discipline item 4.

---

## THE MASTER SEQUENCE — work this list top to bottom, do not skip ahead

This is the one authoritative order, replacing whatever each original document separately suggested. It exists because Political Systems Overhaul Part B depends on Continuous Time Phase 0, and because building new Roadmap features or converting existing systems to daily granularity while Parliament's gating is mid-rollout would mean touching the same code for two different reasons at once — exactly the kind of overlap this project's discipline exists to avoid.

1. **Part A (Cabinet). DONE** — see `COMPLETED.md`. *Limitation: 3 of 6 portfolios implemented.*
2. **Part C (UI/graph restyling). DONE** — see `COMPLETED.md`.
3. **Continuous Time Phase 0. DONE** — see `COMPLETED.md`. Calendar/UI only; changed no economic math.
4. **Part B, PILOT (Tax Policy tab). DONE** — see `COMPLETED.md`.
5. **Part B, full rollout (5a–5f).** 5a–5d **DONE** (`COMPLETED.md`). **5e is the only live part:** Phases
   A and B done, Phase C batches 1–3 live-confirmed, **batches 4–6 built but awaiting visual
   confirmation** — see `VISUAL_REVIEW_BACKLOG.md` items 4, 5 and 9. Scope absorbed 5f. Confirming those
   three closes step 5 entirely. Full 5e spec in Part B below.
6. **Resume Roadmap work (a new Round 4)** — only scope this once step 5 is done, so anything new is built directly against the gated-legislation model from day one.
7. **Continuous Time Migration — Phases 1 through 5** (the actual daily-granularity conversion of each system's math, safest-first, core macro engine last). This is deliberately positioned after the political-systems work — it's a separate concern (simulation granularity, not who can change policy) and touching the same files for two unrelated reasons in the same window is worth avoiding.
8. **NEW (2026-07-31) — Build a save/load system.** Not yet scoped, not yet sequenced into the numbered order above (appended here rather than renumbering 1-7, which are referenced extensively throughout this document and `CLAUDE.md`). **Recommendation, pending Elias's confirmation**: scope and build this before or alongside Round 4 (item 6) — Round 4 is already unscoped and is the natural next planning point, and building more features on top of an unpersisted game only compounds the amount of state a save system will eventually need to cover. Reasoning this is a real severity issue, not a nice-to-have: confirmed via direct investigation (zero `PlayerPrefs`/`JsonUtility`/`BinaryFormatter`/any persistence mechanism anywhere in the codebase) that every Unity Editor/Play-mode restart discards ALL game state silently, with no error or warning - and the amount of state that now matters has grown substantially since this was last a non-issue: Cabinet ministers and their competence/philosophy, Parliament seat composition, any pending TaxBill/BudgetBill and its DaysRemaining countdown, every draft dictionary across every gated tab, the calendar date itself, Fed Chair terms, SWF holdings - losing any of this on an ordinary restart is a real loss of play, not a cosmetic gap. This was the leading suspect for a live-play anomaly where an SWF draft never became standing across two observed fiscal-year cycles - **now confirmed as the actual cause**: Elias confirmed Unity was closed/reopened multiple times between setting the draft and the next fiscal date, and the underlying bill mechanism itself was independently proven correct across two full fiscal years via a targeted diagnostic (see CLAUDE.md's "Master Sequence step 5a/5b/5c" writeup). Needs its own design pass before implementation starts, not a guess: what serializes cleanly under Unity's own `JsonUtility` (which - like Unity's Inspector serialization generally - doesn't support `Dictionary<>` natively either, the same limitation already visible as `UAC1009` warnings on several existing fields, e.g. `PolicyDecision.TaxRateOverrides`/`SpendingLineChanges`/every Sector-override dictionary; `BudgetBill`'s own dictionaries would hit the same wall), whether a mid-cycle pending bill's DaysRemaining and a real save timestamp interact cleanly, and how much of `World`/`Country`'s current in-memory object graph can serialize as-is versus needs a dedicated save-data shape. Escalate format/scope decisions rather than guessing, per this document's own working discipline item 4.

   **SCOPED (2026-08-01), both escalated decisions now answered by Elias.** The design pass this item
   asked for has been done, and the answer to "what serializes cleanly under `JsonUtility`" turned out to
   be: **essentially none of it.** `JsonUtility` fails this state model on four independent counts, each
   verified against the real code rather than assumed:
   - **`Dictionary` is unsupported** — `SimulationManager` alone holds 10+, several NESTED
     (`Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>>`), on top of `Country`'s own. This is
     what the 11 standing `UAC1009` build warnings have been reporting all along.
   - **`DateTime` is unsupported** — and `SimulationManager.CurrentDate` is a `DateTime` auto-property
     with a *private setter*, so it fails twice over. The in-game calendar is not optional state.
   - **`readonly` collection fields are not serialized** — 4 of them, including `StatHistory`'s series.
   - **Nullables are unsupported** — `DateTime?` throughout `StatHistory`.

   **Decision 1 — serializer: Newtonsoft JSON** (`com.unity.nuget.newtonsoft-json`, a Unity-published
   package), chosen by Elias over hand-writing a DTO layer. It handles dictionaries, nested dictionaries,
   `DateTime`, nullables and private setters natively, so game classes can be persisted close to as-is.
   The rejected alternative meant mirroring a large share of the 33 types in `Assets/Scripts/Data` into
   save-only DTOs, flattening every dictionary into paired lists, hand-encoding `DateTime` — and
   re-mirroring every one of them on each future model change. Added to `Packages/manifest.json`.

   **Decision 2 — first-pass scope: all three layers**, per Elias:
   1. **Core sim state** — `World` (countries, economies, parliaments, cabinets) + current turn/date.
   2. **Pending bills and interrupts** — in-flight bills WITH their day counters, plus pending
      cabinet/foreign-policy/Fed-chair decisions. Omitting these would make a reload silently cancel
      anything mid-vote, which is its own version of the bug this item exists to fix.
   3. **UI draft values** — the unintroduced slider drafts. This is precisely what was lost in the
      original 5c incident (the SWF draft), so a "save system" that dropped them would not have
      prevented the very bug that motivated it.

   **Implementation note for whoever builds it**: `SimulationManager`'s pending state lives in `private
   readonly` dictionaries, so it needs an explicit `CaptureSaveState()`/`RestoreSaveState()` pair
   returning a plain state object, rather than reflection over private fields. Explicit is better here
   for a reason beyond taste: it makes the persisted surface reviewable, so a newly added pending-bill
   type that nobody wired into the save shows up as an obvious omission instead of silently
   half-persisting. Same for `GameController`'s drafts.

   **Dependency is in place and proven (commit `ebcc2d2`)**: the package resolved, and a throwaway probe
   serializing a NESTED dictionary — the precise case `JsonUtility` cannot express — compiled clean
   against it before being deleted. One local-only gotcha for whoever picks this up: Unity had not
   regenerated `Assembly-CSharp.csproj` with the Newtonsoft reference, so it was added by hand (that file
   is gitignored, so this does not travel with the repo — if a fresh clone can't see `Newtonsoft.Json`,
   let the Editor regenerate the csproj or re-add the reference the same way).

   **Independent confirmation from Unity's own analyzer (2026-08-01)**: adding `Country.Published` produced
   `warning UAC1001: Field 'Published' type 'PoliSim.Data.PublishedData' is skipped by serialization`.
   Unity's serializer silently DROPS the entire published series — the exact failure this item's
   serializer decision was made to avoid, now demonstrated by the compiler rather than argued from
   documentation. Note the word *skipped*: no error, no data, no indication at runtime that anything was
   lost. Had save/load been built on `JsonUtility`, every published figure would have vanished on reload
   while the save file looked perfectly valid.

   The warning is deliberately NOT silenced. Adding `[System.Serializable]` to `PublishedData` would
   remove it without making the type serializable — its `Dictionary` fields remain unsupported — so the
   warning would be traded for a false reassurance. It stands as an accurate signal, and it is why the
   project's warning baseline moved from 11 to 12.

   **Implementation has NOT started.** Everything above is design and dependency work. The build order
   that follows from the two decisions: (1) a `SaveGame` payload type holding format version, player
   country, turn/date, `World`, the simulation snapshot and the draft snapshot; (2) explicit
   `CaptureSaveState`/`RestoreSaveState` on `SimulationManager` and on `GameController`'s drafts;
   (3) read/write through `Application.persistentDataPath`; (4) UI to trigger it. **Include the format
   version from the very first write** — this game's data model is still changing weekly, and a save file
   with no version field is unreadable the moment a field moves, with no way to detect that it happened.

   ### Scoping extension (2026-08-01) — two save-blocking types that did not exist when the above was written

   The design above was written on 2026-07-31. Step A landed on 2026-08-01 and introduced two pieces of
   state the serializer decision does **not** already cover. Both were found by inspection, not
   speculation, and both would produce a save file that loads without error while being wrong.

   **Gap 1 — `SimulationRandom` cannot be saved as designed, and is the more serious of the two.**
   It holds `Dictionary<Stream, System.Random>` plus the master seed, and `System.Random` exposes no way
   to read or restore its internal position. Saving only the master seed and re-seeding on load therefore
   rewinds **every stream to its turn-zero position**: a game saved at turn 50 and reloaded would replay
   the same event draws, Fed-chair candidates and cabinet decisions the player already saw, in the same
   order. The save would look valid and the simulation would still look deterministic — it would just be
   running the wrong part of the sequence. This is a *replay*, not a reroll, so it is not a save-scum
   exploit; it is a correctness failure that is easy to mistake for one.

   Two viable fixes, and this is a genuine fork rather than a clear call:

   - **Wrap `System.Random` in a counting shim** that records how many draws each stream has taken, then
     on load re-seed and fast-forward each stream by its recorded count. Minimal change to the existing
     seeding contract, and the `Stream` enum's append-only rule keeps working untouched. Cost: the
     fast-forward is O(draws), a real if brief load-time loop after a long game.
   - **Replace `System.Random` with a small explicit PRNG** (e.g. 64-bit xorshift) whose entire state is
     two integers that serialize directly. Constant-time load, and the state becomes inspectable in a
     diff. Cost: it changes every existing stream's number sequence — a deliberate, one-time break of
     every recorded baseline in `CLAUDE.md`, and this project has already accumulated several such
     discontinuities in a single day.

   *Recommendation: the counting shim.* It is reversible, preserves every existing baseline, and its cost
   is bounded by draws-per-game rather than anything unbounded. **Escalated to Open Questions** rather
   than settled here, because it trades a permanent baseline break against a permanent load-time loop,
   and that is Elias's call.

   **Gap 2 — `PublishedData.PeriodClosingValues` is keyed by a `ValueTuple`.** Its declared type is
   `Dictionary<(PublishedStat Stat, DateTime PeriodStart), float>`. Newtonsoft serializes dictionaries as
   JSON objects with string keys and needs a `TypeConverter` to render a key as a string; `ValueTuple`
   has none, so this will fail or emit unusable keys. Same class of problem as the `UAC1001` finding
   above, caught before a silent data loss rather than after.

   It cannot simply be dropped from the save. `PeriodClosingValues` is what a later revision converges
   toward, and publishing has already had exactly one bug from resolving that value wrongly — revisions
   converging on the publication date's live figure instead of the reference period's closing figure. A
   save that omitted it would reintroduce that bug on every load. It also grows without bound, one entry
   per stat per period forever, so it wants a retention rule at the same time.

   Straightforward fix, no fork: flatten it into a list of `{stat, periodStart, value}` records on
   capture and rebuild the dictionary on restore — the same flattening the design above already commits
   to for every other dictionary. The retention rule (how many closed periods stay worth keeping once
   every publication referencing them is `Final`) is a separate question worth answering deliberately
   rather than defaulting to "keep everything".

   **Still not started, and correctly so.** This extension adds scope; it does not begin implementation.

9. **NEW (2026-08-01) — Macro Data & Release Calendar Overhaul (Steps A–D).** Full spec in
   `POLISIM_MACRO_OVERHAUL_DIRECTIVE.md`; every real-world figure it depends on is in
   `POLISIM_SEED_DATA_MACRO_OVERHAUL.md`. Appended rather than renumbering 1-8, which are referenced
   throughout this document and `CLAUDE.md`.

   **Why it is four steps and not one** — the directive's own reasoning, recorded here because it governs
   how the work may be sequenced: seven new tracked stats + a revision mechanic + per-period lag tracking
   + six countries publishing independently is the largest single change attempted on this project,
   larger than Demographics (5 fields, which still needed two structural bug fixes and three correction
   rounds). The split exists to make failure ATTRIBUTABLE. Step A introduces the publication machinery
   with **zero new tracked variables**, so any trajectory drift can only be the machinery; Step C then
   adds stats onto a foundation already proven inert. Landing both together would make a drift
   impossible to attribute — and per the directive, such a drift "may not surface for hundreds of turns".

   - **Step A — release calendar, published series, revisions, Tier 0 derived stats. DONE (2026-08-01), commit `e3a0feb`.** The risky
     foundation. Rule-based per-country schedules (not hardcoded dates), a published series per stat
     carrying reference period / publication date / value / revision status, preliminary→revised figures
     derived from the true underlying value, and display-only derived stats (GDP per capita, tax burden
     % GDP, spending % GDP, deficit % GDP, real GDP growth, sector shares).
   - **Step B — graph overhaul + contextual policy-screen stats.** Display only, depends on A. EXTEND
     `GraphRenderer`, do not build a parallel system. Calendar date axis, release-point markers,
     preliminary-vs-revised treatment, selectable ranges, existing threshold lines retained.
   - **Step C — the seven new tracked stats, in four batches** (C1 housing, C2 inequality + real wages,
     C3 youth unemployment + life expectancy, C4 credit rating as a DERIVED value). Never all at once.
     Rule 11 applies to every batch: any effect on an existing tracked variable folds into that
     variable's existing combined ceiling, audited first — `PotentialGrowthRate` and
     `LaborForceParticipationRate` are already heavily stacked.
   - **Step D — sprite asset request.** A document, not code.

   **SEQUENCING CHANGED BY ELIAS (2026-08-01), superseding the directive's own "Sequencing summary"**:
   start with **STEP D**, not Step A. Sprite work has a long external turnaround (Claude Design round
   trip, security review, import), while A–C are pure code that does not depend on it. D goes out first
   and runs in parallel; **A starts immediately after D's request document is written**, not after the
   assets arrive. The directive's ordering is otherwise unchanged.

   **Consequential adjustment this forces**: Step D's spec says to derive the stat-icon list "from the
   real stat enum once Step C's stats exist" — circular if D runs first. Resolved by compiling the list
   from (a) the stats NAMED IN THE DIRECTIVE and (b) the 29 fields already on `EconomyState`, which are
   readable today. Any icon whose exact name depends on a not-yet-written enum ships with a provisional
   filename to be reconciled at implementation time, rather than blocking the request.

   **THE CRITICAL CORRECTNESS RISK, and Step A's actual bar**: the player-facing UI reads the PUBLISHED
   (lagged, possibly-revised) series; every internal system — Okun's Law, the Phillips Curve, the Fiscal
   Reaction Function, sector integration — must keep reading LIVE values. A leak makes the model consume
   its own stale output, and the effect may not appear for hundreds of turns. **Step A must change ZERO
   simulation numbers, proven by identical trajectories before and after** — not by inspection. That
   requires capturing a baseline run on the pre-change HEAD BEFORE any Step A code is written, since an
   untainted reference cannot be reconstructed afterwards.

   **`[GAP]` figures are Elias's to source, never to invent.** This session has no web search. Each Step
   C batch must report its needed gaps before starting. Three further traps the seed file flags
   explicitly and that must be carried forward: `[PARTIAL]`/source-conflicted figures must not be
   silently merged (Sweden ~70 and Poland ~24.5 productivity are Statista, not OECD PPP — Poland is
   implausible against an OECD PPP average of $67.5); Gini needs one normalized scale (Eurostat 0–100 vs
   US 0–1, different methodology); and youth unemployment must use *rate* consistently, never *ratio*
   (the Germany 3.6 / Poland 3.5 figures are ratios and would badly distort the model).

If a step's own validation fails, fix it before moving to the next — never proceed past a failing step to "make progress" on the next one.

---

# PART ONE: Continuous Time Migration

*(Full original plan preserved — becomes step 3 and step 7 of the master sequence above.)*

## Why this exists

Migrating from turn-based (1 turn ≈ 121 days / 4 months) to true daily-granularity continuous time with Pause/1x/2x/3x speed controls. Nearly every tuned constant in the game is implicitly calibrated against a ~121-day step. This is the largest single risk in the project's history — do not attempt it as one pass.

## The translation methodology — do not guess new constants

Identify which mathematical shape a constant is before touching it:

1. **Linear/additive rates**: `rate_per_day ≈ rate_per_turn / 121`
2. **Multiplicative/compounding rates**: `rate_per_day = (1 + rate_per_turn)^(1/121) − 1`
3. **Probabilities**: `p_per_day = 1 − (1 − p_per_turn)^(1/121)`
4. **Hard clamps/ceilings do NOT shrink by 121x** — a ceiling bounds the state itself, not a per-step increment. Only the *speed of approach* changes (via #1 or #2 above). Treating a ceiling as something to also divide by 121 is a likely first-attempt bug.

## The validation bar: aggregation-equivalence

Before any system's daily version is trusted: simulate 121 consecutive days and confirm the result is within ±3-5% of what the existing, already-validated single turn-level step produces for the same inputs. This is the ground truth every phase below must pass before moving to the next.

## Real-world data release cadence (cross-cutting, applies to every phase)

The internal simulation can evolve daily; the player-facing display should only update stats on the schedule real institutions actually publish them, for realism and pacing:
- **Continuous/real-time**: Currency Strength.
- **Monthly**: Unemployment, Inflation, Trade Balance, sector output figures.
- **Quarterly**: GDP and GDP growth %, DebtToGdpRatio.
- **Annual**: Population, demographic rates, PovertyRate, CrimeIndex/PrisonPopulationRate, Infrastructure ConditionIndex, annual budget figures.
- **Central-bank-meeting-based** (not calendar-periodic, ~8/year like the real Fed/ECB): interest rate decisions.
- **Election-cycle-based**: elections, Fed Chair appointment.
Optional refinement (Open Question, not required for a first pass): real reporting lag between a period ending and its data being published.

## Phase 0 — DONE (2026-07-30). See `COMPLETED.md`.

## Phases 1-5 — daily-granularity conversion (MASTER SEQUENCE STEP 7, safest-first)

- **Phase 1**: Sectors and Infrastructure (smallest blast radius, proves the methodology).
- **Phase 2**: Labor Market and Crime & Justice (moderate risk).
- **Phase 3**: Tax portfolio, Welfare, Spending categories, SWF (revenue/spending-critical, same seriousness as the original debt work).
- **Phase 4**: Demographics (its YearsPerTurn scaling is a direct dependency on turn-length — cannot start until the new day-length constant is threaded through correctly; use the same throwaway-diagnostic-before-full-matrix discipline that caught its two prior structural bugs).
- **Phase 5**: The core macro engine — GDP identity, Okun's Law, Phillips Curve, interest rate transmission, Fiscal Reaction Function, debt dynamics. Highest risk, last on purpose — this system has the worst track record for hidden instability in the project. Do not start until every other phase has proven the methodology reliable.

Each phase: apply the correct transform per constant, aggregation-equivalence check FIRST, full scenario matrix SECOND, one commit per phase, escalate ambiguous constant shapes to Open Questions rather than guessing.

---

# PART TWO: Political Systems Overhaul

*(Full original plan preserved — becomes steps 1, 2, 4, and 5 of the master sequence above.)*

## Confirmed scope (both at maximum ambition — treat accordingly)

- **Cabinet is INTERACTIVE** — ministers periodically bring decisions/events, not a passive stat bonus.
- **Parliament gates ALL existing policy changes across every tab** — Tax, Spending, Welfare, Labor, Crime & Justice, Sectors, Infrastructure, SWF. This is the largest single architectural consequence in the project's history, larger in code surface than the time migration itself.

## Part A — Cabinet (MASTER SEQUENCE STEP 1) — DONE. See `COMPLETED.md`.

---

## Part B — Parliament (MASTER SEQUENCE STEPS 4 and 5). Step 4 and 5a-5d DONE, see `COMPLETED.md`. **Only 5e is live**, spec below.

### 5e implementation plan — three phases (confirmed by Elias, 2026-07-31)

Structure settles first using the EXISTING procedural rendering (no visual style changes) - THEN the
sprite reskin gets piloted on Statistics/Dashboard specifically - THEN rolled out to the rest once
proven. Written here before any code changes so it survives a session restart. Standing rules apply
throughout: one commit per phase (or per batch within Phase C), escalate genuine design forks rather
than guessing, never mark a phase done without a live screenshot.

#### Phase A — Tab/IA restructuring (no visual style changes yet). DONE (2026-07-31).

Reuses the existing tab-bar UI mechanics (color-coded per area, same interaction pattern, same
`DrawRightColumnTabButton` mechanics, now `DrawConsolidatedTabButton`) - only the grouping/navigation
changed in this phase, not the visual style (icons/sprites are Phase B/C's job, no `icon_*` texture is
drawn anywhere yet).

**Real tab audit (confirmed against `GameController.cs`'s actual `RightPanelTab` enum, not assumed -
18 tabs, matching Elias's own list exactly, none missing).**

**Old → new mapping - CONFIRMED (directly stated in this document's own original 5e scope text, or
require no judgment call):**

> **AMENDED 2026-08-01 — the structure is now 6 tabs, not 7** (commit `865fcf4`). Tax and Spending were
> merged into a single **Budget** tab at Elias's direction, so every row below that previously read "Tax",
> "Spending" or "Tax or Spending (same screen)" now reads **Budget**. The original split gave the same
> screen two top-level entry points that differed only in which of its OWN five sub-categories
> (Tax/Spending/Welfare/Infrastructure/SWF) was pre-selected — which also misrepresented Tax and Spending
> as peers of Statistics and Politics when they are really peers of Welfare and Infrastructure, already
> sub-categories of that very screen. The rows are amended in place rather than duplicated, since the
> destination changed but every mapping DECISION below still stands exactly as reasoned.

| Old tab (`RightPanelTab`) | New tab | Basis |
|---|---|---|
| `RecentTurns` | Statistics → **Domestic** | Original 5e scope named it explicitly. **RESTRUCTURED 2026-08-01**: "Recent Turns" was a turn-based-era name that no longer describes anything under continuous time; its content is now the Domestic sub-tab, and the turn log itself moved to International (it reports world-wide activity, not domestic). 
| `WorldMap` | Statistics → **International** | Original 5e scope named it explicitly. **RESTRUCTURED 2026-08-01**: renamed International, and it ABSORBED Trade - trade is international relations, and was only ever a peer sub-tab for historical rather than conceptual reasons. 
| `TaxPolicy` | Budget | Retired as standalone - becomes an ENTRY POINT into the existing Budget Process screen (`DrawBudgetProcessTab`), opened at its Tax category. Confirmed via `DrawRightColumnTabs`'s own code comment: *"Budget Process consolidates Tax/Spending/Welfare/Infrastructure/SWF's existing content... those five tabs stay as independent entry points for now per the Master Sequence step 5 design, not removed until step 5e's own tab consolidation."* This IS that consolidation. |
| `SpendingPolicy` | Budget | Same as Tax above, opened at its Spending category. |
| `WelfarePolicy` | Budget | Same mechanism - Welfare is one of `BudgetProcessCategory`'s 5 existing values, reachable from EITHER new entry point once inside the consolidated screen, not a separate top-level tab. |
| `SwfPolicy` | Budget | Same mechanism - `BudgetProcessCategory.Swf`. |
| `LaborMarket` | Policy/Laws | Original 5e scope text: "Policy/Laws (standalone bills from 5d)" - `LaborPolicyBill` is exactly this. |
| `CrimeJustice` | Policy/Laws | Same - `CrimeJusticePolicyBill`. |
| `SectorPolicy` | Policy/Laws | Same - `SectorPolicyBill`. |
| `Cabinet` | SPLIT: Decisions + Politics | Original 5e scope text names Cabinet under BOTH "Decisions (pending... Cabinet... interrupts)" AND "Politics (Parliament/Compass/Cabinet)" - confirmed via code that `DrawCabinetTab` genuinely has two distinct pieces: the pending-decision modal loop (`GetPendingCabinetDecisions`/`DrawCabinetDecisionModal`) moves to Decisions, the portfolio/candidate-picker UI stays under Politics. |
| `CompassAndDemographics` | SPLIT: Politics + Demographics | Original 5e scope text separates "Compass" (under Politics) from "Demographics (population/pie charts)" as its own tab - this single existing tab's two halves (the Political Compass chart vs. the demographic pie charts) split accordingly. |
| `ForeignPolicy` | Decisions | Original 5e scope text names it explicitly, AND confirmed via code that the ENTIRE standalone tab's content is the pending-meeting interrupt (explanatory text + either the modal or "No meeting currently pending") - nothing left behind to place anywhere else. |
| `Parliament` | Politics | Original 5e scope text names it explicitly. Deliberately NOT also split into Decisions - unlike Foreign Policy/Cabinet, a pending bill never pauses time (only the ANNUAL budget process's Phase 1 does, and that's covered by the global banner already, not a tab), so there's no true "interrupt" here needing Decisions' attention pattern - Parliament's own "Pending Legislation" list (all 7 bill types) is informational, not blocking. |
| `BudgetProcess` | Budget | This tab doesn't move anywhere new - it effectively becomes what "Tax" and "Spending" both open into (see `TaxPolicy` row above). Not retired so much as promoted to the thing the other five folded into. |
| `Trade` | SPLIT: Statistics + Policy/Laws | **RESOLVED by Elias (2026-07-31), confirmed as originally proposed.** The Trade Balance graph and per-partner import/export volume bars are informational (Statistics-flavored); the tariff bill status/live-estimate/Introduce action (`TradePolicyBill`, a genuine standalone bill from 5d) is exactly what "Policy/Laws (standalone bills from 5d)" describes. Implementation refinement (not a design fork): the per-partner override CONTROLS stay bundled with their own bars in one row rather than splitting a single row's rendering across two different tabs (a real UX regression - a player adjusting an override wants the volume bars right next to it) - so Statistics gets only the aggregate Trade Balance graph, and Policy/Laws gets the full per-partner section (bars AND override controls together). | **RESTRUCTURED 2026-08-01**: the Statistics half is no longer a `Trade` sub-tab of its own - it folded into **International**, since trade IS international relations and was only a peer for historical reasons. The Policy/Laws half is unchanged. 
| `FederalReserve` | Politics | **RESOLVED by Elias (2026-07-31), confirmed as originally proposed.** A real political institution with its own active lever (interest rate, Fed Chair selection) that groups naturally with Parliament/Cabinet, even though the Fed/Eurozone exemption means it's never Parliament-gated. |
| `PolicyWeb` | Policy/Laws | **RESOLVED by Elias (2026-07-31), OVERRIDING the original recommendation (Statistics).** Elias's own reasoning: "it's a relationship/reference tool consulted while deciding what to change, closer to where bills get drafted than to a pure stats readout." |
| `Infrastructure` | Budget | **RESOLVED by Elias (2026-07-31), confirmed as originally proposed.** Consistent with Welfare/SWF above - `DrawInfrastructureContent` is ALREADY reused verbatim inside Budget Process's own Infrastructure category, and the standalone tab has no independent lever of its own. |
| Does "Decisions" need the Budget Process mandatory-pause interrupt too? | Yes | **RESOLVED by Elias (2026-07-31), confirmed as originally proposed.** Elias's own reasoning: "any 'time is blocked until you respond' state belongs in the same place, not treated as an exception." Reuses the same `DrawBudgetBillStatusAndIntroduce` status+Introduce UI already built for Tax/Spending, shown in Decisions ONLY while `GetPendingBudgetProcess` is true (mirroring Foreign Policy/Cabinet's own "only appears while actually pending" pattern) - the ongoing per-bill countdown status stays under Tax/Spending, this is specifically the blocking Phase-1 moment. |

All five previously-escalated placement questions are now resolved - see Open Questions below, marked
closed. Phase A implementation proceeded using the mapping above in full, exactly as confirmed - the
`ConsolidatedTab`/`StatisticsCategory`/`PolicyLawsCategory`/`PoliticsCategory` enums and their dispatch
in `GameController.cs` match this table row for row. One extension beyond the five resolved items,
applying Elias's own stated general principle (see the Budget Process row above - "any 'time is blocked
until you respond' state belongs in the same place, not treated as an exception") rather than guessing
at something new: Fed Chair selection is ALSO a blocking interrupt (see `UpdateFedChairSelectionState`)
that was never one of the five items Elias was asked about - added to Decisions too
(`DrawFedChairSelectionModal`, extracted from Federal Reserve's own tab so both places render the exact
same UI), flagged clearly rather than silently added.

**Validation for Phase A: DONE.** `dotnet build` clean (0 errors, full output read - not just grepped
for "error"). The single-scenario automated smoke check (100-turn baseline via
`BatchSimulationRunner`) was attempted twice and abandoned - both attempts stalled inside Unity's own
cold-start asset-reimport/indexing phase (confirmed via process CPU/responsiveness checks: the process
was genuinely busy, not deadlocked, but never got past Editor startup to actually run the scenario),
the same category of infrastructure issue already documented earlier in this project's history as
unrelated to the code under test. Not retried a third time - Phase A's code changes are 100% UI-layer
(`GameController.cs` only; `ParliamentSystem.cs`/`SimulationManager.cs`/`MacroSystem.cs`/every
simulation file untouched), so the real risk here was always navigation/UI breakage, which only a live
click-through can verify anyway (a headless batch run never drives real `OnGUI`). **Confirmed via
Elias's own live-Editor click-through (2026-07-31)**: all 7 new tabs render correctly, every
sub-category selector (Statistics' 3, Policy/Laws' 5, Politics' 4) switches correctly, and the mapping
matches this section's own table exactly.

#### Phase B — Sprite reskin pilot: Statistics/Dashboard only — **DONE (2026-08-01)**

**Prerequisite check: DONE (2026-07-31), confirmed BEFORE any real Statistics/Dashboard rendering work
started, per Elias's own explicit instruction not to build on an unverified assumption.** The
icon-tinting helper (`UiPalette.DrawTintedIcon(Rect, Texture2D, Color)` - `GUI.color`-multiply tinting,
the exact mechanism the Claude Design asset pack's own README specifies, mirroring `HemicycleRenderer`'s
own existing per-seat dot-tinting idiom) is now real, permanent code in `UiPalette.cs`, not just planned.
Verified via a throwaway, fully isolated Editor-only test window (`Assets/Editor/IconTintingTest.cs`,
zero production-code changes, deleted after use per this project's own established convention) showing
3 real imported icons (`icon_area_fiscal`, `icon_area_infrastructure`, `icon_nav_statistics`) each tinted
3 ways (white/its own area color/dimmed grey). **Confirmed by Elias directly**: icon shapes clearly
visible in every cell, background stays genuinely transparent, and the three tint colors are visibly
different from each other - the core visual claim actually holds, not just assumed from the asset
pack's own README text. Real production usage (which texture reference mechanism Statistics' actual
tab-bar button and any other real call site uses - serialized Inspector fields vs. `Resources.Load` vs.
something else - `AssetDatabase.LoadAssetAtPath` only works in-Editor, so the throwaway test's own
loading method is NOT what production code will use) is still Phase B's own work below, not resolved by
this prerequisite check alone.

Apply the now-confirmed icon-tinting helper and `PoliSimTheme`/`PoliSimWidgets`' card/stat-tile/
threshold-bar primitives to the Statistics tab specifically - its own nav icon (`icon_nav_statistics.png`,
already imported) tinted appropriately in the tab bar, and its headline stats/graphs restyled using the
new widget patterns instead of raw `OnGUI.Label` layout. Highest-visibility screen, validates both
pieces of infrastructure (icon tinting + card widgets) together, in production code, before trusting
them anywhere else. Do NOT touch any other tab's rendering in this phase.

**Validation**: live-Editor screenshot confirming the new look renders correctly AND that all the same
data is still accurate - a visual change must never be able to silently change a number. Hold here for
Elias's confirmation before Phase C.

**Validation: DONE (2026-08-01)**, confirmed by Elias in the live Editor across two rounds. Built: the
production texture-loading mechanism Phase B's prerequisite check explicitly left open is now
`IconLibrary` (a cached name -> `Texture2D` lookup over `Resources.Load`, which is why the Icons folder
moved to `Assets/Resources/Art/UI/Icons` - `AssetDatabase` is Editor-only and would have silently broken
in a real player build), the Statistics tab's own nav icon drawn through it, and `DrawDashboard`'s nine
headline stats restyled from a raw two-column `GUILayout.Label` list onto a three-column
`PoliSimWidgets.StatTile` grid (`DrawHeadlineStatTiles`). `DrawHeadlineGraphs` deliberately untouched,
per rule 10's own carve-out keeping every data visualization procedural. Only GDP carries a delta pill,
since `_lastGrowthPercent`/`_prevGdp` is the only genuine turn-over-turn value tracked - the other eight
show no delta rather than a fabricated one. **Round 1** confirmed the grid ("restyled grid is looking
good") but rejected the icon as overlapping its label; that is fixed in Phase C batch 1 below, where the
cause and the arithmetic are recorded. Commits `b6da098` (code), `967fb46` (the tinting helper).

#### Phase C — Rollout to remaining 6 tabs

Only after Phase B is confirmed. Apply the same now-proven pattern to Decisions, Demographics,
Tax/Spending (Budget Process), Policy/Laws, and Politics - NOT all 6 simultaneously. Split into 2-3 at a
time, the same discipline the original Parliament gating rollout used (step 5's own revised design
explicitly avoided touching all seven remaining tabs in one pass, for exactly this reason). Screenshot
and confirm after each batch before continuing to the next.

**Batch 1 — the tab bar itself: DONE (2026-08-01)**, commit `a8decf9`, confirmed by Elias in the live
Editor ("all the tabs are looking great"). Taken first, and as a single batch rather than split, because
the tab bar is shared chrome rather than any one tab's content - splitting it would have left the bar
visibly half-iconned between batches. All tabs now carry their icon (7 at the time; 6 since the Tax/Spending merge); the four `icon_nav_*` exist
precisely because Statistics/Decisions/Demographics/Policy-Laws map to no single `SystemArea`, while
Tax/Spending/Politics reuse `icon_area_fiscal`/`icon_area_political` directly per
`CLAUDE_DESIGN_ASSET_REQUEST_5E.md`'s own manifest. **Tax and Spending share one icon** - both are
`GetConsolidatedTabArea` -> `Fiscal` and both are differently-labelled entry points into the same Budget
Process screen; flagged to Elias explicitly rather than silently substituted, and left as documented.

Two things worth carrying into later batches:
- **The icon is stacked ABOVE its label, not beside it, and this was forced by real arithmetic, not
  taste**: the right column is ~55% of the window, so each of 7 tabs got ~143px at 1080p (6 tabs now, slightly wider, but the conclusion holds), while
  "Demographics" alone is ~175px of text at the 26px tab font. A left gutter pushed labels to three
  lines and clipped them; shrinking the icon to compensate returned it to the unreadable speck the work
  set out to fix. Elias picked stacking from three costed options.
- **Reserve space, never overlay.** Phase B drew the icon on top of the already-finished button, so
  GUILayout centred the label with no knowledge an icon was there - that WAS the overlap bug. Batch 1
  reserves the space via `style.padding.top` before the button draws, so the label's own layout accounts
  for the icon and they cannot collide at any window size or label length. Any future "draw art into an
  existing control" work in this codebase should assume the same failure mode.

**Batch 2 — card chrome + the Decisions tab: DONE (2026-08-01)**, commit `5df7811`, confirmed by Elias
in the live Editor. Added the shared infrastructure the remaining batches all depend on, then used it on
one tab: `UiPalette.BuildCardStyle`/`DrawCardSpine`/`GetRoundedTexture` (a 9-sliced rounded texture
behind a GUIStyle background) plus `UiPalette.GetPortfolioArea`, and `GameController`'s
`BeginDecisionCard`/`EndDecisionCard`. The Decisions tab now renders each pending interrupt as its own
card with a caps kind caption and an area-colored left spine; Cabinet decisions tint by PORTFOLIO
(Finance→Fiscal, Interior→CrimeJustice, Health→Welfare) rather than the flat `Political` every cabinet
surface used before, which reads as one undifferentiated block once several stack up.

Three things later batches should reuse rather than rediscover:
- **`PoliSimTheme.RoundedBox` is not sufficient on its own.** It only draws into a Rect the caller has
  already measured - fine for fixed-layout widgets like `StatTile`, useless for the large majority of
  this UI, which is GUILayout flow whose height isn't known until its content has been laid out. The
  9-sliced STYLE background is what makes existing GUILayout content cardable without rewriting it into
  manual Rect math - and that rewrite is exactly what produced two real layout bugs in 5b. **Prefer
  `BuildCardStyle` over converting a screen to Rect math.**
- **Check every call site of a shared renderer before restyling it.**
  `DrawForeignPolicyMeetingModal`/`DrawCabinetDecisionModal` already opened their own `_boxStyle` frame,
  so wrapping them nested a flat grey box inside the dark card. Both took a `drawOwnFrame` parameter
  **defaulting to true**, so the Politics tab's own Cabinet screen (a later batch) stayed byte-for-byte
  unchanged. Several of these renderers are shared across tabs that belong to DIFFERENT batches - that
  is the main way a batch can silently leak into a tab it wasn't supposed to touch.
- **Cache anything built per frame.** Card styles and their textures are rebuilt by callers every frame;
  allocating per call is a per-frame leak, not just a cost.

**Batch 3 — Politics (+ Demographics assessed): DONE (2026-08-01)**, commit `6922f9f`, confirmed by
Elias in the live Editor. The 16 imported portraits moved into `Resources/` and are now actually drawn
for Fed chair candidates, cabinet candidates and appointed ministers, via `IconLibrary`'s
`GetCabinetPortrait`/`GetFedChairPortrait` (filename derived from the character's generated name, not a
hand-maintained table that could drift from the pools in `CabinetSystem`/`FederalReserveSystem`;
unmatched names fall back to the procedural silhouette rather than showing someone else's face).
Portfolio panels head in their own area color. Pending bills render as cards with a lean bar.

**Demographics needs no restyle, and this is a finding rather than an omission**: its content is
entirely pie charts, which working discipline item 10 explicitly keeps procedural. Wrapping them in
decorative cards would add clutter without meaning. Treat it as complete.

**The roadmap's own recommendation to use `PoliSimWidgets.SupportBar` here was WRONG, and the reason
generalizes.** That widget renders "N of 200 seats, majority 101". This simulation has no seats-based
majority at all: `ParliamentSystem` decides a vote by summing `seatShare * fiscalStance * billSign` and
testing it against ZERO, so a bill can pass with fewer aligned seats than opposed ones (if its
supporters hold stronger stances) and fail with more. Using it would have drawn a rule the model does
not implement. **The design pack's widgets were authored against an assumed generic political sim, not
against this codebase's actual mechanics — check each one against the real model before reaching for
it, rather than trusting that a plausible-sounding fit is a real one.** `UiPalette.DrawDivergingBar`
(centre-threshold, no number printed) is the honest substitute, fed by a new
`ParliamentSystem.GetSeatWeightedAlignment` that `WouldBillPass` also calls so the two cannot disagree.

Also worth carrying forward: **`Mathf.Sign(0f)` returns 1 in Unity, not 0.** A zero-direction bill
(drafts introduced unchanged) passes unconditionally via a short-circuit in `WouldBillPass`, but scoring
it anyway yields parliament's raw net stance — negative in the documented tied-parties case — which
would have painted a red bar beside the words "leans PASS". Any derived display must short-circuit on
the same condition its verdict does. This was caught while writing repro steps, not during review.

**Batch 4 — Policy/Laws: BUILT (2026-08-01)**, commit `a1bec98`. **Not yet live-confirmed.** All four
standalone-bill screens (Labor, Crime & Justice, Sectors, Trade) had a byte-for-byte identical
live-estimate renderer differing only in which draft they built and which direction getter they called;
they now delegate to one `DrawBillLiveEstimate(float direction)`, which also gained the diverging lean
bar. Each screen's bill block is wrapped in an area card. `BeginDecisionCard`/`EndDecisionCard` were
renamed `BeginAreaCard`/`EndAreaCard` (caption now optional) since they serve three tabs, not just
Decisions. **The collapse mattered for correctness, not just tidiness**: the `Mathf.Sign(0f)`
zero-direction trap has to be handled identically in every copy, and four copies is four chances to get
it wrong — that exact bug had already shipped once in `DrawPendingBillCard`.

**Batch 5 — Budget Process: PARTIALLY BUILT (2026-08-01)**, commit `cdd5a1c`. **Not yet live-confirmed.**
`DrawLegislativeSupportEstimate` turned out to be a FIFTH copy of the same renderer, on the most
important screen in the game; it now shares `DrawBillLiveEstimate` too, so the annual budget bill gets
the same lean bar and zero-direction handling. Equivalent by construction, not just by eye:
`WouldBillPass`'s `BudgetBill` overload is documented as computing the bill's direction and delegating
to the float core.

**PLAN ADJUSTED — `StandingDraftPair`/`DraftTrack` are NOT being used, and this document's expectation
that they would be is withdrawn.** The standing/draft *concept* is exactly right for budget line items;
the *widget* is not. It lays out from `rect.x` with hardcoded offsets (110px standing, +120px draft,
+130px for the delta pill) and ignores `rect.width` entirely, while the Budget Process columns are
variable-width — and this is the screen that already shipped a "panel rendering catastrophically narrow"
regression, plus a header clipping mid-word. Its rows also carry information the widget has no slot for
(rate ranges, "not implemented" states). Forcing it would risk re-breaking the most fragile screen in
the project to gain a readout it can't fully express.

**This is now the SECOND design-pack widget rejected on the same grounds** (after `SupportBar`), which
makes it a pattern rather than a coincidence: **the pack was authored against an assumed generic
political sim with assumed layout, so treat every remaining widget as a proposal to verify, not a
component to adopt.** `StatTile` and the card primitives passed that check and are in use; `SupportBar`
and `StandingDraftPair`/`DraftTrack` failed it. `Portrait` is superseded by real art (batch 3).

**Batch 6 — the amber draft cue: BUILT (2026-08-01)**, commit `78280c8`. **Not yet live-confirmed.**
Delivers the one genuinely load-bearing idea `StandingDraftPair` would have provided — its amber
"draft differs from standing" signal, which the pack calls "never optional" — via `DrawDraftLabel`,
with none of the fragile geometry. **25 call sites in a single pass** (the earlier "~21" estimate
undercounted): Crime & Justice 6, Labor 5, Minimum Wage, Trade base rate + per-partner overrides,
Sectors 5, Tax rates, Welfare generosity, Spending lines, and all three SWF drafts.

Three different models of "changed" exist in this codebase, each handled on its own terms rather than
forced into one shape: standing/draft pairs compare the two values; **spending drafts are a percentage
CHANGE**, so non-zero is the condition; **the SWF's own existence is a draft**, compared against whether
a fund actually stands. Anything added later needs the same case-by-case treatment.

Two cases deliberately stay neutral and should remain so: an **unimplemented** tax line or welfare
program is changed by its own standalone Implement/Remove bill, not by the slider beside it, so its
draft label must not light up regardless of what the inactive draft value holds; and the standing labels
themselves are precisely what has not changed.

#### Phase C status — the visual rollout is functionally complete

Batches 1-6 cover every tab, and nothing further is planned under 5e's visual scope. **Batches 4-6 are
BUILT but NOT live-confirmed** — that confirmation is the only outstanding 5e work, and until it happens
they must not be described as done. Suggested check: Policy/Laws (all five sub-categories), Budget
Process (all five categories, watching the amber cue appear on drag and the three columns hold at a
narrow window), and one bill introduced end to end.

**5e retrospective, worth carrying into any future art/widget work.** The design pack's components fell
into two groups. `StatTile`, the card/rounded-box primitives and the icon tinting were built against
assumptions this project actually holds, and are in production use. `SupportBar` and
`StandingDraftPair`/`DraftTrack` encoded mechanics and layout this project does not have — a
seats-based majority, and fixed-width columns — and were rejected only after being checked against the
real model; in **both** cases this roadmap had already recommended them before that check happened.
`Portrait` was superseded by real art. **Treat a plausible-sounding component as a proposal to verify
against the actual code, never as a fit already established.**

- **5f. FOLDED INTO 5e (2026-07-31)** — the aesthetic restyling pass originally scoped as its own later phase is now part of 5e's combined scope (see above), not a separate step. Kept here, not deleted, per this document's own practice of marking supersession explicitly rather than silently rewriting history. Original 5f scope: aesthetic restyling pass (reference image 1: rounded cards, dark theme, big-number/small-label hierarchy, progress-bar visualizations, generous spacing) — LAST, applied to the final consolidated 7-tab structure, not to tabs about to be merged/removed, precisely because restyling a screen that's still being consolidated/rewired means restyling it twice. **Prep material referenced below is now folded into 5e's own broader asset request, not held separately.**

**5f prep, superseded by 5e's own consolidated asset request — "PoliSim GUI redesign.zip" asset pack** (`G:\UNITY\Projects\PoliSim\PoliSim GUI redesign.zip`, still not yet imported) — **origin confirmed**: Windows' Zone.Identifier mark-of-the-web on the file shows `ZoneId=3` (Internet zone), `HostUrl=https://claude.ai/`, i.e. a browser download from claude.ai (a Claude Design handoff), not an unknown/untrusted source. **Full security review completed** before this was treated as trusted prep work: both C# files (`PoliSimTheme.cs`, `PoliSimWidgets.cs`) read line-by-line and grepped clean for `System.Net`/`System.IO`/`System.Diagnostics`/`System.Reflection`/`Process.Start`/`UnityEditor`/`WebRequest`/`HttpClient`/`File.`/`Application.OpenURL`/`PlayerPrefs`/`Socket` — zero matches; all 8 SVG icon sources read in full and confirmed pure static geometry (`rect`/`circle`/`ellipse`/`path` only, no `<script>`, no event handlers, no external references); all 9 PNGs verified as genuine PNG image data via magic-byte detection, scanned for embedded scripts/URLs/executable signatures with none found. **Two distinct pieces, two different statuses**: the C# theming/widget code (`PoliSimTheme.cs` design tokens + rounded-rect primitives, `PoliSimWidgets.cs`'s six widgets) is PURE PROCEDURAL DRAWING LOGIC — unaffected by the rule 10 reversal, since it was already compliant either way. The actual icon/texture image files (8 SVGs + 9 PNGs) were the genuinely different case rule 10's reversal above now explicitly clears for import - the 8 existing `SystemArea` icons cover 8 of the 11 areas (all but Infrastructure, Global, and Neutral) and are folded into `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`'s asset manifest as reusable, avoiding a duplicate request; the `menu_pattern_tile.png` background texture is likewise reusable as-is. **Still not yet imported into the project** - importing is a GameController.cs rendering change, explicitly deferred until Elias has reviewed the full 5e asset request and the remaining (new) assets are back.

**Open tie-in**: the Annual Budget tier explicitly includes SWF rate/allocation changes — this sharpens the existing "SWF emergency drawdown fast-track" Open Question below into something 5c/5d actually needs an answer to, not just a hypothetical. Resolve it before SWF is wired into the omnibus bill, not after.

#### Original step 5 plan (SUPERSEDED 2026-07-31 — historical record only, do not build)

**Rollout discipline**: PILOT on Tax Policy only first (master sequence step 4) — well-understood, clean implement/adjust/remove semantics already in place. Full validation matrix on the pilot before touching any other tab. Only then (step 5) roll out to the remaining seven, using the exact same uniform draft → introduce → vote → pass/fail pattern the pilot used, unchanged per tab.

## Part C — UI/graph restyling (MASTER SEQUENCE STEP 2) — DONE. See `COMPLETED.md`.

---

## Resolved Open Questions (from Roadmap Rounds 1-3 — historical, no action needed)

1. **Economic Sectors feedback**: Resolved INTEGRATE. Implemented as bounded nudges onto PotentialGrowthRate/Unemployment under an all-sources ceiling (MaxTotalPotentialGrowthAdjustment = 1.0). Real-Unity confirmed, growth rate observed pinned exactly at the ceiling under worst-case stress — direct evidence it binds correctly. Full detail: CLAUDE.md "Sector Integration."
2. **Infrastructure ConditionIndex feedback**: Resolved FEED BACK. Threshold-based drag on PotentialGrowthRate, reconciled with the pre-existing Infrastructure-spending nudge under one combined ceiling (0.75). Real-Unity confirmed. Full detail: CLAUDE.md "Infrastructure Feedback."

## Open Questions (live — add new entries here as they come up; do not resolve silently)

- **Cabinet appointment confirmation** — should appointing a minister also require a parliamentary vote, or does the player retain unilateral appointment power? Not yet decided.
- **SWF emergency drawdown fast-track** — LOAD-BEARING, not hypothetical: SWF rate/allocation changes have been part of the annual omnibus budget bill since 5c (DONE), so a genuine emergency drawdown can currently get stuck behind that country's next fiscal-year vote (up to a year away). **Recommendation (2026-07-31), pending Elias's confirmation**: emergency SWF drawdown becomes a standalone bill — the SAME tier 2/3 mechanism 5d already built (now real, not hypothetical - see 5d above) for new/removed programs and non-budget policy — not bundled into the annual budget, and NOT fully exempt like the Fed/Eurozone carve-out. Reasoning: real governments handle fiscal emergencies via expedited votes, not zero-oversight unilateral action; Norway's own GPFG withdrawal is itself an ordinary budget-process matter, not a central-bank-style independent decision, so a full exemption would overstate SWF's real-world independence. This needs zero new mechanism — it's exactly 5d's standalone-bill pattern, reused (most naturally as a fifth tier-3 bill type alongside Labor/CrimeJustice/Sector/Trade). Not yet confirmed — do not build against this until Elias signs off.
- **RESOLVED (2026-07-31) — "PoliSim GUI redesign.zip" icon/texture assets vs. working discipline item 10.** Elias explicitly reversed item 10: icons, portraits, and background/menu textures are now approved as imported sprite art (see item 10's own updated text above). `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`'s full 58-file request (all six original sub-questions plus a later tab-navigation-icon addition) has been answered by Elias, delivered by Claude Design, security-reviewed, and imported into `Assets/Art/UI/` (2026-07-31). Fully closed - no image assets remain outside the project. `GameController.cs` rendering work is still deliberately not started - see the new "5e implementation plan" open items directly below for what's actually gating that now.
- **RESOLVED (2026-07-31) — Phase A tab-placement calls.** All five confirmed by Elias; see Part B's "5e implementation plan" mapping table above for the final placements and reasoning. Kept here for the record:
  1. `Trade` tab - confirmed split: informational content (Trade Balance graph) to Statistics, policy content (the `TradePolicyBill` and per-partner override rows) to Policy/Laws.
  2. `FederalReserve` tab - confirmed Politics.
  3. `PolicyWeb` tab - **Policy/Laws, NOT Statistics** (overrides the original recommendation) - Elias's own reasoning: "it's a relationship/reference tool consulted while deciding what to change, closer to where bills get drafted than to a pure stats readout."
  4. `Infrastructure` tab - confirmed folding into the Tax/Spending (Budget Process) destination alongside Welfare/SWF.
  5. Budget Process mandatory-pause interrupt surfaces under Decisions too - confirmed. Elias's own reasoning: "any 'time is blocked until you respond' state belongs in the same place, not treated as an exception."
- **Real reporting lag for data releases** (Continuous Time Migration) — optional realism refinement, not required for a first pass.
- **"Ongoing-process budgets"** (Continuous Time Migration Phase 0, item 5) — RESOLVED IN DESIGN (2026-07-31): this is now Master Sequence step 5's Annual Budget bill tier — see Part B above for the full design (real per-country fiscal-year dates, USA-only mandatory pause, the rest AI-resolved). Implementation is that plan itself (phases 5a-5c), not yet built.

---

## When Elias returns to this document

- Check the Master Sequence section — confirm which step is actually in progress or next, don't assume.
- Check Open Questions first.
- Review the commit log — each step should be its own commit(s), validation results in the message or CLAUDE.md.

### RESOLVED (2026-08-01) — `SimulationRandom` stream position across save/load

**Decided: the counting shim.** Record draws per stream, re-seed and fast-forward on load. *"Reversible
beats permanent under uncertainty; the xorshift option can be revisited once save/load exists and real
load times are known."* Preserves every recorded baseline; pays an O(draws) loop per load. Implement as
part of Master Sequence item 8.

### RESOLVED (2026-08-01) — `PublishedData.PeriodClosingValues` retention

**Decided: keep everything, no pruning.** The data is small, and the failure pruning risks — a revision
converging on a missing closing value — is a bug this project has already fixed once (`ea0a6a4`). Flatten
to `{stat, periodStart, value}` records on save, rebuild the dictionary on load.

### RESOLVED (2026-08-01) — harness swing-check coverage

**Decided: leave it at five fields, and stop describing "N anomalies" as a whole-simulation health
measure.** Extending would mean ~24 threshold choices plus a third baseline discontinuity in a single
day, and several fields (NetMigrationRate, PopulationGrowthRate) legitimately exceed 20% turn-over-turn,
so blanket coverage would bury real signal in noise.

**The fix is documentary, not mechanical.** Every doc quoting anomaly counts now states plainly that it
is a **5-field check** — GDP, Unemployment, Inflation, InterestRate, DebtToGdpRatio — while `CheckFinite`
is complete at **29/29**, so no NaN can escape. **Revisit if something ever slips through unnoticed.**

### RESOLVED (2026-08-01) — B2 shows LIVE values, not published

**Decided: live, as built.** *"Your reasoning is better than the directive's."* A lagged preliminary
figure in a "what am I doing right now" panel misrepresents itself, and the directive's instruction was
only partly satisfiable — 6 published stats against 18 policy-screen stats. `POLISIM_MACRO_OVERHAUL_DIRECTIVE.md`
has been corrected to match rather than left contradicting the code.

**Noted for the record:** the deviation was correct, and escalating it was still right.

### RESOLVED (2026-08-01) — C4 built out of order, deliberately

**Decided: build C4 (credit rating) before C1–C3.** This departs from the Master Sequence's
"top to bottom, do not skip ahead" rule, and the reason is recorded here specifically so it does not
become precedent for casual skipping:

1. **C4 depends on nothing in C1–C3.** It is `[DERIVE]` — computed from debt-to-GDP, deficit trajectory
   and growth, all already tracked — and consumes no seed data from the earlier batches.
2. **Its blocker is external and may persist indefinitely.** C1–C3 need figures requiring direct
   Eurostat/OECD database access, which no session here can obtain.

Skipping is justified when a later item is genuinely independent *and* the earlier blocker is outside the
project's control. Neither condition alone is sufficient.


### RESOLVED (2026-08-01) — Step C1: which stat is the primary housing metric?

**Decided: homeownership rate, reversing the directive's recommendation.** Overburden remains the better
concept — affordability stress rather than tenure, responsive to interest rates and housing assistance,
both live levers — but a gap-closing attempt confirmed only **2 of 6** verified (Germany 12.0, Sweden
10.6). Italy, France and Poland are `[BOUNDED]` to 4.0–9.0, honestly derived from Eurostat naming only
the countries above 9.0 and below 4.0, and a bound is not a value. Seeding four countries from a range
would be inventing precision.

Homeownership has **4 of 6** verified and preserves the sharpest real contrast in the data, Germany ~47%
against Poland ~87%. Overburden is deferred as a secondary metric pending exact `ilc_lvho07a` figures,
which need direct Eurostat database access rather than search.

### RESOLVED (2026-08-01) — Step C1: how should the USA housing figure be handled?

**Deferred with the metric.** The question was that Eurostat overburden measures >40% of disposable
income while US convention measures >30% or >50%, so no comparable figure exists. With homeownership as
the primary metric the USA has a genuinely comparable verified figure (65.3%), so this stops blocking
C1. It returns only if overburden is later added as a secondary metric.

### RESOLVED (2026-08-01) — Step C1: which measurement basis for homeownership rate?

**Decided: OECD Affordable Housing Database, share of HOUSEHOLDS owning.** The suspicion that the table
mixed bases was correct and understated — Germany's spread is three-way (OECD households 41.0 /
dwelling-based ~46.7 / Eurostat nationals-only 52.3), an 11.3-point range across three correct-for-their-
source definitions, with Eurostat's population base (68.4% EU) as a fourth.

Single-basis set: USA 65.3, France 58.5, Germany 41.0, OECD average 70.1. Poland leaves the verified set
— its ~87.9 is a Eurostat nationals line — joining Italy and Sweden as gaps on this basis.

**Consequence: the C1 margin is 3–2, not the 4–2 that justified the decision.** The decision holds
(three same-basis figures beat two, and overburden's gaps are unobtainable by search while these are
ordinary lookups) but by one country rather than two. See `STEP_C1_HOUSING_GAP_REPORT.md`, which also
corrects an overstated claim about the Germany-vs-Poland contrast.
