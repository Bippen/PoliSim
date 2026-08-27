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
   - **A legitimately time-blocking decision with no globally-visible indicator** — Fed Chair term appointment, a Cabinet decision, and a Foreign Policy meeting all correctly pause `GameController.Update`'s day-loop (every gate is checked correctly - this is NOT a simulation bug), but each one's actual resolution UI (the Fed Chair candidate picker, `DrawCabinetDecisionModal`, `DrawForeignPolicyMeetingModal`) renders ONLY inside its own specific tab's draw call - never globally. A player on any other tab (e.g. Tax Policy) when one of these fires sees simulated days silently stop advancing with no visible cause - indistinguishable from a hang. Before the fix, `DrawCalendarAndSpeedControls`'s always-visible status line (the one piece of UI pinned outside the scroll view on every tab) named the reason for Fed Chair and Cabinet only, in a modest, easy-to-miss label style, and said NOTHING for a pending Foreign Policy meeting - the one of the three statistically most likely to fire early in a fresh session, since it rolls per DAY (~1% chance) rather than per TURN (121-day then, 365-day since `d8f55ce`) like the other two. Fixed by escalating that line to the same bold/orange `_eventBannerStyle` used for the dashboard's own BREAKING banner whenever ANY of the three is pending, always naming which one and which tab resolves it - still exactly one Label control either way, per the stable-control-layout pattern above. This is a genuine UX gap, not a code crash: every future interrupt/decision system (gated legislation on the remaining seven tabs very much included) needs its "something needs your attention" state represented somewhere visible from every tab, not only on the tab where it originated.

   Assume a new mechanic is guilty of all six until the full-horizon batch run (for the first four) and direct live-Editor confirmation (for the last two, which batch runs cannot exercise) prove otherwise.
3. **Commit per unit of work.** One feature, one commit, descriptive message. Confirm staged contents match the message before committing.
4. **⚠ REPLACED 2026-08-11 at Elias's direction — ESCALATE TO ELIAS IN THE REPORT, NOT TO A DOCUMENT.**
   Recorded explicitly per rule 10's own requirement that a reversal never look like drift. **Previous
   wording (2026-08-02):** *"make the call, state the reasoning in the commit message, and flag it for
   Elias to overrule; escalate to Open Questions only when undoing it would be expensive or
   irreversible."*

   **Why it changed: Open Questions became a queue nobody drains.** An escalated question sat there
   unruled until work reached it months later and halted — so the escalation deferred the interruption
   instead of preventing it, and did so to the least convenient moment. Deciding-it-yourself still
   applies to everything reversible; what changed is where the genuine forks go.

   **The new standard:**
   - Every report ends with a **`RULINGS NEEDED`** block. One entry per question: the question stated so
     it can be answered **yes/no or A/B** wherever possible; the recommendation with one or two lines of
     reasoning; and **what it blocks**, or *"nothing, decide when convenient."*
   - **If a question blocks the pass in progress, STOP THERE and report it** rather than carrying it to
     the end. *A blocked pass reported at minute 3 is worth more than a finished pass that guessed.*
   - **"I can't call this one" is a legitimate entry.** A genuine coin-flip presented as a recommendation
     is worse than an admitted one.

   ⚠ **ONCE RULED, WRITE IT DOWN in whichever document owns the decision** — request doc, roadmap, or
   `CLAUDE.md`. **A ruling given in chat and not recorded did not happen**: same class as *"a delivery is
   not self-announcing"*, and the same failure mode, since the next session reads documents rather than
   transcripts.

   **Open Questions stops being a queue and becomes a record of decisions made.**
5. **Ground new mechanics in real data.** Label anything stylized honestly — never let a placeholder look like real data.
6. **Scope every new system small on the first pass.** Plumbing plus a few clearly-justified effects, not full theoretical richness.
7. **Update CLAUDE.md after every item**, including validation results, so history stays traceable.
8. **Verify Unity processes actually exited** (`Get-Process Unity*,UnityPackageManager`) before trusting that a closed window means it's safe to run a batch validation — confirmed to cause false failures more than once.
9. **⚠ SPLIT 2026-08-11 at Elias's direction — INSTITUTIONS MAY BE REAL, PEOPLE NEVER ARE.** Recorded here per rule 10's own requirement that *"any FUTURE reversal of a standing rule must be recorded the same explicit way"*, and recorded late: `main` carried four real party marks (`mark_party_us_gop`, `mark_party_us_dem`, `mark_party_se_s`, `mark_party_se_v` — imported, guarded and documented) for several hours while this rule still forbade them outright. The reversal was written down only on `stranded/politics-elections`, so `main` held the consequence without the rule. **That is the cached-status failure of rule 12 applied to a rule rather than to an asset.**

    - **PARTIES AND INSTITUTIONS — REVERSED, may be real.** Real party names, real vote shares, real seat counts, real thresholds, real chamber sizes and real electoral formulas. The Riksdag holds Socialdemokraterna and Sverigedemokraterna, not Progressive Alliance.
    - **PEOPLE — UNCHANGED, and this half is not negotiable.** Cabinet ministers, party leaders, legislators, Fed Chairs and heads of state remain **original and fictional**. The Fed Chair rule stands exactly as written.
    - **The distinction, stated so it survives paraphrase: a real party is an INSTITUTION; a real politician is a PERSON.** Only the first is reversed.

    **The cost this buys, stated so nobody rediscovers it:** real party data goes stale. Sweden votes 13 September 2026 and Italy's replacement electoral law is before the Senato that month. **Seed data is now a cached value with an expiry** — rule 12's shape — so every seeded figure carries its retrieval date.

9a. **NEW 2026-08-11 — TRADEMARK: A PARTY MARK IS ORIGINAL ART, NEVER THE PARTY'S OWN MARK.** Real party *names* are text and are used. Real party *logos* are marks owned by organisations, and reproducing one in a commercial game on Steam is a different proposition entirely. **Every `mark_party_*` sprite is an original abstract drawing** — recognisable by silhouette and by the party's real colour, owned by us, and defensible.

    **This is already load-bearing rather than theoretical.** The delivered pack gave Socialdemokraterna a **banner** rather than a rose *specifically because the rose is the subject of their registered mark*, and the Democrats a **torch** rather than the donkey. That reasoning was recorded once, in a delivery note, and must be stated in **every future party-mark request** rather than re-derived by whoever writes the next one. ⚠ It applies directly to §1G's outstanding `mark_party_us_lib`, whose party's associated imagery carries the same question.
10. **REVERSED (2026-07-31), was a hard rule through Master Sequence step 5d**: visuals are now a MIXED procedural/sprite model, not "all procedural." Elias has explicitly approved imported sprite art for **icons, portraits, and background/menu textures specifically** — see `CLAUDE_DESIGN_ASSET_REQUEST.md` (the single standing asset request; the original 5E/chrome/macro requests were consolidated into it 2026-08-02, all delivered) for the asset work this decision unblocked. **Stays procedural, unchanged, no exception**: all UI chrome/layout (`PoliSimTheme.cs`'s `RoundedBox`/`RoundedCard`/`Pill`/`Rule`/`TopAccent`/`LeftSpine` — pure `GUI.DrawTexture` rounded-rect/line geometry, no art asset, no reason to change) and every existing DATA visualization (`GraphRenderer`, `MapRenderer`, `PolicyWebRenderer`, `PoliticalCompassRenderer`, `HemicycleRenderer`) — none of these draw a "picture," they render real tracked simulation data, which is exactly what rule 5 ("ground new mechanics in real data") already protects; nothing about the icon/portrait decision touches that. **Becomes sprite-based**: one icon per `UiPalette.SystemArea` (policy area), one portrait per Cabinet minister candidate, one emblem per `PartyArchetype`, and background/menu textures — all sourced from Claude Design with the same origin-verification and security-review discipline already established for the first pack (Zone.Identifier mark-of-the-web check, full code/asset read-through before treating anything as trusted). This is a real, deliberate policy reversal, documented as such per this same working-discipline section's own precedent for recording a caveat/correction honestly rather than letting it look like silent drift - any FUTURE reversal of a standing rule must be recorded the same explicit way.
11. **Any new mechanic that nudges an existing tracked variable must fold into that variable's existing combined ceiling**, not add an uncounted new source — audit the actual ceiling code before adding a contributor, don't assume there's room.
12. **NEW (2026-08-02) — "awaiting delivery" is a status that must be RE-DERIVED FROM THE FILESYSTEM, never trusted from a document.** Two separate assets were recorded as outstanding while already sitting in zips at the project root: `icon_stat_interestrate` (registered *"REQUEST SENT, awaiting delivery"* on the day it in fact arrived) and `menu_pattern_tile.png` (delivered, then unimported for weeks while three documents named it as a gap). **Neither register was wrong when written.** Nothing watches the project root, a delivery does not announce itself, and so the status simply outlived the fact — twice, which is what makes it a pattern rather than an oversight. Both gaps were eventually closed only because Elias happened to say the file already existed. **Run `DeliveredAssetCheck` before reporting any asset as outstanding**: it compares every zip's contents against what exists under `Assets/` and fails on any gap, which is the one comparison that cannot go stale. Its companion `StatIconCoverageCheck` asks the runtime half of the same question — that a name the UI hard-codes actually resolves through `Resources.Load`, which a file merely existing on disk does not guarantee when its `.meta` is hand-written. The general form: **a status describing the outside world is a cached value, and needs an expiry.** ⚠ *Amended 2026-08-11: `StatIconCoverageCheck` covers the 19 names it ENUMERATES — every `StatNodeId` icon plus `menu_pattern_tile` — not "a name the UI hard-codes" generally. See rule 14.*

13. **NEW (2026-08-11) — TWO AGENTS IN ONE WORKING TREE NEED A LOCK, not a cleanup afterwards.** On 2026-08-11 two sessions wrote this repo concurrently with no coordination, and it produced three distinct failures, none of which either session could see from inside. **A merged contradiction:** one session recorded "§1E is closed" while the other recorded "I will not file these because §1E's namespace blocker is open" — read together as one agent reasoning badly, when it was two agents' claims merged without attribution. **A silent co-commit:** commit `452bf68` staged three files by explicit path and still carried ~150 lines of the other session's uncommitted §1F prose, because staging by path does not stop a path carrying another author's changes. **A stale lock read as litter:** a 2.2-hour-old `.git/index.lock` with no `git` process alive — correctly diagnosed as stale, but *"no process is running now"* and *"no session owns this"* are different propositions, and only the first is observable.

    **The rules.** Before any commit, run `git status` and confirm every staged path is one this session actually modified — `git show --stat HEAD` afterwards is the backstop, not the check. **Never `git add -A` / `git add .` in a tree that may be shared**; stage by explicit path, and inspect the diff of any path you did not create. **Never clear an `index.lock` without first confirming no live session owns it** — a dead process's litter and a live session mid-operation are indistinguishable from the lock's mtime alone, the same way a closed Unity window is not proof Unity exited. When a document's claims contradict each other, **suspect two authors before suspecting bad reasoning**, and check `git log`/`git blame` for authorship before writing a correction that may be arguing with a version that never existed.

    **Rule 13 at the filesystem level (2026-08-25, the two-copy consolidation — `COMPLETED.md` §31):** G: is
    the working copy; the C: copy is `PoliSim-ARCHIVE-DO-NOT-OPEN-2026-08-16` with `ProjectSettings.RETIRED`
    so it cannot be launched. **Standing habit: every harness or tool invocation passes the explicit project
    path** — the `/code-review` fall-back to `C:\Users\elias` was the instance that earned the rule.

14. **NEW (2026-08-11) — A CHECK IS EVIDENCE ONLY FOR CLAIMS ITS ENUMERATION CONTAINS.** `StatIconCoverageCheck` was cited in two documents as proof that four newly imported party marks resolved through `Resources.Load`. It enumerates `StatNodeId` plus `menu_pattern_tile` and never touches `Emblems/` — it passed **19 of 19** with the marks present, and would have passed with them absent or corrupt. `CLAUDE.md` had the scope stated correctly the whole time; `COMPLETED.md` said *"every name the UI hard-codes"*, and that looser phrasing is what licensed the misuse. **A passing check that cannot fail for the stated reason is worse than no check, because it retires the question.**

    The same defect recurred twice more in one session, which is what makes it a rule. A **57-file byte-identical diff** was read as proof that five import blockers were closed — but two of those blockers are about NAMING, which is not a byte-level property, so the diff structurally could not see them. And `PartyMarkCoverageCheck`'s own first version reported *"4 of 4 resolve at 128×128, the metas are sound"* when it only checked that a handle came back; extended to assert `texture.format`, it immediately found all four imported as **DXT5** — block compression on white-on-alpha, the exact damage vector the settings exist to prevent. **When citing a check, name what it enumerates.** When a check's bar is another artifact ("matches the reference"), it inherits that artifact's defects — the reference emblem was itself DXT5.

15. **NEW (2026-08-12, Elias) — COMPARE AGAINST THE PREVIOUS CAPTURE SET; DO NOT JUST LOOK AT THE NEW
    ONE.** `cbdde4e` shipped the selected sub-tab's label as cream on pale paper — unreadable — and its
    own capture run was **approved by eye with the defect on screen**. It was caught a day later
    (`4192042`) not by looking harder but by putting the pre-conversion `accessors_*` set beside the new
    one: readable white-on-brass next to unreadable cream-on-paper is a finding no single image
    produces. **The three verification layers each answer a different question, and only the third
    answers regression:**

    | Layer | Answers | Cannot answer |
    |---|---|---|
    | Guards (`UiOverflowGuard` / `UiContainmentGuard` / `ScreenEdgeCheck`) | containment — does content fit and stay inside? | composition — does it READ? |
    | A single capture set, by eye | plausibility — does this look like a working screen? | change — is anything worse than before? |
    | The set DIFF, old beside new | **did this change break something?** | — |

    ⚠ One practical limit, measured 2026-08-12: the capture warm-up is **unseeded**, so two sets differ
    in every simulated figure (AA+ vs AAA between consecutive runs) — the comparison is structural and
    by eye, never pixel-wise, until someone decides seeding the warm-up is worth it.

    ⚠ **PAIRED-DETECTOR CORRECTION (2026-08-24, the Calendar Panel pass).** The table above reads
    naturally as three passes over overlapping ground, which invites an assumption neither the table
    nor this rule ever actually claimed: that one layer's blind spot is the other layer's job. **It is
    not, and two findings from the same session sit cleanly on either side of the line.** A day-cell
    height defect (a real overflow, `_calendarDayNumberStyle` sized against a flat guess instead of its
    own metric) was caught by `UiOverflowGuard` **alone** — 2,004 violations, found straight from the
    guard's own count before any image was opened, no eye involved at all. A ledger date-column that
    **wrapped** rather than clipped at 2560px (`"10"` over `"/1"`) was caught by eye **alone** — none of
    the three guards reports it, because a string that wraps instead of overflowing satisfies
    containment, fit, and edge-flushness simultaneously; wrapping-instead-of-clipping is not a
    question any of them asks. **Neither layer backstops the other's blind spot.** A guard's blind
    spot is not safety-netted by looking (the guard-only case above needed no eye at all to be found,
    but nothing guarantees a DIFFERENT guard-blind defect would be visually obvious the way this one's
    count was); an eye's blind spot is not safety-netted by a guard built to answer a narrower question
    than "does this read well" (wrapping is exactly the shape rule 15's original finding — the sub-tab
    ink of `cbdde4e`, fixed `4192042` — also was: a composition question, not a containment one). Read the table's three rows as three
    INDEPENDENT questions with three independent gaps, not three redundant passes that jointly cover
    the ground — the reference-class trap's own lesson (2026-08-11: "adjacency is not sameness")
    applied to verification layers instead of to a lookup.

    **RETENTION (added 2026-08-16, the repository-weight pass).** "Keep old sets to compare against"
    is why captures reached 5,316 PNGs / 5.1 GiB in five days, 2,003 of them committed (~874 MiB of
    git blobs — see CLAUDE.md "The repository weight finding"). The comparison this rule actually
    needs is **one good baseline per axis, plus the run under judgment**:
    - **Keep**: the current baseline set per axis — the main sweep per size, the per-country coverage
      sets, the state-pin sets — and the most recent run per size. A superseded baseline is kept only
      until its successor is confirmed, then it becomes prunable.
    - **Prunable**: every older iteration set, the moment its finding is recorded in `CLAUDE.md`. The
      set is EVIDENCE while the finding is open and a cache once it is written down — rule 12's shape
      applied to pixels.
    - **Mechanics**: captures live OUTSIDE the tree at `../PoliSim-captures/` (driver, capture entry
      point and `ScreenEdgeCheck` all read one shared default, `-shotdir=` still overrides;
      `/screenshots/` is gitignored defensively). Nothing under the capture dir is ever committed.
    - Applying this policy today keeps roughly 1,900 files (~1.9 GiB) and marks ~3.2 GiB prunable —
      ✅ **APPROVED 2026-08-16 (Elias) — and EXECUTED the same day** (the prune ran alongside the
      history rewrite; the execution annotated here 2026-08-26, one word late per its own rule). The history rewrite was ruled YES
      the same day and ✅ **EXECUTED later that day as its own gated pass** — pack 742.03 → 4.92
      MiB, 76 citations swept, fresh clone at 4.89 MiB with all six checks green. Full record in
      CLAUDE.md "The history rewrite — executed 2026-08-16"; backup + commit-map at
      ~~`C:\Users\elias\PoliSim-backup-2026-08-16`~~ **`C:\Users\elias\PoliSim-ARCHIVE-DO-NOT-OPEN-2026-08-16`
      (renamed 2026-08-25 under rule 13, `ProjectSettings` → `ProjectSettings.RETIRED` so it cannot be
      launched; `.git` untouched — the pin and the commit-map are why it is kept).**

---

## Where things stand — re-derived 2026-08-27 (the third consolidation pass)

**This document holds only live work.** Everything finished is in `COMPLETED.md`; everything waiting on a
named party is in `MISSING_PREREQUISITES.md`; the split is the standing pattern at the bottom of this
file. **A task is live here only if someone could start it today.** Built-but-unconfirmed and
built-but-uncalled are neither finished nor live — they wait on Elias's eyes and sit in
`MISSING_PREREQUISITES.md` §V.

| Document | Holds |
|---|---|
| `COMPLETED.md` | Finished work and lasting decisions. This file shrinks into it, never grows |
| `MISSING_PREREQUISITES.md` | Work waiting on a named party — Elias's send, decision, eyes or playtest; Design's delivery; item 10; a raster path |
| `CLAUDE.md` | The detailed technical record for both. **Never superseded** |

**The board, stated once (verified at HEAD `d29406f`, 2026-08-27; re-derive it, do not edit it
forward):**

- **DONE** — Master Sequence I (items 1–9) and Master Sequence II Steps 1, 2, 3 and 5; Round 4; the
  fiscal-engine arc; the law system at 100 of 100 in two categories; the ruled build order (five passes)
  and the shelf's first item, tariff costs (pass 6). Records: `COMPLETED.md` §§27–31.
- **WAITING, NOT LIVE** — `MISSING_PREREQUISITES.md`: **§S** the send package (the request SENT
  2026-08-27, hash-verified, and ANSWERED the same day — the portraits delivered and imported, §D1
  closed; boards 1k/1l are live items 8–9 below; the courtesy note alone waits on Elias's send); **§A**
  the ruling queue Q6–Q10 (Elias, at named triggers); **§B** three seed quality debts (a database
  session); **§D** item 10 and everything riding its gate — 13 Sept 2026, Sweden votes — including
  election night (1h), Step 6, Riksbank-B, the stranded branch and the party marks; **§D1** the eight
  outstanding cabinet portraits — ✅ delivered 2026-08-27, `PortraitCoverageCheck` 25/25 (the roster
  look is §V's); **§E2/§E4** the mark accounting and the icon promotion (the raster diff, E3, is NOT
  Design's — it is live item 7 below); **§V** the
  built-but-unconfirmed surfaces (Elias's eyes); **§P** the three felt verdicts (a playtest).
- **LIVE** — the list below, in order.

---

## Live work — startable today

### 1. Scheduled next: the causal-graph screen (trigger FIRED 2026-08-25 — over-fired)

The original trigger was *"the ledger carries a second stat's terms"*; the ledger now carries THREE
(Approval, Consumer Confidence, Debt — Step 2 v1 and its third section, `COMPLETED.md` §30), and the
term IDs ARE the derived stat → stat edge list. Queued per the fiscal-chain precedent: **derived, never
authored** — which is also the structural fix for the Policy Web's declared edge list, whose signs
drifted once already (the 2026-08-27 edge sweep, CLAUDE.md). Startable by Claude with no external
input; the surface (a screen, a panel section, or the web itself reading the ledger) is the first ruling
the pass takes under rule 4. **The Policy Web gaps below are sequenced behind it.**

### 2. Content backlog — the two remaining scenarios (ruled 2026-08-26: keep, build when elected)

Specs migrated from the Step 3 package before its deletion (the format, the evaluator and the first two
scenarios are shipped — `COMPLETED.md` §30; `ScenarioLibrary.cs` holds exactly two entries today):

- **Poland convergence.** *Deltas:* the seeded 3.0%/turn potential, 59% debt. *Objectives:* sustained
  real-wage growth with inflation in band N consecutive turns. *Fail:* inflation > 6% three turns
  running. **Why hard:** growth is the easy half — the tightness → wage → (Q2) sentiment → consumption
  loop plus the Phillips curve means a convergence boom overheats itself, and the Taylor rule answers
  with rates. ⚠ Measure against `UnemploymentReversionSpeed` FIRST — it dropped two scenarios on one
  root cause (`COMPLETED.md` §§22/30).
- **The Unequal Recovery.** *Deltas:* elevated Gini, a hostile seat composition. *Objectives:* Gini back
  to baseline without losing a confidence vote. *Fail:* approval < 30. **Why hard:** every lever that
  closes the Gini gap (welfare, minimum wage, tax) runs through Parliament, and each failed bill charges
  approval — while the Q1 Gini term is itself pushing approval down until it closes. This is the
  scenario that proves Step 2's output is load-bearing.

### 3. The four remaining budget decompositions — Germany, France, Italy, Poland

Sweden's 24 sourced utgiftsområde lines shipped 2026-08-25 (ruled: decomposition now, Sweden first) and
the recalibration (build-order item 1, `290d4ee`) means the other four now decompose CORRECT totals.
Unscheduled, startable, one country per pass on Sweden's method (real budget documents, retrieval dates,
all-discretionary and not-byte-identical deviations stated with measured reasons — CLAUDE.md "Item 4
BUILT").

### 4. Chrome and UI residues — small, no gate, one pass could take them all

Each is a `POLISIM_V2_SCREEN_SPEC.md` clause the 2026-08-27 sweep found unbuilt or half-built, or a
capture-width item; none needs Design, a ruling, or a playtest first.

- **The status line's RUNNING state (§A.6, B8's second carrier):** only the HELD half is dressed
  (`DrawHoldBannerLabel`); the running branch is a bare label — the `#EDE2CB` plate on `1px #C9BA9B`
  with the `8px #3E8A5F` dot is unbuilt. The "Clock running" copy may be renamed in the same edit.
- **The speed buttons' held-state face (B5):** `DrawSpeedButton` keys on `selected` only; the
  disabled face (`#DDD2B8` / `1px #C9BA9B` / `#9A917D`, rendered never omitted) has no branch, and
  `ui_btn_disabled` has no loader anywhere in `Assets/Scripts` (the `UiPalette.BuildButtonStyle` comment
  that claimed one was corrected 2026-08-27). A third `ButtonKind` branch on one method.
- **The right-aligned screen caption (§A.8):** `DOMESTIC BULLETIN — DESK READINGS, LIVE` — B6's
  live/published carrier at screen level. Not built; nothing calls for it; §A.8a's "live desk reading"
  state is defined as sitting under it.
- **The inactive tab-swatch tints (§A.3, third column):** delivered as snapped values, never wired —
  the tab swatch draws the area ink today.
- **Three §A.2 ink tokens without a constant** (`ruleRow #D5C8AB` — the row-separator weight — among
  them; the sweep found two more; re-derive the list against `PoliSimTheme.cs` before building).
- **§A.11's urgency chip** (its `1.5px` border and `−2°` rotation — today a plain `DrawColoredLabel`)
  and **the generic stamp treatment**; **§A.13's two envelope rows** with no implementation (re-derive
  which two against `GameController`'s takeover seam before building).
- **International's two and the Fed's concatenated labels** — the row family's last residues; the
  Fed's count is stale since pass 4 rebuilt the central-bank tab (`513b348`) — re-measure, then fold
  into whatever next touches those screens.
- **The 2560×1440 Trade bill card wrap** (pass 6, Elias: cosmetic): the cost line wraps after the "+"
  of "+$0/yr" — `UiFormat.MoneyDelta`'s sign glyph read as a break opportunity — while the other three
  sizes wrap at spaces (`p6usa2560_06e_policylaws_trade` beside `p6usa1280_*`). Reorder the sentence
  so the money delta does not sit at a wrap point, or give the label the explicit measured width the
  free-aspect pass gave its siblings.
- **1920×1080 — the one uncovered capture size**, the most common desktop resolution: a command-line
  argument (`-shotwidth=1920 -shotheight=1080`), no code change.

### 5. Delivered art with no call site — place it or hold it knowingly (`COMPLETED.md` §33)

Re-derived 2026-08-27 from every sprite's call site; the counts are cached values with no expiry
(rule 12) — re-run the trace before acting on them.

- **25 of the 43 `Stats/` sprites** have no loader: 19 `icon_stat_*` for stats without a `StatNodeId`,
  plus `icon_trend_up/down/flat`, `badge_preliminary/revised` and `icon_release_marker`
  (`GraphRenderer` draws markers and `PublishedFigure` draws badges procedurally). Either widen the
  display surface or record the 25 as held stock in `IconLibrary.cs`'s doc.
- **8 of the 10 `icon_area_*` icons** are drawn and reachable but nothing asks for them (only fiscal and
  political, on the tab bar). Candidates: the sub-tab rows, the Policy Web wedge heads.
- **7 chrome names with no load call** — `ui_frame_double`, `ui_btn_disabled` (item 4), `ui_stamp_draft`,
  `ui_portrait_frame_oval`, `ui_btn_paper_canvas` (+`_hover`, `_pressed`); the 2026-08-12 "revivable by
  ruling" set plus the Canvas paper button the pilot never needed.
- **One coverage check that does not exist:** `AreaIconCoverageCheck` — no check enumerates area
  icons or emblems, so their coverage is asserted from the filesystem alone (rule 14).
  (`PortraitCoverageCheck` was built 2026-08-27 with the Progress5 import and runs in the suite:
  every `CandidatePool` minister, every Fed chair and the seeded sitting chair, through `IconLibrary`'s
  own accessors.)
- **The sitting turn-0 Fed chair** (Harriet Ellsworth, `WorldFactory.cs`, deliberately outside the
  candidate pool) has no portrait and no call site asks for one: decide the sitting-chair row's
  treatment. If it gains a portrait, that is one new asset and a fresh Design ask.

*(Two items left this list on 2026-08-27 by Elias's ruling: the eleven superseded SVG sources are
deleted, and the three dead widgets are deleted — `COMPLETED.md` §§29/33.)*

### 6. The label-clipping CLASS — open as a watch item (P4)

`PoliSimWidgets.MeasuredLabel` (measure in the rendering style, shrink never truncate) was implemented and
the known sites swept; the class has kept producing instances on NEW AXES since — #12 the frame itself,
#13 the ECB sub-tab through the COUNTRY axis, the 2026-08-26 width-less-label class (`CalcSize` ignores
`wordWrap` without an explicit width — six instances, fixed under the minimum-window ruling), the 2560
wrap above. The sibling survey (constant-sized chrome under wrappable labels) is named-not-fixed in
CLAUDE.md. **The class closes only by a capture-matrix pass at all supported sizes showing no new
instance**; rule 15's paired-detector correction is its standing discipline. Instance history:
`COMPLETED.md` §§17/32.

### 7. The rasterization diff — our half (moved here from the blocked register 2026-08-27, Elias's ruling)

Design asked (2026-08-11) that their strip-cut PNGs be diffed against our own rasterization once before
the pipeline is trusted; **Design's half closed 2026-08-17** (six per-state button PNGs re-rasterized
fresh from SVG, pixel-diffed 6/6 identical). Ours is a tooling pass, not a wait: `StripCutDiffCheck`
exists with the full tolerant-compare machinery, and a rasterizer exists on this machine (Unity's
built-in vectorgraphics module tessellates every `Chrome/Source/` SVG at import) — but the module's
`RenderSpriteToTexture2D` path yields a BLANK texture under the batch harness, probed and viewed rather
than inferred (never attributable to an SVG `<pattern>` parse limit — `ui_slider_track`'s features are
`linearGradient` + `currentColor`; corrected 2026-08-26). **Closes when either a render path in this
repo produces comparable pixels or an external rasterizer is installed here.** It had sat under "Waiting
on Claude Design" — a prerequisite attributed to the wrong supplier is one that lapses.

### 8. Board 1k — the calendar panel as ONE almanac sheet (answered 2026-08-27; NOT STARTED — Elias's call)

Design answered request §2 by drawing (`POLISIM_V2_SCREEN_SPEC.md` §A.16 carries the rulings): the
" X" suffix retires for a single diagonal ink stroke through the numeral (1.5px at 1600 / 2px at
2560, ink at 55%, ≈ −24°, inset 2px); the dots-vs-ledger split stands and the ledger row repeats the
grid's own 5px dot; the month flip stays instant; a saturated day (the 4-dot cap) gains a 2px ink
underline beneath the dot row; header, grid and ledger become one paper sheet separated by rules, one
scroll. No sprite — `RoundedCard`/`Rule`/`Pill` draw everything; measurements stay measurements. A
UI-only pass under rule 0's bar (compile + capture, the calendar panel at the four sizes, the
`capfold_83a` density case re-captured).

### 9. Board 1l — the graph-weight ruling R-G1…R-G5 (answered 2026-08-27; NOT STARTED — Elias's call)

Design answered request §3 as pixel rules, no art (`POLISIM_V2_SCREEN_SPEC.md` §A.16): history 3
buffer px (from 2), solid; projection stays 2 px, lighter, dash cadence re-cut to 3 on / 2 off;
threshold stays 1 px amber — a 3 / 2 / 1 weight order; sparklines `max(2, round(rectHeight / 34))`
device px; the 300×90 buffer may stand (if raised, restate the rule in device px); release-point
markers scale to weight + 2 px; the green/red deltas, the PRELIMINARY badge and the 1px revision
frame do NOT move. Lands as one constant and one cadence change in `BuildSparklinePixels` under the
existing 336-combination regression set; the eye's check is four stacked graphs at 2560 where history
plainly outranks the amber reference — the inverse of `couple2s2560_02a`.

---

## Queued at named triggers — not startable, and no named party owes anything

The roadmap's third category: real work whose trigger has not fired. Two of these share a trigger (a
capital stock), so if one ever ships, two fire together.

- **Per-scenario term accumulation** (the epilogue's named v1 upgrade) — trigger: the first scenario
  whose epilogue reads wrong without it.
- **Investment deepening (R-Q5e)** — return trigger: a capital stock ships, or I/GDP measures cyclical
  (both conditions recorded with the deferral, `COMPLETED.md` §22).
- **The identity's government-consumption block** (queued 2026-08-26 by pass 4's derivation — the
  honest form of its rejected branch A): the national-accounts identity's G is discretionary lines only
  (mandatory transfers excluded, correctly, but general-government consumption is nowhere), so every
  country's level output gap is a share-determined fixed point no seed can close (re-measured at HEAD
  after the recalibration, which turned the EU-five gaps from drifting series into stable levels — USA
  −14.5%, Poland −7, Italy −4.5, Germany −2.7, Sweden −0.8, France −0.5; `COMPLETED.md` §22's Q5 table
  carries the pre-recalibration figures; the map r* = [s + (1−s)·(G+NX)/Pot] / [(1+g) − (1−s)·a]
  reproduces the dumps to ~0.02 pp). Closing it means a government-consumption term in the identity and
  six re-solved potentials — a seventh-scale discontinuity across all six countries and Okun's anchor —
  which is why pass 4 fixed the RULE instead. Trigger: the first mechanic that needs the level output
  gap to mean something (a capital stock, an investment-deepening return, or a displayed "output gap"
  stat).
- **The un-voted "Reset to Default" click — a NAMED GAP (Elias, 2026-08-27), not a note.** It is the
  same shape as the free lever pass 6 priced: resetting a partner override is an immediate, structural
  on/off on the Trade tab (`GameController.DrawTradePartnerRow`, the `TaxLine.IsImplemented` idiom), so a
  player who has taken the take can cut back to the standing rate for free, un-voted, and the partner's
  mirrored tariff lifts at the next boundary — a live path around the pricing (a cut through the bill
  would read negative on the fiscal axis and pass at most compositions anyway, but it would be voted,
  delayed 21 days and visible in the division log). Closing it means either the reset riding the Trade
  bill like the rate does, or the mirror lingering a boundary after a reset (retaliation memory, which
  the model has no state for today). The harness's `PolicyDecision.PartnerTariffOverrides` path also
  bypasses the vote; that is the harness's privilege for every lever, not a player path. **Trigger: the
  first pass that touches the Trade bill's introduce/reset flow, and before item 10 opens the vote to
  real parties.**
- **Pass 6's deferred set** (reasons in CLAUDE.md "Pass 6 ships"): trade volumes indexed to GDP (moves
  NX on every baseline — its own force; when it lands the wedge must become `Δτ̄ × m` with an explicit
  rate anchor, recorded in code); retaliation against a base-dial hike (no excess to mirror — needs a
  seed-anchored base reference; the dial is voted; reach 0.49% of USA GDP); retaliation memory or lag (no
  diplomatic state exists); a trade axis for the vote (item 10's, where real parties land).
- **Policy Web gaps the edge sweep named (2026-08-27; CLAUDE.md "The Policy Web edge sweep" is the
  record of what it FIXED) — sequenced behind item 1 above.** Three real channels the web cannot yet draw
  honestly: `InterestRateDecision → DebtToGdp` (a direct interest-cost channel for five countries, but
  FALSE for the USA whose debt rate is anchored at `BaseDebtInterestRateOverride`; an edge is one truth
  for all six, so it needs a per-country edge set or the widget noting the exception); the two
  generic-line folds — `SpendingCategory.InfrastructureAndDevelopment` onto the Transportation node and
  `HealthcareAndSocialCare` onto HHSDiscretionary — so the five non-USA portfolios' lines draw the growth
  and confidence edges their USA twins draw. Indirect effects (the incarceration cost reaching debt
  through PrisonPopulation, MinimumWage → Approval through Gini, FamilyPolicy → DependencyRatio through
  BirthRate, TariffPolicy → Gdp through TradeBalance) stay undrawn by the web's own convention. The
  causal-graph screen derives edges from the ledger's term IDs rather than authoring them — the honest
  fix for a declared list that drifts.
- **Riksbank-B** is NOT on this shelf: it waits on item 10's appointment machinery, a named task —
  `MISSING_PREREQUISITES.md` §D.

---

## Standing constraints — rules that bind every pass, kept here because they are not tasks

- **Numbering is stable.** Master Sequence items 1–8 and Steps 1–6 are cited throughout CLAUDE.md and
  the code; never renumber. ⚠ TWO items carry "9": the macro overhaul (2026-08-01, cited as "step 9" in
  code comments, `COMPLETED.md` §§6/9/25) and the v2.0 overhaul (2026-08-03, `COMPLETED.md` §27).
  Disambiguate by name when citing.
- **THE CRITICAL CORRECTNESS RISK — published vs live.** The player-facing UI reads the PUBLISHED
  (lagged, possibly-revised) series; every internal system — Okun's Law, the Phillips Curve, the Fiscal
  Reaction Function, sector integration — keeps reading LIVE values. A leak makes the model consume its
  own stale output, and the effect may not appear for hundreds of turns. The one-directional rule
  (`PublicationSystem` writes `Country.Published`, reads `Country.State`, never the reverse) is the
  enforcement; the 55-call-site count is a 2026-08-01 snapshot.
- **`[GAP]` figures are Elias's to source, never to invent.** The seed doc's variant-axis rule
  (`POLISIM_SEED_DATA_MACRO_OVERHAUL.md` Part 4; `MISSING_PREREQUISITES.md` §B) governs any re-sourcing.
- **If a step's own validation fails, fix it before moving to the next** — never proceed past a failing
  step to "make progress" on the next one.
- **Calibration stays at turns 100–200; t1000 is a diagnostic, never a target** — judge a fix by whether
  the mechanism is present and correctly signed. **The word "equilibrium" stays banned without a run that
  earns it** (the unbounded-divergence block's two surviving rulings, `COMPLETED.md` §32).
- **SCREEN GRANULARITY, NEVER ELEMENT GRANULARITY.** IMGUI composites as one flat rectangle and no Canvas
  render mode draws above it; a screen is either Canvas or IMGUI, never both interleaved. **Any request
  that violates this silently is a request to migrate that screen wholesale to Canvas**, and should be
  recognised as such rather than hacked around (`COMPLETED.md` §27).
- **WHAT MUST NOT REGRESS — eight load-bearing behaviours**, each of which fixed a real defect,
  catalogued in CLAUDE.md. The appearance may change completely; the FUNCTION may not: the amber draft
  cue; direction-aware green/red (`GetDeltaColor`, keyed to *good*, not to *up*); the `MoneyUnit`
  formatter (a call site must not be able to render currency without naming a unit); `MeasuredLabel`'s
  shrink-never-truncate; stable control layout; the published/live distinction; per-area colour
  identity; the always-visible interrupt indicator.
- **Spec references:** anything row-shaped is `POLISIM_V2_SCREEN_SPEC.md` §A.9 with §A.9a (the resort
  ladder, numeric variant included), §A.9b (negative fill = no gauge) and §A.9c (Parliament's real
  pointer; §A.10 is Buttons). **Every number the spec supplies is suspect until derived or explicitly
  confirmed as fixed** (the spec's own banner); **declare deviations from the boards rather than
  diverging silently** — the V-series record is `COMPLETED.md` §24.
- **One row type per capture, never batched.** The tax row took three capture rounds and each found a
  defect code review had passed.
- **The guard scopes** (`UiOverflowGuard` — does text fit its rect; `UiContainmentGuard` — does a child
  rect sit inside its container; `ScreenEdgeCheck` — four pixel lines per PNG, right and bottom,
  flushness not magnitude, at the captured resolutions only) **and DO NOT BUILD A THIRD SITE-SPECIFIC
  GUARD** — a GUILayout-aware check needs IMGUI internals; the pixel check is cheaper, exists, and asks
  the question the player experiences. Any reflective guard must be justified against `COMPLETED.md`
  §32's paragraph.
- **`stranded/politics-elections` stays as-is until item 10 is scheduled, and its layout work is not
  extracted** without a failing measurement to justify it (rulings 2026-08-11; `COMPLETED.md` §32;
  the branch's contents inventoried in `MISSING_PREREQUISITES.md` §D).

---

## Open Questions — a record of decisions, not a queue (rule 4)

**No open question at HEAD (2026-08-27).** Every entry this section held was ruled, closed or migrated:
decisions live in `COMPLETED.md` §§11/23/32; questions waiting on a named party live in
`MISSING_PREREQUISITES.md` §A. A new question is written here only until it is ruled, and a ruling given
in chat and not recorded did not happen.

---

## When Elias returns to this document

- Read **The board** above, then the live list — both re-derived, never edited forward. If the board
  disagrees with `git log`, the log is right and the board is stale.
- `MISSING_PREREQUISITES.md` **§V and §P** hold what needs your eyes and your play; **§S** holds the
  send package.
- Review the commit log — each unit of work is its own commit, validation results in the message or
  CLAUDE.md.

---

## Document set and the consolidation rule

**Established 2026-08-02 in the first consolidation pass; run again 2026-08-26 and 2026-08-27. This is
the standing pattern — run it whenever the live documents start describing finished work.**

Ten files at the repo root, each with one job. If a fact belongs in two of them, it belongs in the one
further down the charter table; the four scoped documents below it are not a second home for anything.

| Document | Holds | Grows or shrinks |
|---|---|---|
| `POLISIM_MASTER_ROADMAP.md` | **Live work only** — startable today, plus the trigger shelf and the standing constraints | Shrinks |
| `MISSING_PREREQUISITES.md` | Blocked work, by supplier — including built-but-unconfirmed work waiting on Elias's eyes (§V), the home the deleted `VISUAL_REVIEW_BACKLOG.md` used to be | Shrinks as blockers clear |
| `CLAUDE_DESIGN_ASSET_REQUEST.md` | The single standing asset request, derived from the codebase | Appended to, then emptied on delivery |
| `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` | The Round-4 macro stats' real-world figures and the release schedules, with seven marker kinds (`[VERIFIED]`, `[ESTIMATED]`, `[GAP]`, `[PARTIAL]`, `[PROVISIONAL]`, `[BOUNDED]`, `[DERIVE]`) and the sourcing rules that govern any re-sourcing | Reference; stable |
| `COMPLETED.md` | Finished work + lasting decisions and lessons | Grows |
| `CLAUDE.md` | The detailed technical record. **Never superseded** | Grows |

**Scoped documents (not roadmap material, kept while they are load-bearing):**

| Document | Job | Retires when |
|---|---|---|
| `POLISIM_V2_SCREEN_SPEC.md` | The v2.0 visual conventions the code cites by section (`LedgerRow.cs`, `GameController.cs`), and the spec of the one unbuilt screen (1h) | never as a whole — a spec is a reference; its finished history moved to `COMPLETED.md` §24 |
| `LAW_BROWSER_BOARD_RULINGS.md` | Design's Screen 1i rulings, the build target two `GameController.cs` comments cite | the `board1jc*` eye review closes and the comments are repointed |
| `CLAUDE_DESIGN_BOARD_1I_NOTE.md` | An outbound courtesy note — attachment of the §S send package | the package is sent |
| ~~`POLISIM_R4_4_PREREPORT.md`~~ | The R4-4 ruling package — **consumed to `COMPLETED.md` §19 and deleted 2026-08-27** when D1's portraits landed, per §22's ruling | — |

Deleted under this rule: `VISUAL_REVIEW_BACKLOG.md` (2026-08-02), `POLISIM_MACRO_OVERHAUL_DIRECTIVE.md`
(2026-08-26), the three scoping packages and the derivation reports (2026-08-26, `COMPLETED.md` §§21/22).

### The three-way test every task gets

1. **Finished?** → `COMPLETED.md`, then delete from source.
2. **Waiting on a named party?** → `MISSING_PREREQUISITES.md`, then delete from source.
3. **Neither?** → it stays live.

**"Built but unconfirmed" and "built but uncalled" are case 3, not case 1** — or case 2 when the only
thing missing is Elias's eyes, which is a named party. They are the two states this project keeps
mistaking for done: both were found again in every pass so far, and the 2026-08-27 sweep downgraded 26
of 103 proposed DONEs on exactly this ground.

### Rules learned from doing it three times

- **Verify against the repo and the commit history, not against a summary.** The first pass found Step A
  marked *"DONE, commit `e3a0feb`"* with Tier 0 derived stats folded in — but that commit contains two
  files and neither is `DerivedStats.cs`, which arrived at `70798e9` carrying "NOT trajectory-validated"
  in its own message. A summary is exactly where that error hides.
- **Check callers before believing a feature exists.** A4 validated cleanly and displayed nothing; all
  four new files from 2026-08-01 had zero callers when checked. `grep` for the call sites, do not assume
  the wiring landed with the code.
- **If removing finished items empties a document, delete it.** An empty shell drifts back into use.
- **Do not duplicate a live list into the blocked register** — or the blocked register into this file.
  The 2026-08-27 pass found the register's five rows restated here, D1 restated three times, and item
  10's gate stated three times in one section. Two copies of one list is the drift this pass exists to
  undo.
- **Repoint references before deleting a file, and grep afterwards to prove nothing dangles** — source
  comments included. The 2026-08-26 pass missed five; the 2026-08-27 pass found four more pointing at
  files deleted on 2026-07-30 (`ROADMAP_BRIEF.md`) and sections deleted on 2026-08-26 (the request doc's
  §1F/§7).
- **A document can assert two states of one task at once.** "Still to build" and "DONE 2026-08-02" stood
  197 lines apart in this file for 25 days. When a live document is edited, search it for the task's
  other mentions before saving.
- **A capture is a harness film, not Elias's eyes.** "Pinned on film" and "verified both sizes" are
  containment evidence (rule 15's first layer); a strike-through that closes a visual item on that
  evidence alone is the conflation this rule exists to catch. The record of a sighting names the
  session.
