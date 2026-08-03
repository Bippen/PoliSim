# PoliSim — Master Roadmap

This replaces three previously-separate standing documents (`ROADMAP_BRIEF.md`, `CONTINUOUS_TIME_MIGRATION.md`, `POLITICAL_SYSTEMS_OVERHAUL.md`), which had grown real dependencies on each other without being coordinated in one place. Read this in full before starting anything.

---

## Non-negotiable working discipline (applies to everything below, no exceptions)

0. **SCALE VALIDATION TO RISK (added 2026-08-02).** Real Unity stays the standard of truth — rule 1 is
   unchanged — but the *size* of the check matches what the change can actually break:
   - **Simulation math** → full matrix, 100 and 500 turns, like-for-like before/after.
   - **UI-only** → compile check plus a smoke run. A change that cannot reach simulation math cannot move
     a trajectory; `BatchSimulationRunner` never calls `OnGUI`.
   - **Data-layer additions nothing calls yet** → compile check.

   **Three further standing cuts, same date:** stop creating new standing documents (findings go in
   `CLAUDE.md`, status in this file); the verification-integrity log **stops at 10** — later instances get
   one line, not a numbered write-up; and **batch the reporting** — work through several items and report
   once, unless something fails or needs a decision.

   ⚠ **What does NOT get cut**, because each caught a real defect in the last two days: real-Unity
   validation before anything is called done; never inventing a `[GAP]` figure; visual work being
   built-not-confirmed until Elias sees it; verifying against commits and callers rather than summary
   memory; and the API cross-check gate.

1. **Real Unity is the standard of truth, not the standalone harness.** It has been wrong about project state multiple times this project (a stale swing threshold, an interest-rate crash mischaracterized as noise, a debt trajectory that flatly contradicted real Unity). Use it for fast iteration only. Before considering *anything* done, validate via `BatchSimulationRunner` against real Unity (`G:\UNITY\Unity Hub\6000.5.6f1\` - migrated from `6000.5.4f1` on 2026-08-01 after the older install became corrupted; see CLAUDE.md's "Real-Unity Validation is the Standard Path" for the full story) at both 100 and 500 turn horizons (or their day-equivalent, once the continuous-time migration changes the unit).
2. **Watch for the six failure patterns already seen repeatedly**: turn-1 discontinuities, oscillation, unbounded/compounding growth, bimodal attractors, and two new ones (both new as of Continuous Time + Parliament + Cabinet/Foreign-Policy coexisting, both surfaced investigating the SAME reported live-play freeze):
   - **Background/timed state mutation vs. active UI interaction** — a background system (a bill resolving, or any future timed/probabilistic mechanic) mutating live state that a GUILayout control is reading, on a day/frame the player has an active multi-frame drag in progress on that exact control. GUILayout allocates control IDs positionally, not by a stable key, so a control disappearing or a preceding control's count changing mid-drag is a documented Unity IMGUI hang/desync trigger, especially inside a ScrollView — and it's invisible to `BatchSimulationRunner`, which applies policy decisions programmatically and never drives real OnGUI/mouse-drag events, so no batch run can ever catch it. First hypothesized in the Tax Policy tab (Master Sequence step 4 pilot) when a pending TaxBill could resolve while the player was mid-drag on a rate slider; hardened there via the stable-control-layout pattern (see `GameController.DrawTaxPolicy`'s doc comment, commit `adb34ae`) regardless — every control a gated tab can ever draw renders every frame, in the same order, with "not currently applicable" expressed via `GUI.enabled = false` (composed with, not clobbering, any ambient enabled state) rather than by omitting or swapping the control. **Caveat, recorded honestly**: this fix did NOT resolve the reported freeze — Elias reproduced it again under the same conditions after commit `adb34ae`. The pattern and fix are still real and worth keeping (every one of the seven remaining tabs gains this exact same theoretical exposure once Master Sequence step 5 wires them into the draft/bill/vote model), but it was not the actual trigger of the original report. See the next pattern for what the investigation found instead.
   - **A legitimately time-blocking decision with no globally-visible indicator** — Fed Chair term appointment, a Cabinet decision, and a Foreign Policy meeting all correctly pause `GameController.Update`'s day-loop (every gate is checked correctly - this is NOT a simulation bug), but each one's actual resolution UI (the Fed Chair candidate picker, `DrawCabinetDecisionModal`, `DrawForeignPolicyMeetingModal`) renders ONLY inside its own specific tab's draw call - never globally. A player on any other tab (e.g. Tax Policy) when one of these fires sees simulated days silently stop advancing with no visible cause - indistinguishable from a hang. Before the fix, `DrawCalendarAndSpeedControls`'s always-visible status line (the one piece of UI pinned outside the scroll view on every tab) named the reason for Fed Chair and Cabinet only, in a modest, easy-to-miss label style, and said NOTHING for a pending Foreign Policy meeting - the one of the three statistically most likely to fire early in a fresh session, since it rolls per DAY (~1% chance) rather than per 121-day TURN like the other two. Fixed by escalating that line to the same bold/orange `_eventBannerStyle` used for the dashboard's own BREAKING banner whenever ANY of the three is pending, always naming which one and which tab resolves it - still exactly one Label control either way, per the stable-control-layout pattern above. This is a genuine UX gap, not a code crash: every future interrupt/decision system (gated legislation on the remaining seven tabs very much included) needs its "something needs your attention" state represented somewhere visible from every tab, not only on the tab where it originated.

   Assume a new mechanic is guilty of all six until the full-horizon batch run (for the first four) and direct live-Editor confirmation (for the last two, which batch runs cannot exercise) prove otherwise.
3. **Commit per unit of work.** One feature, one commit, descriptive message. Confirm staged contents match the message before committing.
4. **DECIDE IT YOURSELF (amended 2026-08-02 — the old rule was producing days of round-trips).** The
   escalate-don't-guess rule was for genuine design forks; in practice it escalated things like where a UI
   tile goes. **New standard: make the call, state the decision and its reasoning in the commit message,
   and flag it for Elias to overrule.** Escalate only when undoing it would be expensive or irreversible —
   a data model change, a mechanic other systems will build on, anything touching the fiscal engine.
5. **Ground new mechanics in real data.** Label anything stylized honestly — never let a placeholder look like real data.
6. **Scope every new system small on the first pass.** Plumbing plus a few clearly-justified effects, not full theoretical richness.
7. **Update CLAUDE.md after every item**, including validation results, so history stays traceable.
8. **Verify Unity processes actually exited** (`Get-Process Unity*,UnityPackageManager`) before trusting that a closed window means it's safe to run a batch validation — confirmed to cause false failures more than once.
9. **All new named entities (cabinet ministers, party names, legislators) are original and fictional** — never real people or real political parties. Same rule the Fed Chair mechanic already established, extended to every new character/entity going forward.
10. **REVERSED (2026-07-31), was a hard rule through Master Sequence step 5d**: visuals are now a MIXED procedural/sprite model, not "all procedural." Elias has explicitly approved imported sprite art for **icons, portraits, and background/menu textures specifically** — see `CLAUDE_DESIGN_ASSET_REQUEST.md` (the single standing asset request; the original 5E/chrome/macro requests were consolidated into it 2026-08-02, all delivered) for the asset work this decision unblocked. **Stays procedural, unchanged, no exception**: all UI chrome/layout (`PoliSimTheme.cs`'s `RoundedBox`/`RoundedCard`/`Pill`/`Rule`/`TopAccent`/`LeftSpine` — pure `GUI.DrawTexture` rounded-rect/line geometry, no art asset, no reason to change) and every existing DATA visualization (`GraphRenderer`, `MapRenderer`, `PolicyWebRenderer`, `PoliticalCompassRenderer`, `HemicycleRenderer`) — none of these draw a "picture," they render real tracked simulation data, which is exactly what rule 5 ("ground new mechanics in real data") already protects; nothing about the icon/portrait decision touches that. **Becomes sprite-based**: one icon per `UiPalette.SystemArea` (policy area), one portrait per Cabinet minister candidate, one emblem per `PartyArchetype`, and background/menu textures — all sourced from Claude Design with the same origin-verification and security-review discipline already established for the first pack (Zone.Identifier mark-of-the-web check, full code/asset read-through before treating anything as trusted). This is a real, deliberate policy reversal, documented as such per this same working-discipline section's own precedent for recording a caveat/correction honestly rather than letting it look like silent drift - any FUTURE reversal of a standing rule must be recorded the same explicit way.
11. **Any new mechanic that nudges an existing tracked variable must fold into that variable's existing combined ceiling**, not add an uncounted new source — audit the actual ceiling code before adding a contributor, don't assume there's room.
12. **NEW (2026-08-02) — "awaiting delivery" is a status that must be RE-DERIVED FROM THE FILESYSTEM, never trusted from a document.** Two separate assets were recorded as outstanding while already sitting in zips at the project root: `icon_stat_interestrate` (registered *"REQUEST SENT, awaiting delivery"* on the day it in fact arrived) and `menu_pattern_tile.png` (delivered, then unimported for weeks while three documents named it as a gap). **Neither register was wrong when written.** Nothing watches the project root, a delivery does not announce itself, and so the status simply outlived the fact — twice, which is what makes it a pattern rather than an oversight. Both gaps were eventually closed only because Elias happened to say the file already existed. **Run `DeliveredAssetCheck` before reporting any asset as outstanding**: it compares every zip's contents against what exists under `Assets/` and fails on any gap, which is the one comparison that cannot go stale. Its companion `StatIconCoverageCheck` asks the runtime half of the same question — that a name the UI hard-codes actually resolves through `Resources.Load`, which a file merely existing on disk does not guarantee when its `.meta` is hand-written. The general form: **a status describing the outside world is a cached value, and needs an expiry.**

---

## Where things stand right now

**This document holds only live work.** Two companions carry everything else, and the split is the
standing pattern — see "Document set and the consolidation rule" at the bottom of this file.

| Document | Holds |
|---|---|
| `COMPLETED.md` | Finished work. This file shrinks into it, never grows |
| `MISSING_PREREQUISITES.md` | Work that is *waiting* on a named party. Not startable, so not live |
| `CLAUDE.md` | The detailed technical record for both. **Never superseded** |

**A task is only live here if someone could start it today.** Blocked → `MISSING_PREREQUISITES.md`.
Finished → `COMPLETED.md`. Built-but-unconfirmed and built-but-uncalled are **neither** — they stay live,
because they are not done.

- **DONE** — Master Sequence steps **1, 2, 3, 4 and 5 (all of it)**; Roadmap Rounds 1–3 (15 items); macro
  overhaul Steps A1–A3 and D. See `COMPLETED.md`.
- ✅ **MASTER SEQUENCE STEP 5 IS CLOSED (2026-08-02).** Elias reviewed all eleven items live as USA, then
  re-reviewed the five that had failed or carried caveats. **All eleven are confirmed.** Full record in
  `COMPLETED.md` section 16; `VISUAL_REVIEW_BACKLOG.md` was deleted per the standing pattern rather than
  left as an empty shell.
  - Three of the four failures were real defects, and each left a permanent check behind —
    `MoneyFormatDiagnostic`, `GraphRendererDiagnostic`, `StatIconCoverageCheck`.
  - **Items 7 and 8 passed without either being touched**, because both were blocked on item 3's unit bug
    rather than on marker design. Sequencing them behind it instead of iterating saved the wasted pass.
  - The `[DEBUG]` publication-lag dump is **removed** — item 8 passed, which was its whole purpose.
  - **Round 4 scoping is unblocked**, having been gated on exactly this.
- ✅ **macro Step A4 — DONE 2026-08-02.** Its derived stats are on screen (Statistics → Domestic, under
  the headline tiles): GDP per capita, tax burden, government spending, deficit/surplus and sector
  shares. It had been built and trajectory-validated but displayed nothing, which is the "built but
  uncalled" state this project repeatedly mistook for done. **Needs a visual look**, like anything new.
- **WAITING, NOT LIVE** — `MISSING_PREREQUISITES.md` is down to **one upstream defect and one blocked
  task**: Step C4's closure (section F), and cabinet portraits (section D1, waiting on three portfolios
  being authored). ~~16 figures needing database access~~ — **all sourced 2026-08-02; section B is empty
  and C1/C2/C3/C5 are buildable.** *Decisions, database access, Claude Design and the visual reviews all
  emptied on 2026-08-02.* Three quality debts survive in section B — none blocks a batch.
- 🔴 **HIGHEST-PRIORITY DEFECT — the debt-to-zero bimodality.** Dedicated entry immediately below.
  Promoted 2026-08-02: it now blocks a step AND is player-visible, neither of which was true before.
- **NOT STARTED, UNBLOCKED** — Master Sequence item 6 (**Round 4 — now scopeable, its gate cleared when
  step 5 closed**) and item 7 (Continuous Time Phases 1–5), both **weeks** of work; item 8 (save/load,
  scoped only, and first in the agreed execution order).

**Built 2026-08-01/02, all now reachable**: macro Step A4 (`70798e9`), Step C4 (`76a8f35`), and B2
rendering (`5701a04`) wired at sub-screen granularity (`4869476`). C4 is placed on the dashboard tile
grid beside Debt-to-GDP (`3d77b11`) — **placement CONFIRMED** by review item 11.

**Trajectory validation, run 2026-08-02** (`3d77b11`; full matrix, 15 scenarios × 100 and 500 turns,
`-seed=777`, real Unity 6000.5.6f1). Both were validatable only once `SimulationTestRunner` evaluated
them per turn — a dashboard tile is OnGUI code and `BatchSimulationRunner` never calls `OnGUI`, so
placement alone would not have covered them.

- **A4 — PASSES.** Zero finiteness failures across all 30 runs, all six countries, every turn. Its
  outstanding "NOT trajectory-validated" caveat from `70798e9` is discharged.
- **C4 — IMPLEMENTATION COMPLETE** (`a4155ca`). The first run failed with 3,421 rating-thrash anomalies;
  Elias's A1 ruling fixed it by review CADENCE rather than damping. Anchors hold **5 of 5, unchanged**;
  matrix anomalies fell to **1,416**. The residual is **not a C4 defect** — see the entry directly below.
  **Closure, not the build, is what remains outstanding.**

---

## ✅ The debt-to-zero bimodality — FIXED 2026-08-02. Its successor defect is named at the end.

**The floor is gone, debt may go negative, and the 0.00% pinning with it** — debt-swing anomalies fell
60% across the full matrix (6,225 → 2,507, 100 and 500 turns, seed 777, like-for-like before/after).
A symmetric −300% bound was needed to stop an unbounded negative runaway; see Open Questions, because that
bound is my call rather than Elias's.

🔴 **What it was hiding, and what now carries the priority: the rating thrash is the DEFICIT term's.**
Removing the floor moved rating anomalies by 1.6% (1,416 → 1,394) while
`DebtClampDiagnostic` reports the debt stock's own contribution as almost perfectly stable — 0 notch moves
in 117 years for four of six countries. **Step C4's closure waits on the deficit term, not on this.**
Full evidence in `CLAUDE.md`. The original entry follows, kept because the mechanism reasoning is what
made the fix possible.

### Original entry — the defect as it stood before the fix

**What:** Sweden's, France's and Germany's `DebtToGdpRatio` collapses to exactly **0.00%** and, under
stress, spikes back to ~44% and collapses again within a year. Sweden in plain `baseline`: 21.8% (turn 1)
→ 0.90% (turn 25) → **0.00%** from turn 50 on. Full technical history in CLAUDE.md, "SpendingLine Amount
Ceiling — Debt-to-Zero Fix"; this is roadmap failure pattern 4, bimodal attractors.

**Not new. Its PRIORITY is new**, and for two specific reasons:

1. **It now blocks a step.** Step C4's closure waits on it — see `MISSING_PREREQUISITES.md` section F1.
   No previously-known consequence of this defect blocked anything; it was a background modelling
   concern that batch runs reported and nothing acted on.
2. **It is now player-visible.** Until 2026-08-02 this defect was **log-only** — it lived in anomaly
   counts, batch summaries and prose. Step C4's credit rating is the **first instrument that surfaces it
   on screen**: the tile sits in the dashboard grid on every tab and reports its input faithfully, so a
   debt stock swinging 0%↔45% now reads as a rating visibly collapsing and recovering. **The defect did
   not get worse — it got a display.**

**Do not fix this by damping the rating.** That option was raised and explicitly rejected in A1, and
doing it now would return the defect to log-only while making C4 dishonest. A derived stat that stayed
calm while its inputs did this would be the broken one.

**Scope note:** the affected set is exactly the documented one. USA, Italy and Poland have well-behaved
debt trajectories and produce **zero** rating anomalies both before and after the cadence change, which
is itself evidence the rating is reading faithfully rather than misbehaving.

### DECIDED 2026-08-02 (Elias, delegated) — allow net government debt to go NEGATIVE

**Approach: remove the zero floor rather than damp the symptom.** `Mathf.Clamp(debt, 0f, maxDebt)` is
what creates the bounce artifact — a stock driven below zero is held at zero and then released, which is
exactly the shape a bimodal attractor takes.

**Why negative debt is correct rather than a hack.** A country whose sovereign wealth fund exceeds its
debt is a **net creditor**, which is a real fiscal state — and specifically **Norway's**, the country this
project already used to calibrate SWF returns. The game *already displays* "Net Government Position (debt
minus fund assets)"; it is only the simulation that refuses to represent it. Clamping at zero encodes an
assumption the UI has already rejected.

⚠ **DO NOT IMPLEMENT UNTIL THE MECHANISM IS CONFIRMED.** Verify against a real trajectory that the zero
clamp is what produces the 0.00% → ~44% swings, and establish whether it **fully** explains the −135.5% to
+170.8% settled-deficit range or whether something else contributes. **Three wrong theories preceded the
right one on the Unity batch-run hang** — that precedent is why this is gated. Report the mechanism before
proposing an implementation.

**MECHANISM CONFIRMED 2026-08-02 — the gate is satisfied, implementation may be scoped.** Full evidence in
CLAUDE.md. In short: the FLOOR is the mechanism (Sweden 67/120 baseline turns, France 14/120); the
**ceiling is never hit by anyone**, so `MaxDebtToGdpPercent` is not involved; and the affected set is
exactly "countries whose SWF drives net position negative" — which explains Germany, whose anomalies occur
**only in `swfstress`**, where its debt does reach 0.0% repeatedly. Elias's premise holds and is stronger
than stated: Sweden is a net creditor from **turn 1**, reaching a net position of −599 by turn 16, with
single-turn excursions to −64.3% of GDP.

⚠ **It does NOT fully explain the deficit range.** The per-turn budget balance is itself volatile
(Sweden: +79, +16, +48, +0.8, +30, −40 …) and that volatility is upstream of the clamp. Removing the floor
should eliminate the 0.00% pinning and the bounce; whether it eliminates the rating thrash entirely is
**not** established. Re-run `DebtClampDiagnostic` with the floor removed and check year-over-year deltas
against the notch threshold — if they still clear it, the residual is budget-balance volatility and is a
separate defect this one was hiding.

⚠ **Design decision to settle before building:** with debt clamped at zero, interest on debt is zero, so a
net creditor currently earns nothing on its net assets. Removing the floor without deciding how negative
debt interacts with `GetInterestOnDebt` creates either free money or a new asymmetry.

---

## THE MASTER SEQUENCE — work this list top to bottom, do not skip ahead

### ⚠ EXECUTION ORDER FOR ITEMS 6–8 CHANGED 2026-08-02 (Elias, delegated)

**The numbering below is deliberately NOT changed** — items 1–8 are referenced throughout this file and
`CLAUDE.md`, and renumbering would break every reference. Only the order of work changes:

| Work order | Item | Why |
|---|---|---|
| **1st** | **8 — save/load** | Already scoped and decided (serializer, three-layer scope, counting shim). Every crash or Editor restart currently destroys all state. And Phases 4–5 need heavy multi-turn validation that is painful without persistence |
| **2nd** | **7 — Continuous Time Phases 1–5** | Must precede Round 4 |
| **3rd** | **6 — Round 4** | Round 4 would add systems that Phases 1–5 must then convert to daily granularity — **doing the work twice** |

**The Round 4 reasoning is the load-bearing part, and it is item 6's own argument applied one level up.**
Item 6 already says to scope it only once step 5 closes, so new work is built against the
gated-legislation model from day one. The same logic applies to the daily-granularity conversion: build
Round 4 against BOTH finished foundations, not one. *Item 6 also remains gated on step 5 closing — that
dependency is unchanged.*

This is the one authoritative order, replacing whatever each original document separately suggested. It exists because Political Systems Overhaul Part B depends on Continuous Time Phase 0, and because building new Roadmap features or converting existing systems to daily granularity while Parliament's gating is mid-rollout would mean touching the same code for two different reasons at once — exactly the kind of overlap this project's discipline exists to avoid.

1. **Part A (Cabinet). DONE** — see `COMPLETED.md`. *Limitation: 3 of 6 portfolios implemented.*
2. **Part C (UI/graph restyling). DONE** — see `COMPLETED.md`.
3. **Continuous Time Phase 0. DONE** — see `COMPLETED.md`. Calendar/UI only; changed no economic math.
4. **Part B, PILOT (Tax Policy tab). DONE** — see `COMPLETED.md`.
5. **Part B, full rollout (5a–5f). DONE 2026-08-02** — see `COMPLETED.md` sections 10 and 16. 5e's Phase C
   batches 4–6 were the last live part, and Elias's review confirmed them. Scope absorbed 5f.
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

   **RESOLVED (2026-08-01) — the counting shim.** Elias's ruling: reversible beats permanent under
   uncertainty, and the xorshift option stays revisitable once save/load exists and real load times are
   known. It preserves every recorded baseline, and its cost is bounded by draws-per-game rather than
   by anything unbounded. **This is the version to build.**

   **Gap 2 — `PublishedData.PeriodClosingValues` is keyed by a `ValueTuple`.** *(Key type widened to
   `ClosingStat` on 2026-08-02; still a `ValueTuple`, so this gap stands unchanged.)* Its declared type is
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

   - **Step A1–A3 — release calendar, published series, revisions. DONE (2026-08-01).** See `COMPLETED.md`.

     ⚠ **CORRECTED 2026-08-02, and it is the kind of error this consolidation exists to catch.** This line
     previously read *"Step A — release calendar, published series, revisions, Tier 0 derived stats. DONE
     (2026-08-01), commit `e3a0feb`"* — wrong on two counts. `e3a0feb` contains exactly two files,
     `PublicationSystem.cs` and `SimulationManager.cs`. `DerivedStats.cs` was not added until `70798e9`,
     whose own message says "NOT trajectory-validated". **A4 was folded into a DONE marker for a commit
     that did not contain it**, and stayed there for a day.
   - **Step A4 — Tier 0 derived stats. DONE 2026-08-02.** Built (`70798e9`), trajectory-validated
     (`3d77b11` — zero finiteness failures across the full matrix), and now **displayed**: a "Derived"
     panel on Statistics → Domestic carries GDP per capita, tax burden % GDP, spending % GDP, the signed
     deficit/surplus and sector shares. The directive defines A4 as *"pure display arithmetic"*, so it was
     not done while it displayed nothing — for a full day, with four of its six methods reachable only
     from the test harness. **The lesson is the roadmap's own: check callers before believing a feature
     exists.**
   - **Step B — graph overhaul + contextual policy-screen stats.** Display only, depends on A. EXTEND
     `GraphRenderer`, do not build a parallel system. **B1 built (`dd7e323`), B2 built and wired
     (`4869476`) — both await visual confirmation**, backlog items 3 and 10. Neither is done.
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

- **Phase 1: Sectors and Infrastructure. ✅ DONE 2026-08-02 (`321a10e`).** Aggregation-equivalence 28/28,
  max drift 0.0004% against a 3% bar; full matrix anomaly counts identical to the pre-phase baseline.
  **The methodology is proven** — the two constants took *different* shapes (a gap-closing fraction is
  multiplicative, a decay rate is linear) and the sector sensitivities correctly took none at all, which
  is the distinction the rest of the phases turn on. Investment stayed on the turn boundary as a discrete
  budget action; see the commit for that decision.
- **Phase 2: Labor Market and Crime & Justice. ✅ DONE 2026-08-02 (`275e014`).** Aggregation-equivalence
  34/34, max drift 0.036%; matrix 1306/140/116/93/19 against a 1305/… baseline — one extra inflation
  anomaly out of ~1,670, traced to the known confidence-path residual. **The `PerDayReversion` helper now
  exists and is shared**, so Phases 3–5 reuse one proven conversion rather than deriving their own.

  ⚠ **HANDOFF FOR PHASES 4–5.** The pattern is established and mechanical for anything shaped
  `state.X += speed * (target − X)`: add a default `reversionSpeed` parameter, add a `…Daily` wrapper
  passing `PerDayReversion(turnSpeed)`, move the call from `AdvanceTurn` to `AdvanceDay` preserving
  order, leave `PreviewTurn` on the turn form, then extend `AggregationEquivalenceCheck`.
  **What is NOT mechanical, and is where the remaining risk lives:**
  - **Accumulating terms with no target** take the LINEAR transform, not the multiplicative one — see
    `ApplyCrimeEffects`. Every remaining phase has some.
  - **A constant that is a POLICY STANCE rather than a flow may need to stay frozen for the period** —
    Phase 3's fiscal reaction multiplier is the worked example, and the only Phase 1–3 constant that
    failed the bar on its first shape. Ask of every remaining constant: is this a quantity that flows,
    or a decision that was taken? Decisions belong to the boundary that took them.
  - **Phase 4's `YearsPerTurn`** is a direct turn-length dependency and has produced two prior structural
    bugs; the roadmap's own instruction is a throwaway diagnostic BEFORE the matrix.
  - **Phase 5 is the core macro engine** and has the project's worst record for hidden instability. It is
    last on purpose and should start with full attention, not at the end of a long session.
- **Phase 3: Tax portfolio, Welfare, Spending categories, SWF — the fiscal engine. ✅ DONE 2026-08-03.**
  Part 1 (`42a499f`) moved `PovertyRate`; part 2 moved the money. Aggregation-equivalence 39/39, Phase 3's
  own max drift 1.35% against a 3% bar; full matrix at 100 and 500 turns, same seed, like-for-like:
  **25 of 30 combinations byte-identical**, 1629 → 1637 anomalies, and the only two categories that moved
  at all are the two directly downstream of the debt path (DebtToGdp swings 139 → 145, credit-rating notch
  moves 18 → 20) — Inflation, Unemployment and InterestRate counts are unchanged to the anomaly.
  `DebtClampDiagnostic` reports zero ceiling hits, zero negative-debt turns and zero runaway-guard hits
  before and after; `CreditRatingAnchorCheck` is unchanged at 5/6 with Poland's known expected failure.
  Every country's 120-turn debt ends 1–5% lower, all in the same direction, which the interest change
  below accounts for.

  **What moved and what deliberately did not.** Revenue, unemployment benefits, welfare, interest, the
  SWF's contribution/return/draw and the debt stock itself now accrue daily. The BUDGET RESOLUTION stays
  on the boundary, because a budget passing is an event on a date rather than a flow — so a plan is
  resolved at one boundary and executed over the following 121 days. That ordering is forced rather than
  chosen, and its one player-visible consequence is worth stating: **a policy change's CASH effect now
  lands one period after the boundary that made it.** Its effect on the GDP identity's G term does not
  move, because that is Phase 5.

  ⚠ **THE ONE CONSTANT THAT FAILED ITS FIRST SHAPE, and the lesson that generalises.** Recomputing
  `GetFiscalReactionMultiplier` daily — the obvious "more continuous" choice — failed the bar outright
  (Sweden 24.8% drift on budget balance, Germany 22.7%). Not a bug: `FiscalReactionSensitivity` is 1.5 and
  one period moves a country's debt ratio ten points or more, so a multiplier re-reading that ratio daily
  walks down its own surplus during the period it is supposed to be governing. Freezing it per period
  passes at 0.45%/1.35% **and is the better model**, because the mechanism is a fiscal *stance* and a
  stance is adopted when the budget is set. Full write-up in `CLAUDE.md`.
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

**Phases A and B, and Phase C batches 1–3: DONE and live-confirmed.** Consolidated to `COMPLETED.md` §10
on 2026-08-02, including the two lasting lessons from batch 3 (the `SupportBar` widget mismatch and
`Mathf.Sign(0f)`) which are worth more than the batches themselves.

#### Phase C — Rollout to remaining 6 tabs

Only after Phase B is confirmed. Apply the same now-proven pattern to Decisions, Demographics,
Tax/Spending (Budget Process), Policy/Laws, and Politics - NOT all 6 simultaneously. Split into 2-3 at a
time, the same discipline the original Parliament gating rollout used (step 5's own revised design
explicitly avoided touching all seven remaining tabs in one pass, for exactly this reason). Screenshot
and confirm after each batch before continuing to the next.

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

**5f prep, superseded by 5e's own consolidated asset request — "PoliSim GUI redesign.zip" asset pack** (`G:\UNITY\Projects\PoliSim\PoliSim GUI redesign.zip`, still not yet imported) — **origin confirmed**: Windows' Zone.Identifier mark-of-the-web on the file shows `ZoneId=3` (Internet zone), `HostUrl=https://claude.ai/`, i.e. a browser download from claude.ai (a Claude Design handoff), not an unknown/untrusted source. **Full security review completed** before this was treated as trusted prep work: both C# files (`PoliSimTheme.cs`, `PoliSimWidgets.cs`) read line-by-line and grepped clean for `System.Net`/`System.IO`/`System.Diagnostics`/`System.Reflection`/`Process.Start`/`UnityEditor`/`WebRequest`/`HttpClient`/`File.`/`Application.OpenURL`/`PlayerPrefs`/`Socket` — zero matches; all 8 SVG icon sources read in full and confirmed pure static geometry (`rect`/`circle`/`ellipse`/`path` only, no `<script>`, no event handlers, no external references); all 9 PNGs verified as genuine PNG image data via magic-byte detection, scanned for embedded scripts/URLs/executable signatures with none found. **Two distinct pieces, two different statuses**: the C# theming/widget code (`PoliSimTheme.cs` design tokens + rounded-rect primitives, `PoliSimWidgets.cs`'s six widgets) is PURE PROCEDURAL DRAWING LOGIC — unaffected by the rule 10 reversal, since it was already compliant either way. The actual icon/texture image files (8 SVGs + 9 PNGs) were the genuinely different case rule 10's reversal above now explicitly clears for import - the 8 existing `SystemArea` icons cover 8 of the 11 areas (all but Infrastructure, Global, and Neutral) and are folded into the asset manifest as reusable, avoiding a duplicate request; the `menu_pattern_tile.png` background texture is likewise reusable as-is. **Still not yet imported into the project** - importing is a GameController.cs rendering change, explicitly deferred until Elias has reviewed the full 5e asset request and the remaining (new) assets are back.

**Open tie-in**: the Annual Budget tier explicitly includes SWF rate/allocation changes — this sharpens the existing "SWF emergency drawdown fast-track" Open Question below into something 5c/5d actually needs an answer to, not just a hypothetical. Resolve it before SWF is wired into the omnibus bill, not after.

#### Original step 5 plan (SUPERSEDED 2026-07-31 — historical record only, do not build)

**Rollout discipline**: PILOT on Tax Policy only first (master sequence step 4) — well-understood, clean implement/adjust/remove semantics already in place. Full validation matrix on the pilot before touching any other tab. Only then (step 5) roll out to the remaining seven, using the exact same uniform draft → introduce → vote → pass/fail pattern the pilot used, unchanged per tab.

## Part C — UI/graph restyling (MASTER SEQUENCE STEP 2) — DONE. See `COMPLETED.md`.

---

## Open Questions (live — add new entries here as they come up; do not resolve silently)


**Blocked questions live in `MISSING_PREREQUISITES.md`, not here.** This section is for questions that
are genuinely open and not yet escalated. Once a question is waiting on a named party it moves, so this
list stays short enough to actually read.

- **Real reporting lag for data releases** (Continuous Time Migration) — optional realism refinement, not
  required for a first pass. Nobody is blocked on it; it has simply never been prioritised.

- ✅ **RULED 2026-08-02 — the net-creditor bound: FIX THE CAUSE, keep a non-binding guard.** Route SWF
  returns through the fiscal reaction multiplier so the stabiliser can reach them, **and** retain a
  deliberately wide runaway guard (~−1000% of GDP) that no country approaches. **The −300% symmetric bound
  is retired.**

  Elias's reasoning, recorded so it is not reopened:
  1. **France at −298% against a −300% bound is not a risk, it is already pinning.** Everything downstream
     of C4 reads that number, so it is reading the bound rather than the model.
  2. **The deficit-term investigation is the very next work and it reads this value.** Investigating a
     clamped signal wastes the investigation — exactly how the debt floor hid the deficit term until it
     came off.
  3. **A guard is not a bound.** Its job is to stop an unbounded runaway during and after the fix, never
     to shape a live value. **If any country reaches it, that is a bug report, not a clamp.**
  4. The Norway calibration this touches **has anchors**, so the cause-fix's risk is measurable where the
     bound's risk is hidden.

  ⚠ **Report the mechanism before proposing the implementation** — same shape as the debt-floor
  investigation, and the same reason: three wrong theories preceded the right one on the batch-run hang.

- 🔴 **NEW 2026-08-02 — the rating thrash's real cause is the DEFICIT term, and it is a separate defect.**
  Removing the floor cut debt-swing anomalies 60% (6,225 → 2,507) and moved rating anomalies by 1.6%
  (1,416 → 1,394). Two independent measurements agree the debt stock is no longer the driver. **Step C4's
  closure now waits on this instead.** Full evidence in `CLAUDE.md`.

### RESOLVED 2026-08-02 — all three section A decisions

Full reasoning in `MISSING_PREREQUISITES.md` section A, kept there deliberately so none is reopened later.

- **C4's rating thrash — fixed by REVIEW CADENCE, not damping.** The cap-and-average recommendation was
  **rejected**: it shrinks the thrash without removing its cause, and lands on the exact term the 5-anchor
  calibration runs through. Instead the rating updates on a scheduled annual review reading a *settled*
  fiscal position. Agencies review on a cycle rather than re-rating continuously; the release-calendar
  machinery already models exactly this shape; and Fed rate decisions are existing precedent for
  scheduled-not-continuous. **Implemented (`a4155ca`), and it produced a finding.** The 5-anchor
  calibration still passes 5 of 5, and matrix anomalies fell 3,421 to 1,416 — but they are **not gone**,
  which was the bar. The residual traces to the pre-existing **debt-to-zero bimodality**, not to the
  rating: the settled annual deficit ranges −135.5% to +170.8% of GDP because the debt stock itself
  oscillates 0% to 45% and back within a year, in exactly the documented Sweden/France/Germany set.
  **Step C4's implementation is complete; its closure now waits on that upstream defect** — see
  `MISSING_PREREQUISITES.md` section F.
- **SWF emergency drawdown — standalone tier-3 bill**, reusing 5d's mechanism. Not bundled into the
  annual budget, not fully exempt like the Fed/Eurozone carve-out. **Still to build — see below.**
- **Cabinet appointments stay unilateral.** Parliament gates *policy*; appointments are *executive*, and
  reshuffling already carries an approval cost. **No code change — this confirms current behaviour.**

*Five 2026-08-01 resolutions and two 2026-07-31 ones moved to `COMPLETED.md` section 11.*

### Live work from the 2026-08-02 visual review

- **P2 — the currency unit bug (review item 3). ✅ BUILT 2026-08-02 (`628d78e`), NOT YET SEEN.**
  `UiFormat.Money(value, MoneyUnit)` renders `$29.0T`; the unit is a **required** parameter on every
  graph and pie-chart entry point, so a currency display that does not state its unit no longer
  compiles. `MoneyFormatDiagnostic` passes 6 of 6 in real Unity against the seed figures. Full record,
  including four findings — two of them defects in the fix that only the diagnostic caught — in
  `CLAUDE.md`. **It stays live here because built-but-unconfirmed is not done:** item 3 asks whether a
  one-point published graph reads as working or broken, and that is a judgment about the screen, not
  about the arithmetic. 🟢 **Items 7 and 8 are unblocked** — they are now reviewable, not passing.

- **P4 — the label-clipping class (review items 5 and 6).** Investigated rather than patched, because
  it is a recurrence that survived earlier site-specific fixes. Full findings in `CLAUDE.md`. Seventh recurrence. **Item 6 is literally the
  "9,3" bug again, one field away in the same method**: `StatTile`'s label style is
  `new GUIStyle(GUI.skin.label)`, inheriting `wordWrap = true`, drawn into a fixed `12f * scale` rect —
  so a long label wraps to two lines and both get clipped. The **value** field in that same widget has
  carried the fix (wordWrap off, shrink-to-fit) since the "9,3" incident and its comment names the exact
  cause; the label never got it. Item 5 ("trade is cut off") is the width variant — five `ExpandWidth`
  buttons in a row with no width budget. **Recommended: one `PoliSimWidgets.MeasuredLabel` helper** that
  measures in the style text actually renders in, shrinks rather than truncates, recomputes per frame,
  and leaves margin — then sweep the seven known sites. Six site-specific fixes have not ended this class.

### Live, unblocked work carried out of section A

- ✅ **SWF emergency drawdown fast-track (A2) — DONE 2026-08-02 (`b1c077f`).** `SwfDrawdownBill`, the
  fifth tier-3 bill alongside Labor/CrimeJustice/Sector/Trade, reusing 5d's mechanism wholesale. Wired
  into the day advance, the Sovereign Wealth Fund tab and the pending-bills list. Full matrix anomaly
  counts identical to the run before it, which is the expected result: no batch scenario introduces this
  bill, so identical counts prove it inert until used. **Needs a visual look**, like anything new.

- ~~**`menu_pattern_tile.png` — delivered, never imported.**~~ **DONE 2026-08-02.** Imported, wired into
  `DrawCountrySelector`, zip archived. **The project root now holds no zips at all**, which is the first
  time that has been true — and is itself the standing signal, per working-discipline rule 12: a zip at
  the root means something in it is unfinished. Details in `COMPLETED.md`.

---

## When Elias returns to this document

- Check the Master Sequence section — confirm which step is actually in progress or next, don't assume.
- Check Open Questions first.
- Review the commit log — each step should be its own commit(s), validation results in the message or CLAUDE.md.


---

## Document set and the consolidation rule

**Established 2026-08-02, in the first consolidation pass. This is the standing pattern — run it whenever
the live documents start describing finished work.**

Eight documents, each with one job. If a fact belongs in two of them, it belongs in the one further down.

| Document | Holds | Grows or shrinks |
|---|---|---|
| `POLISIM_MASTER_ROADMAP.md` | **Live work only** — startable today | Shrinks |
| ~~`VISUAL_REVIEW_BACKLOG.md`~~ | Built but never seen. **DELETED 2026-08-02** — all eleven items confirmed, so it shrank to nothing and went, exactly as the rule below prescribes | — |
| `MISSING_PREREQUISITES.md` | Blocked work, by supplier. Not startable, so not live | Shrinks as blockers clear |
| `CLAUDE_DESIGN_ASSET_REQUEST.md` | The single standing asset request | Appended to, then emptied on delivery |
| `POLISIM_MACRO_OVERHAUL_DIRECTIVE.md` | Step 9's spec. Done steps become pointers; live specs stay | Shrinks |
| `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` | Real-world figures with `[VERIFIED]`/`[PARTIAL]`/`[GAP]` markers | Reference; stable |
| `COMPLETED.md` | Finished work + lasting decisions and lessons | Grows |
| `CLAUDE.md` | The detailed technical record. **Never superseded** | Grows |

### The three-way test every task gets

1. **Finished?** → `COMPLETED.md`, then delete from source.
2. **Waiting on a named party?** → `MISSING_PREREQUISITES.md`, then delete from source.
3. **Neither?** → it stays live.

**"Built but unconfirmed" and "built but uncalled" are case 3, not case 1.** They are the two states this
project has repeatedly mistaken for done, and both were found again in this pass.

### Rules learned from doing it the first time

- **Verify against the repo and the commit history, not against a summary.** The pass found Step A marked
  *"DONE, commit `e3a0feb`"* with Tier 0 derived stats folded in — but that commit contains two files and
  neither is `DerivedStats.cs`, which arrived at `70798e9` carrying "NOT trajectory-validated" in its own
  message. A summary is exactly where that error hides.
- **Check callers before believing a feature exists.** A4 validates cleanly and displays nothing; all four
  new files from 2026-08-01 had zero callers when checked. `grep` for the call sites, do not assume the
  wiring landed with the code.
- **If removing finished items empties a document, delete it.** An empty shell drifts back into use.
  `ELIAS_ACTION_LIST.md` was deleted for exactly this reason — every section had migrated.
- **Do not duplicate a live list into the blocked register.** While the visual reviews were open,
  `MISSING_PREREQUISITES.md` named what they blocked and pointed at the backlog rather than restating the
  11 items. Two copies of one list is the drift this pass exists to undo. *(Both documents have since
  emptied that section — the pattern held right through to deletion.)*
- **Repoint references before deleting a file**, and grep afterwards to prove nothing dangles.
