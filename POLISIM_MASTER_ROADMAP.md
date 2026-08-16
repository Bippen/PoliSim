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
      ✅ **APPROVED 2026-08-16 (Elias), execute at convenience.** The history rewrite was ruled YES
      the same day and ✅ **EXECUTED later that day as its own gated pass** — pack 742.03 → 4.92
      MiB, 76 citations swept, fresh clone at 4.89 MiB with all six checks green. Full record in
      CLAUDE.md "The history rewrite — executed 2026-08-16"; backup + commit-map at
      `C:\Users\elias\PoliSim-backup-2026-08-16`.

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
- 🔴 **FISCAL-ENGINE PRIORITY — the unbounded debt divergence, PARKED pending its own dedicated pass**
  (ruled 2026-08-11; diagnosis complete — interest compounding against an asymmetrically bounded
  stabiliser, see Open Questions). It supersedes C4's closure, which waits on it
  (`MISSING_PREREQUISITES.md` §F1, reconciled 2026-08-12). *(The former entry here — "highest-priority
  defect: the debt-to-zero bimodality" — was FIXED 2026-08-02; its full record migrated to
  `COMPLETED.md` in the 2026-08-12 reconciliation.)*
- **IN FLIGHT — v2.0 (item 9-v2.0):** the IMGUI half is COMPLETE through the chrome placement track
  (2026-08-12 — row family, six placements, Division Records, every chrome sprite dispositioned by
  Phase 2's derived statement). Coverage machinery standing: six countries × two sizes captured, the
  reachable state axes pinned, rule 15 in the discipline. ~~**Next major track: the Canvas path**~~
  **The Canvas track CLOSED 2026-08-12** (`e0a510f`; §A.14 set 2 of 3, ELECTION NIGHT item-10-gated
  per R2) and **the folder-tongue pass closed 2026-08-16** (`9497673`, startable row 3) — **item
  9-v2.0 now has NO ungated work left.** What remains of it waits on item 10.
- **NOT STARTED, UNBLOCKED OR GATED** — item 8 (save/load: scoped, zero persistence code exists —
  re-verified 2026-08-12, search `persistentDataPath|JsonConvert|CaptureSaveState` over
  `Assets/Scripts`, no hits); item 7 Phases 4–5 (deferred behind v2.0, now against 365-day turns);
  item 6 (Round 4, after 7/8); **item 10 (elections — gated: priced after Sweden votes 13 Sept 2026)**.

*(The 2026-08-02 "Built and now reachable" and trajectory-validation paragraphs that stood here are
historical validation records; superseded in place by the entries above, detail in `COMPLETED.md` and
`CLAUDE.md`.)*

---

## The debt-to-zero bimodality — FIXED 2026-08-02, record migrated

Full entry (mechanism, Elias's negative-debt ruling, the gate, the confirmation) moved to COMPLETED.md §18
in the 2026-08-12 reconciliation — finished work leaves the live file, per the three-way test. The
successor chain it named ends in the PARKED unbounded-divergence pass; see Open Questions.

---

## THE MASTER SEQUENCE — work this list top to bottom, do not skip ahead

### ⚠⚠ EXECUTION ORDER CHANGED AGAIN 2026-08-03 (Elias) — **v2.0 DISPLACES CONTINUOUS TIME**

**The numbering below is deliberately NOT changed** — items 1–8 are referenced throughout this file and
`CLAUDE.md`, and renumbering would break every reference. Only the order of work changes:

| Work order | Item | Why |
|---|---|---|
| **1st** | **9 — v2.0 UI overhaul** | Elias's decision, 2026-08-03. A total visual redirection touches every screen; anything built first gets rebuilt |
| **2nd** | **8 — save/load** | Already scoped and decided. Every crash or Editor restart still destroys all state |
| **3rd** | **7 — Continuous Time Phases 4–5** | Phases 1–3 are DONE. **Deferred indefinitely behind v2.0 — see the consequence below** |
| **4th** | **6 — Round 4** | Round 4 would add systems Phases 4–5 must then convert — **doing the work twice** |

⚠ **THE CONSEQUENCE, STATED PLAINLY: the game stays on turn-stepped macro for the whole of v2.0, and
that is not a temporary state with a known end date.** *(⚠ CORRECTED 2026-08-12: this block was written
when a turn was 121 days; since `d8f55ce` on 2026-08-10 — discontinuity 3 — **a turn is 365 days, one
year**. The hybrid description below survives with the number changed; every "121" in it reads "365".)*
Continuous Time Phases 4 and 5 are **deferred, not cancelled**. Until they land:

- **A turn is ~~121~~ 365 days and moves as one step.** Demographics (Phase 4) and the entire core macro engine —
  GDP identity, Okun's Law, Phillips Curve, interest-rate transmission, the fiscal reaction function's
  own transmission path (Phase 5) — all still resolve at the turn boundary, in one jump.
- **Nothing breaks, and specifically Phase 0's automatic tick keeps working.** The calendar advances a day
  at a time, `AdvanceDay` returns true on the boundary, the speed controls work, publications release on
  their real schedules, credit ratings review on theirs, and Phases 1–3's systems (sectors,
  infrastructure, labor market, crime & justice, poverty, and the whole money resolution) genuinely do
  move daily. **The daily layer is real and half the simulation already lives in it.**
- **So the honest description of the state we are shipping v2.0 on is a HYBRID SIMULATION**: a daily
  calendar with daily fiscal and social systems, and a turn-shaped macro core underneath. A player
  watching debt tick daily while GDP jumps every 365 days is seeing exactly that seam. **The v2.0 UI must
  not present the macro figures as if they were continuous** — the published/live distinction already
  carries most of this weight, and it should keep carrying it.
- **Phase 4's and Phase 5's handoff notes stay live and unmodified** below. They are not stale; they are
  waiting. The Phase 3 lesson (*a constant can be a DECISION rather than a FLOW*) is the most valuable
  thing in them and must survive the wait.

**Why v2.0 goes first, in Elias's framing:** a total visual redirection rewrites every draw method
regardless of framework, so anything built before it gets built twice. That is item 6's own
"don't-build-it-twice" argument applied one level further up — the same reasoning that put Phases 1–5
ahead of Round 4 now puts v2.0 ahead of both.

*Item 6 also remains gated on step 5 closing — that dependency is unchanged.*

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

   ✅ **MECHANISM REPORT WRITTEN 2026-08-16** — see CLAUDE.md "Save/load mechanism report". The state
   surface re-inventoried from the code (14 pending structures in the manager, ~30 controller drafts,
   the five serialization hazards named from real call sites — shared `CurrencyZone` identity first
   among them), the save location (`persistentDataPath`, outside the repo, atomic write), the version
   policy (additive changes free; item-10-class model swaps SAVE-BREAKING by declared `SaveVersion`
   bump, not migrated — item 10's collision map gains that line), and the batch-mode round-trip
   diagnostic that validates all of it without a live Editor. **The RNG layer was found already
   built and diagnostic-proven** (`SimulationRandom.CaptureDrawCounts`/`RestoreState`).
   Implementation is unblocked on this report's shape. ✅ **Version policy RULED A 2026-08-16
   (Elias): refuse-load with a plain message, `SaveVersion` bump on model swaps, no migration
   machinery pre-release** — built as ruled.

   ✅ **CORE BUILT AND GATE-GREEN, same day** — see CLAUDE.md "Save/load BUILT and gate-green" for
   the implementation record, including the one finding the gate caught (Json.NET's
   populate-in-place discard of the tuple-dict surrogate, isolated against the project's own DLL,
   fixed with a load-bearing `ObjectCreationHandling.Replace`). F5 saves / F9 loads as the
   temporary debug entry point; loads resume PAUSED; saves live in `persistentDataPath/saves/`.
   **Still open**: the load/save UI (next pass), and the layer-3 live checklist (OPEN VERIFICATION
   GAP block in CLAUDE.md — batch cannot reach OnGUI or the keyboard).

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
   - **Step B — graph overhaul + contextual policy-screen stats. ✅ DONE 2026-08-02** *(reconciled
     2026-08-12 — "both await visual confirmation" outlived the review's closure: items 3 and 10 were
     among the eleven Elias confirmed, `COMPLETED.md` §16)*. B1 `dd7e323`, B2 `5701a04`/`4869476`.
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

9. ⚠ *(NUMBERING COLLISION, recorded 2026-08-12 rather than renumbered: TWO items carry "9" — the
   macro overhaul above (2026-08-01, cited as "step 9" in code comments) and this v2.0 overhaul
   (2026-08-03, the "9" the execution-order table means). Renumbering would break references in both
   directions, the exact reason items 1–8 were never renumbered; disambiguate by name when citing.)*

   **NEW (2026-08-03) — v2.0 UI OVERHAUL. Elias's decision, and it is now FIRST in the work order.**
   A total visual redirection to the Suzerain (Torpor Games) idiom: a 1950s-republic aesthetic built as
   physical furniture — desk surfaces, paper documents, folders, ornate frames — with painted portraits,
   textured backgrounds and full-screen focus for consequential moments. The current UI is a dark-mode
   data dashboard. **This is a redirection, not a restyle**, and it displaces Continuous Time Phases 4–5
   (see the execution-order block above for the consequence, which is stated there rather than here
   because it changes what the SIMULATION is, not just what the UI looks like).

   ### ARCHITECTURE — DECIDED 2026-08-03, HYBRID AT SCREEN GRANULARITY

   Elias chose the hybrid after the architectural survey, on one specific finding: **the desk metaphor is
   NOT continuous.** A data screen is *"looking at a document"*, not a document sliding around on a shared
   desk. That is what makes the hybrid safe, and the reasoning is worth preserving because it is the whole
   justification:

   | | Renders in | Gets |
   |---|---|---|
   | **Narrative / consequential screens** | **Canvas** | Transitions, TextMeshPro, masks, effects |
   | **Data-dense screens** | **IMGUI, restyled** | 9-slice frames, textures, a real font — no rewrite |

   **Canvas**: country selector, election results, Fed chair selection, cabinet decisions, foreign policy
   meetings, bill votes, the budget signing moment, the pending-interrupt banner.
   **IMGUI**: Statistics, Budget line items, Policy/Laws tables, Demographics, and all seven renderers.

   ⚠ **SCREEN GRANULARITY, NEVER ELEMENT GRANULARITY. This is the rule the architecture rests on.**
   IMGUI composites as one flat rectangle, so **the IMGUI layer cannot be masked, animated, or PARTIALLY
   occluded by Canvas elements**. A screen is therefore either a Canvas screen or an IMGUI screen — never
   both with interleaved layering. **Any request that violates this silently is a request to migrate that
   screen wholesale to Canvas, and should be recognised as such rather than hacked around.**

   ⚠⚠ **THE RENDER ORDER IS NOT WHAT THE SURVEY ASSUMED. Measured 2026-08-03; full write-up in
   `CLAUDE.md`.** The survey proposed ScreenSpaceCamera Canvas *below* → IMGUI → ScreenSpaceOverlay Canvas
   *above*. The spike confirmed the first half and **disproved the second**:

   | Layer | Measured |
   |---|---|
   | ScreenSpaceCamera Canvas | renders BELOW IMGUI ✅ as assumed |
   | ScreenSpaceOverlay Canvas | **also renders BELOW IMGUI** ❌ — buried completely |

   **There is no Canvas render mode that draws above OnGUI.** IMGUI is always topmost. Two consequences,
   both load-bearing for how v2.0 gets built:

   1. **A Canvas screen is only visible when the IMGUI layer is SUPPRESSED** — `GameController.OnGUI` must
      early-return while a narrative screen is up. This is not a workaround; it is the screen-granularity
      rule enforced by the renderer itself, which is the best possible outcome for a rule that would
      otherwise rely on discipline.
   2. **The survey's proposed transition — "a Canvas overlay fades in over the IMGUI screen" — is
      impossible.** Transitions must instead be driven from the IMGUI side (an IMGUI full-screen scrim can
      fade over everything, because IMGUI is on top) and then handed to the Canvas screen, or played
      entirely inside the Canvas screen after the hand-off.

   ✅ **RESIDUAL RISK CLOSED 2026-08-03 — re-measured in a BUILT WINDOWS PLAYER, identical result.**
   `outer=RED, band=GREEN, centre=GREEN` at 1600×900, byte-for-byte the Editor's answer. The hybrid no
   longer rests on an Editor-only measurement, and the architecture can be locked.

   ### WHAT MUST NOT REGRESS

   Eight load-bearing behaviours, each of which fixed a real defect, catalogued in full in `CLAUDE.md`.
   The appearance may change completely; the FUNCTION may not. In brief: the amber draft cue; direction-
   aware green/red (`GetDeltaColor`, keyed to *good*, not to *up*); the `MoneyUnit` formatter (a call site
   must not be able to render currency without naming a unit); `MeasuredLabel`'s shrink-never-truncate;
   stable control layout; the published/live distinction; per-area colour identity; and the
   always-visible interrupt indicator.

   ### WHERE IT STANDS — end of 2026-08-03

   | | |
   |---|---|
   | Architecture | **Decided and measured.** Hybrid at screen granularity; render order confirmed in a built player |
   | Typography | **Done.** TeX Gyre Pagella + Courier Prime, open-licence, owned by `PoliSimTheme` |
   | Design brief | **Sent and answered in full.** Two chrome passes delivered; spec reproduced in the request doc §1C |
   | Sprites | ~~136 on disk, 52 chrome resolving~~ *(counts as of 2026-08-03; superseded by Phase 2's derived statement below — 61 chrome on disk, 29 wired, all dispositioned)* |
   | Wiring | **Systemic layer live** — palette, buttons, panels, tabs, stat plates, chips, sliders, scrollbars |

   ⚠ *Superseded 2026-08-10 — the wiring gate was lifted and the Budget restyle has begun. See below.*
   The wiring was built but not confirmed; Elias reviewed it live and directed the restyle forward.

   ### ⚠ NEXT SESSION STARTS HERE — re-derived 2026-08-11

   ⚠ **THIS MARKER IS DERIVED, NOT NARRATED. Re-derive it; do not edit it forward.** The state below
   comes from `grep LedgerRow.Draw` over `GameController` plus the commit history — not from what the
   previous marker said. **It had been wrong for a day**: it read "Budget is the only converted screen,
   next pick one of Statistics / Politics / Policy-Laws" while `d3cd281`, `df03e97` and `d4083fe` had
   already converted Policy/Laws, so it offered a finished screen as a candidate.
   **A next-steps marker is a claim like any other and goes stale the same way** — and this one is worse
   than most, because it is the first thing read each session, so a stale marker misdirects the pass
   before anything else runs. A pointer that outlived its target, rather than a claim that outlived its
   evidence.

   **Derived conversion state, RE-DERIVED 2026-08-12** — `LedgerRow` call sites mapped to their
   containing methods, plus a label-shape count on the screens that have none.
   ⚠ **SCOPE (2026-08-12): every "converted" and "captured" claim in this table is verified on the USA
   in the default capture state.** Five of six countries have never been photographed, and they reach
   code the USA does not (the legacy four-slider spending UI most of all):

   | Screen | Converted | Evidence |
   |---|---|---|
   | **Budget** | ✅ all five row types | `DrawTaxLineRow`, `DrawSpendingLineRow`, `DrawWelfareProgramRow`, `DrawInfrastructureContent`, `DrawSwfPolicyContent` |
   | **Policy/Laws** | ✅ via `DrawDialRow`, 20 sites | Labor Market, Crime & Justice, Trade — `d3cd281`, `df03e97`, `d4083fe` |
   | **Statistics / Domestic** | ✅ **NEW** | `DrawDerivedStatRow` — `397d829` |
   | **Politics / Parliament** | ✅ **NEW** | `HemicycleRenderer.Draw` legend — `f877915` |
   | Statistics / International | ⚠ 2 concatenated labels | small |
   | Politics / Federal Reserve | ⚠ 5 concatenated labels | small |
   | Decisions · Demographics · Policy Web | — | no row family; cards, dials and a diagram |

   ➡ **THE ROW FAMILY IS DONE.** Every screen carrying a ledger-shaped list is converted. What remains
   are **two small residues** — International's 2 and the Fed's 5 concatenated labels — which are
   leftovers of the same pattern rather than a screen conversion, and are worth folding into whatever
   next touches those screens rather than being a pass of their own.

   ⚠ **SO THE NEXT v2.0 QUESTION IS NOT "WHICH SCREEN" — IT IS WHICH OF THE REMAINING TRACKS.** §A's own
   backlog names three, none of which is a row conversion. **Track 1 CLOSED 2026-08-12** (see its table).

   ⚠⚠ **THE ONE ORDERED STARTABLE-TODAY LIST — reconciliation, 2026-08-12. This replaces every
   contradictory ordering; each item names its gate.** *(The coverage-before-features ruling below it
   is DISCHARGED: country coverage ran, the state axes are pinned, and item 1a closed the same day.)*

   | # | Startable today | Gate |
   |---|---|---|
   | 1 | ✅ **Canvas pilot — DONE 2026-08-12** (`14cbad6`+`257ed39`): the takeover seam (8 named defect classes, one found by the pilot's own first run), `CanvasChrome`, the selector per §A.14, `ui_scrim_takeover` wired. Discipline-carryover statement in `CLAUDE.md`. *(⚠ Count corrected same day: §A.14 defines THREE Canvas screens, not eight — the eight are the design boards.)* |
   | 1b | ✅ **Canvas screen 2 — SIGNING (1g), DONE 2026-08-12** (`5f64554`+`38363c6`): the nearest neighbour, on the pilot's patterns plus the mid-game additions (takeovers stop the clock; CoverIn overlays the live dashboard; ceremonies fire only from play's day tick). Verified both sizes, B8 on film. **The §A.14 Canvas set is now 2 of 3 — ELECTION NIGHT (1h) alone remains, R2-gated on item 10.** ✅ The Canvas TEXT GUARD was ruled and BUILT same day (`6adb7c6`, `CanvasTextGuard`): self-testing both directions, driver-attached after each Canvas capture, fails at zero enumerated; limits verbatim in the class doc. Same commit: `CanvasChrome.TintedImage`/`AsAuthoredImage`, the tint-family choice forced at construction after the class's fifth visit |
   | 2 | ✅ **Track 3 — DONE 2026-08-12** (`10f713e`): the eleven superseded sprites removed, `DeliveredAssetCheck`'s superseded allowance in the same commit (reads the manifest's own `!` rows, each skip logged). Verified: 0 missing / 21 supd skips, ChromeV2 50/50 both directions, full capture run clean post-deletion |
   | 3 | ✅ **Folder-tongue faces — DONE 2026-08-16** (`9497673`): `BuildFolderTabStyle` + the deferred active-tongue paint (§A.7's joined look — the sheet would otherwise close the tongue with its keyline); tongue-edge constants MEASURED from the PNGs' alpha, not the manifest's stated 12px; ink-on-paper labels both ways (the cream selected label would be the inversion class on paper). Verified 78 captures × both sizes, all guards 0, rule-15 diff against `winusa1600`/`grdusa2560`. ⚠ Hover face and the real click on the deferred-painted tab are not harness-drivable — awaiting Elias's live look | none — ruled B |
   | 4 | ✅ **WIN-form election reveal pin — DONE 2026-08-12** (`5eb5dc7`): both reveal forms + game over pinned in one chain; election search extracted to one helper. The FP-meeting search variant queued with it was found ALREADY BUILT (C2's continuation loop, capture 84b) — stated, not re-built |
   | — | *Item 8 save/load* — ✅ **CORE BUILT AND GATE-GREEN 2026-08-16**: all three layers implemented on the mechanism report, `SaveLoadRoundTripDiagnostic` 12/12 scenarios clean (six countries × two seeds, continuation-identical, saves string-equal). Remaining: the load/save UI (its own pass), and the UI-draft/F5-F9 live checklist in the OPEN VERIFICATION GAP block | UI pass startable; live checklist waits on Editor access |
   | — | *The fiscal-divergence pass* | PARKED by ruling — Elias schedules it |
   | — | *CT Phases 4–5, then Round 4* | 3rd/4th in the execution order; Phases 4–5 now calibrate against 365-day turns |
   | — | *Step C batches C1/C2/C3/C5* | buildable since 2026-08-02; ✅ **RULED 2026-08-12 (R1): folds into Round 4's slot** — both add new systems, same don't-build-twice logic |
   | — | *Item 10 — elections* | priced after Sweden votes **13 Sept 2026** |

   ✅ **THE THREE RECONCILIATION RULINGS, RECORDED 2026-08-12 (Elias):**
   - **R1** — Step C folds into Round 4's slot (table row above).
   - **R2** — the ElectionRecord waits for item 10's model; **the Canvas ELECTION NIGHT screen is
     item-10-gated, the other seven Canvas screens are not.**
   - **R3** — the item-10 collision map stands; **no main-side changes until item 10 opens.** One note
     added at Elias's direction: `PartyMarkCoverageCheck`'s reflection over `BuildParties()` *should*
     survive the model swap — **to be VERIFIED when item 10 opens, not trusted now.**

   **The discharged ruling, kept for the record — CAPTURE COVERAGE BEFORE NEW FEATURES (2026-08-12):**
   both 1a and the Canvas pilot were to wait on coverage. The work, as it ran:
   1. ✅ **Country coverage — RUN 2026-08-12, reported before fixing.** 12 runs (6 countries × 2
      sizes), 676 captures, every automated check clean on every set; the different-code screens then
      eyeballed per country (rule 15: the zeros answer containment, not composition). **Two defects
      and one stale premise, full report in `CLAUDE.md` "Country coverage":**
      - 🔴 **The ECB sub-tab label garbles when SELECTED at 1600-class sizes** (Germany+Italy
        confirmed, France same branch; clean inactive, clean at 1440p, clean for shorter names).
        Both guards structurally blind to it — a button label in the panel interior. **Fix not yet
        applied** — awaiting the ruling below.
      - 🔴 **The empty Mandatory spending group leaks `GroupSpendingMax`'s 0.0001f guard as a real
        figure** — "bars to $100k" over zero rows, all five non-USA countries. **Fix not yet applied.**
      - ⚠ The pass's own premise was stale: all six countries HAVE spending portfolios
        (`SeedGenericSpendingLines`, 5 lines each) and the legacy category-delta UI path is
        unreachable for every seeded country. The Overview line that said otherwise is corrected in
        `CLAUDE.md`; the conversion HOLDS on the 5-line sets.
   2. ✅ **The reachable state axes — PINNED 2026-08-12** (`-shotstates`; USA/Germany/Sweden sets, all
      edge-clean; full record in `CLAUDE.md` "The state axes pinned"): cabinet search + appointed
      roster, the cabinet-decision dossier (real rolls), the budget-process pause with the
      multi-reason hold banner, a pending bill of every introducible type (7/7 on Sweden — its seeded
      standing fund is the SwfDrawdownBill's real precondition), and an en-US locale set beside the
      sv-SE eleven. **Still unpinned, reasons named:** the foreign-policy dossier (search refinement,
      drivable), an election resolving (NO observable UI state exists — needs the model record below),
      game over (left for a deliberate pass).

      **⚠ TWO OF THOSE THREE "UNPINNED" CLAIMS FELL WITHIN HOURS, later the same day:** the FOREIGN
      POLICY dossier pinned itself through real rolls once the driver made the controller's daily
      arm calls, and **the election claim was the absence-claim class's sixth instance** —
      `DrawElectionResultsScreen` existed all along; the driver's sim-side turn path simply never
      ran `CheckElection`. The `div2_*` sets (USA + Germany × both sizes, all edge-clean) now pin:
      the FP dossier, six real divisions on the new Division Records panel, the election reveal
      (LOSS form), and game over. Still unpinned: the WIN reveal — one chain pins one form, stated
      rather than implied. The driver's day-tick is now a COPY of the controller's list (arm calls
      + eight countdowns + CheckElection) — the drift risk this creates is raised for ruling.
      *(Both closed same-day/next-day: the copy became `SimulationManager.AdvanceCountryDayTick`,
      one method both callers share — see `CLAUDE.md` "Item 1a closed" — and the WIN reveal pinned
      2026-08-12 (`5eb5dc7`, `winusa1600_88w`): approval forced high at the first election turn, a
      win's dismissal returns to the dashboard, so the same run then searches to the next election
      for the loss chain. Both reveal forms and game over are now on film.)*

      🔴 **NEW FINDING from the reveal's first-ever capture: `DrawElectionResultsScreen` prints its
      body in the paper-ink ramp on the BARE DESK** — "Approval Rating / needed to win / Margin"
      near-invisible; only the brick headline and brass button read. The ink-needs-paper inversion
      class on the one screen that never had paper. **Not fixed — awaiting the ruling.**

   2a. ⚠ **THE STAMPS RULING'S PREMISE WAS WRONG WHEN MADE — `DivisionLog` has existed since
      `a7bd40d` (2026-08-10), two days before the ruling declared no record existed.** Every bill
      type's resolution appends to `Country.Divisions` (last 24: title, date, alignment, passed,
      number); zero UI readers. **Item 1a is therefore mostly DONE on the simulation side** — what
      remains is the UI (a Recent Divisions block on Parliament, where `ui_stamp_carried`/`_rejected`
      then have their state to mark) plus whatever retention/enrichment the Canvas election night
      needs. The item below stands corrected by this entry.
   3. **Track 3 (superseded sprite removal) proceeds when convenient** — `DeliveredAssetCheck` gets the
      superseded allowance in the same commit as the deletion.
   4. **The folder-tongue faces (`ui_tab_folder_*`) as their own pass** — ruled B, nothing blocks on it.
   5. **1a (resolved-bill history)** and **the Canvas pilot** — Elias rules between them after coverage.

   The original three-track backlog, kept for the record:
   1. **The unwired chrome — ⚠ THE LIST OF 13 WAS WRONG, corrected 2026-08-12 by tracing call sites.**
      Six of the thirteen were never IMGUI placement work at all. **This is a correction to the list, not
      a deferral**, so nobody re-derives it:

      | Sprite | Actual state |
      |---|---|
      | `ui_subtab_on` / `ui_subtab_off` | ✅ **WIRED** `cbdde4e` — sub-tab faces |
      | `ui_slider_tick` | ✅ **WIRED** `cbdde4e` — ledger track scale, every 10% |
      | `ui_chip_outline` | ✅ **NEVER UNWIRED** — 2 live call sites in `GraphRenderer` and `PublishedFigure`. It was on the list in error |
      | `ui_stamp_carried` · `ui_stamp_rejected` | ✅ **WIRED** `ab1b72f` — the Division Records panel's verdict stamps, ink weights on paper. *(The "no state to mark" ruling's premise was false when made — see item 2a and the absence-claim guard in `CLAUDE.md`)* |
      | `ui_seal_official` · `ui_seal_state` | ⛔ **CANVAS-PATH** — no signing moment exists in IMGUI |
      | `ui_scrim_takeover` | ⛔ **CANVAS-PATH**, confirmed: no call site outside that track |
      | `ui_grain_tile` | ✅ **WIRED** `b4108a3` — desk grain, drawn first in OnGUI |
      | `ui_banner_hold` · `ui_calendar_pad` | ✅ **WIRED** `7933696` — the interrupt's dark desk plate (both B8 sites, amber lamp, HELD and RUNNING states pinned by capture) and the desk calendar, now the date's carrier |
      | `ui_tab_spine` | ✅ **WIRED** `a220849` — area-hue strip on every tab tongue, ink active / lifted inactive |
      | `ui_folder_dossier` · `ui_portrait_frame` | ✅ **WIRED** `fc16304` — the four Decisions dossier cards (shoulder caption, hue spine against the sprite's geometry) and the brass roster frame on every `DrawPersonPortrait` |

      ✅ **THE PLACEMENT TRACK IS CLOSED — 2026-08-12.** All six real IMGUI placements are wired, each
      verified in real Unity by capture (57–58 shots per run, 0 failed / 0 overflows / 0 escapes /
      0 clipped edges on every run — `holdcal_*`, `spineink_*`, `dosport_*` sets). `ui_portrait_frame_oval`
      is **Canvas-path** per the manifest's own "Canvas hero size" note, joining the seals and scrim.
      ⚠ **SCOPE (corrected 2026-08-12, Elias): verified on the USA, in the default capture state, at
      1600×929** — one country, one state, rule 14 at the largest scale it has appeared. The country
      coverage pass above is what widens this claim.
      One defect found and fixed en route (`4192042`): `cbdde4e`'s sub-tab face left the Primary kind's
      cream text on pale paper — the selected sub-tab label was unreadable in every capture, and the
      commit that introduced it had been approved by eye (now working-discipline rule 15).

      ✅ **RULED 2026-08-12 (Elias) — the sub-tab keeps its recorded no-area-tint decision.** §A.8 specs
      an active-sub-tab "bottom 3px area ink" strip and the manifest says "= ui_tab_spine flipped", but
      `DrawSubCategoryButton`'s recorded decision ("no per-area tinting at this second level") stands:
      the main-tab spine now carries area identity one level up, so the strip would be redundant, not
      missing. The "Clock running" status copy may be renamed when convenient — cosmetic, nothing waits.

      ### PHASE 2's DERIVED STATEMENT — every chrome sprite's disposition, FROM CALL SITES (2026-08-12)

      **This statement is what track 3 gates on** — four screenshots looking right is not the claim "the
      v2.0 chrome set is confirmed"; this is that claim, derived by exact-name call-site trace
      (`git grep '"<name>"'` over `Assets/Scripts`, load calls distinguished from comments) across all
      **61 sprites on disk**. 29 wired + 11 Canvas-path + 2 no-state + 8 orphaned + 11 superseded = 61.
      *(Amended 2026-08-12: the 11 superseded are now REMOVED (`10f713e`, Track 3) — **50 on disk**,
      and ChromeV2CoverageCheck reports 50/50 both directions. The manifest's `!` rows remain as
      `DeliveredAssetCheck`'s allowance source. The stamps' "no state to mark" row below also stands
      corrected — both wired `ab1b72f` per item 2a. The families below are otherwise unchanged as the
      dated record; a fresh call-site trace, not this note, is what re-derives them.)*

      **WIRED — 29, each with a live `IconLibrary.GetChrome` load:** `ui_panel_paper` (boxes + cards) ·
      `ui_btn_brass` / `ui_btn_paper` (every button kind) · `ui_plate_tile` (stat tiles) ·
      `ui_chip` / `ui_chip_outline` (badges, published figures) · 8 scrollbar pieces +
      `ui_scrollbar_button_none` · `ui_slider_track` / `ui_slider_knob` / `ui_slider_knob_disabled` ·
      `ui_slider_tick` · `ui_hatch_draft` · `icon_pencil_draft` · `ui_subtab_on` / `ui_subtab_off` ·
      `ui_grain_tile` · `ui_banner_hold` · `ui_calendar_pad` · `ui_tab_spine` · `ui_folder_dossier` ·
      `ui_portrait_frame`.

      **DELIBERATELY UNWIRED, reason recorded — 13:** the six `ui_btn_*_canvas` states, `ui_folder_country`,
      `ui_scrim_takeover`, `ui_seal_official`, `ui_seal_state`, `ui_portrait_frame_oval` (all Canvas-path,
      rulings of 2026-08-12 and the manifest's own notes); `ui_stamp_carried` / `ui_stamp_rejected`
      (no state to mark — blocked on item 1a, resolved-bill history).

      **ORPHANED — 8, present and specified with NO call site. ✅ ALL EIGHT RULED by Elias 2026-08-12:**

      | Orphan | Ruling |
      |---|---|
      | `ui_tab_folder_on` / `_off` / `_hover` (3) | **B — place the folder faces, as their OWN PASS.** The §A.7 tongue anatomy; the btn-face treatment is interim. Nothing blocks on it; it joins the startable list below |
      | `ui_frame_ornate` | **B — CANVAS-PATH.** The `drawOwnFrame: true` modals exist in IMGUI today, but dressing screens the Canvas path rebuilds is doing the work twice |
      | `ui_frame_double` | **Served-by-current-treatment, revivable by ruling** — no IMGUI home was ever named |
      | `ui_stamp_draft` | **Served-by-current-treatment, revivable** — D1 made `icon_pencil_draft` the inline draft carrier |
      | `ui_btn_disabled` | **Served-by-current-treatment, revivable** — `GUI.enabled` dimming over brass/paper satisfies B5's "rendered, never omitted" |
      | `ui_pixel` | **Served-by-current-treatment, revivable** — every use it was cut for is served procedurally |

      **SUPERSEDED — 11, `!`-marked in `ChromeManifest.txt`, and REMOVAL-SAFE from the code side:**
      `ui_button_normal/hover/pressed/disabled`, `ui_panel`, `ui_scrollbar_track/thumb`,
      `ui_scrollbar_track_vertical/thumb_vertical`, `ui_slider_fill/thumb`. Exact-name trace: **zero load
      calls** — the only textual hit is a doc-comment mention of `ui_button_normal` in `IconLibrary.cs`.
      `ChromeV2CoverageCheck` tolerates their removal by construction (`!` names are exempt from both
      directions). ⚠ **One execution note for track 3:** `DeliveredAssetCheck` fails on any
      zip-content-vs-disk gap, so the removal pass must teach it the superseded allowance (or archive the
      relevant zips out of its scope) in the same commit — otherwise the deletion turns a passing check red.

      ⚠ **RULED 2026-08-12 — the stamps have nothing to stamp.** `ParliamentSystem.ApplyBillResult`
      applies a bill's effects and the bill is discarded; there is **no resolved-bill record anywhere** —
      no history list, no outcome field. `ui_stamp_carried` / `ui_stamp_rejected` cannot be placed
      without one, and inventing a placement would put a state marker where no state exists, which reads
      as a simulation bug rather than a style choice. **See the new roadmap item below.**

      ⚠ **RULED 2026-08-12 — the seals are Canvas-path.** §A's own beat sheet puts them there
      (*"SIGN → pen scratch 400ms → `ui_seal_official` drops 1.3 → 1.0 over 140ms"*), and no signing
      moment exists in the IMGUI path to attach one to.

   1a. ✅ **CLOSED 2026-08-12 — and its premise was false when written** (see 2a below): the record
      already existed (`DivisionLog`, `a7bd40d`), the UI half was the missing piece, and `ab1b72f`
      built it — the Division Records panel on Parliament, stamps placed, verified at both sizes on
      USA and Germany with six real divisions per set. **What the Canvas election night still needs
      is a different record entirely — an ElectionRecord** (elections leave only a transient result
      discarded on dismissal); scoped in `CLAUDE.md` "Election-night scoping", not built, per the
      ruling that the Canvas pilot is a separate pass.
   2. **The Canvas path** — fully specified, nothing built: eight narrative screens, the hand-off
      envelope, the scrim. `CANVAS_SPEC.md`. The country selector is the obvious pilot, being
      self-contained and already a full-screen state.
   3. **The eleven superseded pre-v2.0 chrome sprites** — removal-safe from the code side per Phase 2's
      derived statement (zero load calls, checks tolerate removal, one `DeliveredAssetCheck` execution
      note recorded there). Gated on Elias's ruling on that statement, including its 8 orphans.

   Spec reference for anything row-shaped remains **§A.9** with **§A.9a** (resort ladder), **§A.9b**
   (negative fill = no gauge) and **§A.9c** (Parliament's real pointer; §A.10 is Buttons).

   **The Budget screen was the right thing to finish first** — all five row types, each captured before
   the next was started. It is the densest screen in the game and the one the spec was stress-tested
   against.

   | row type | mapping | control |
   |---|---|---|
   | Tax | standing tick at the enacted **rate** | button + slider |
   | Spending | standing tick at **zero** — the slider carries a percentage *change* | slider |
   | Welfare | standing tick at the enacted **generosity** | button + slider |
   | Infrastructure | **read-only gauge** — condition is an output, not a dial | **none** |
   | SWF | tick at the standing value, on a range that **spans zero**; trailing column carries normalised share | button + 6 sliders |

   **One widget, five semantics, and the shape held for all of them.** The two that looked like they
   would need special handling did not: a negative contribution just puts the knob left of centre, and
   normalised weights turned out to be what the trailing column was always for.

   **The next screen is named in the derived table above, not here** — this line offered Policy/Laws as
   a candidate for a day after it was converted. Keep the sequence — **one type per capture, never
   batched.** That is not
   ceremony: the tax row took three capture rounds, and each found a defect the code review had passed
   (a shared-style mutation degrading every screen at once, columns overflowing their panel, a button
   measured in the wrong style).

   | | |
   |---|---|
   | Capture harness | **In the repo and working.** `UiScreenshotCapture` + `UiScreenshotDriver`, 17 shots at 1600×900, per-sub-category on Budget with a scrolled second pass. **No `-batchmode`** — `WaitForEndOfFrame` never resumes there |
   | `LedgerRow` | **Built, captured, proven across five semantics.** `Draw` for controlled rows, `DrawReadOnly` for gauges, shared `Columns` so both align |
   | Budget screen | **Done.** Tax · Spending · Welfare · Infrastructure · SWF |

   **Two rules carried into that work:**

   1. ⚠ **Every number the spec supplies is suspect until derived or explicitly confirmed as fixed** —
      see the banner at the top of `POLISIM_V2_SCREEN_SPEC.md`. Two instances in one day, the second
      written a method away from the warning about the first. "This one is genuinely fixed" is a real
      answer; a number that survived because nobody questioned it is not.
   2. **Declare deviations from the boards rather than diverging silently** — the request doc now
      carries a DEVIATIONS section, distinct from the import blockers, holding V1 (the
      "(current seat composition)" qualifier moved to the screen header).

   ~~**Open with Design:** five import blockers in §1E, none of which block the IMGUI path.~~
   *(RECONCILED 2026-08-12: §1E closed, all five, verified 2026-08-11 by per-item enumeration against
   disk — the request doc's own status line records it. What remains open with Design is §1G's
   `mark_party_us_lib` — WRITTEN, NOT SENT — and the §1F.1 rasterization diff; both live in
   `MISSING_PREREQUISITES.md` §E.)*

   ~~**Still unwired, and each needs CALL-SITE work rather than a style assignment** — a stamp goes on a
   resolved bill, hatch on a draft delta, the seal on a signing: `ui_stamp_carried/rejected/draft` ·
   `ui_seal_official` · `ui_folder_dossier` · `ui_portrait_frame(_oval)` · `ui_banner_hold` ·
   `ui_hatch_draft` · `ui_grain_tile` · `ui_scrim_takeover` · `ui_calendar_pad` · `ui_tab_spine` ·
   `ui_subtab_on/off` · `ui_chip_outline` · `ui_slider_tick`.~~ **SUPERSEDED 2026-08-12** — this was the
   original list of 13; the corrected table above is authoritative, and its IMGUI half is now fully
   wired. (`ui_hatch_draft` and `ui_stamp_draft`'s pencil carrier were already live in `LedgerRow`.)

   **Then the Canvas path**, which has a full specification and nothing built: eight narrative screens,
   the hand-off envelope, and the scrim. `CANVAS_SPEC.md` is reproduced in the request doc; the country
   selector is the obvious pilot, being self-contained and already a full-screen state.

   **One inherited defect worth fixing early**: the eleven pre-v2.0 chrome sprites are now entirely
   superseded and removable in one pass, once the v2.0 chrome is confirmed.

10. **DEFINED 2026-08-12 — REALISTIC POLITICS AND ELECTIONS.** This number was referenced in three Open
   Questions entries ("when item 10 lands", "`usa_election_check` is scoped to item 10") **without ever
   being defined on `main`** — a dangling pointer the reconciliation closes. **Item 10 IS the work
   specified in `POLISIM_POLITICS_ELECTIONS_ROADMAP.md` on `stranded/politics-elections`** (commit
   `ca6c510`, contents enumerated in Open Questions): real parties and institutions under the split
   rule 9, per-country chambers and electoral formulas, the hybrid national-swing vote model, USA as
   the first vertical slice. **Gate, per Elias 2026-08-12: priced after Sweden votes 13 September
   2026** — the branch's own seed data carries retrieval dates for exactly this expiry. ⚠ The branch
   doc's §1 maps precisely what item 10 replaces on `main` (`PartyArchetype`, `TotalSeats = 200`,
   `ElectionSystem`'s approval threshold) and what it keeps (seat drift, bill scoring, the renderers,
   `PublicationSystem` for polling) — **main's documents describe the four-archetype system as current
   because it IS current on main; the branch doc plans its replacement. Neither is stale; the
   disposition of the collision is item 10's own work, not a documentation fix.** See RULINGS for the
   one main-side dependency this creates (the ElectionRecord).

   ### RESOLVED — was blocking the design brief

   1. ✅ **The eleven-hue question — ANSWERED: all eleven survive, aged and desaturated, no non-colour
      carrier.** The evidence was that colour keys charts as well as identity: emblems drawn *instead of*
      the hemicycle legend's swatch broke the legend's correspondence with its own arcs. A mark cannot
      substitute where the mark is not what the chart is drawn in. Inks now live in `UiPalette`.
   2. ✅ **The font test — ANSWERED**, and it also surfaced that draft amber and the Political hue were
      the same hex. They are now separate. See `CLAUDE.md`.

   Not blocking, but must be settled before art is commissioned: the three modals that render in TWO
   places via `drawOwnFrame` each need both a framed standalone and an unframed embedded treatment.

---

# PART ONE: Continuous Time Migration

*(Full original plan preserved — becomes step 3 and step 7 of the master sequence above.)*

## Why this exists

> ⚠ **TURN LENGTH CORRECTION, 2026-08-12: a turn has been 365 days since `d8f55ce` (2026-08-10,
> discontinuity 3).** Everything below was written at 121 days and is preserved as the original plan;
> every `/121`, `^(1/121)` and "121 consecutive days" reads `/DaysPerTurn` — the constant, which
> Phases 1–3's implementations already read rather than baking in, which is why they survived the
> change. **Phases 4–5, when picked up, calibrate against 365**, and Phase 4's `YearsPerTurn` note is
> doubly live: that constant is now exactly 1.

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

> ⏸ **PHASES 4 AND 5 ARE DEFERRED BEHIND v2.0 (Elias, 2026-08-03) — deferred, NOT cancelled.** Phases 1–3
> are done and their daily systems are live. Until 4 and 5 land, demographics and the core macro engine
> stay turn-shaped and the game runs a **hybrid simulation**: a daily calendar with daily fiscal and
> social systems over a 121-day macro core. See the execution-order block at the top of this file for the
> full consequence. **Everything below stays live and unmodified — these notes are waiting, not stale.**

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

Batches 1-6 cover every tab, and nothing further is planned under 5e's visual scope. ~~**Batches 4-6 are
BUILT but NOT live-confirmed** — that confirmation is the only outstanding 5e work~~ *(RECONCILED
2026-08-12: this sentence outlived its own closure by ten days — step 5 CLOSED 2026-08-02 when Elias
confirmed all eleven review items, this file's own "Where things stand" said so, and `COMPLETED.md` §16
holds the record. Two statements of one fact, one stale; the closure is the one with evidence.)*

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

- ✅ **RULED 2026-08-11 — CAPTURE AT 1440p AS WELL. Done the same day, and it came back clean.**
  `UiScreenshotCapture` now takes `-shotwidth=` / `-shotheight=`, defaulting to the old 1600×950.

  **Result at 2560×1440** (actual Game View 2560×1419, the 21px being window chrome): **55 captured, 0
  failed, 0 text overflows, 0 containment escapes, 0 clipped edges.** Clean at both sizes.

  **Elias's reasoning, recorded so it is not re-litigated:** a second resolution is a capture-config
  change rather than a code change, and it converts three separate resolution-scoped claims into real
  ones. **All three had been stated without saying what they covered:**
  1. `LedgerRow`'s 0.35 squeeze floor "never engages" — measured across 106 Repaint geometries, **all at
     1600×929**. Now confirmed at 1440p too, by render rather than by arithmetic.
  2. Instance #12's closure, "0 of 55 clipped" — same single size. Now 0 of 55 at both.
  3. `ScreenEdgeCheck` itself, which can only ever answer for the captures it is handed.

  ⚠ **This RETIRES `ledger_geometry_check.py` without porting it.** That script existed only because no
  1440p *render* existed — it re-derived `LedgerRow.Columns` from source to answer arithmetically what
  nobody could answer by looking. A real capture at that size answers it better, so the script is no
  longer cited as evidence anywhere. It is also, as of 2026-08-11, unrunnable here (no Python), which is
  the second reason not to port it.

  **Still not covered:** 1920×1080 and a deliberately narrow window. Nobody is blocked; the two sizes now
  captured bracket the range that has ever produced a report.

- ✅ **RULED 2026-08-11 — REAL REPORTING LAG: CLOSED AS WON'T-DO. Reopenable on request.**
  An optional realism refinement to Continuous Time, which is itself deferred behind v2.0 — so this is an
  optional refinement to deferred work, and it has sat unprioritised without blocking anything since it
  was raised. Closing it is not a judgement that it is a bad idea; it is a judgement that a question
  nobody is waiting on should not sit in a list people read looking for work.

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

  ⚠ **A "STATUS AUDIT 2026-08-11" ENTRY HERE CLAIMED THIS RULING SHIPPED HALF. THAT WAS WRONG, and the
  claim is quoted rather than deleted** — it read *"The cause-fix does not exist: routing SWF returns
  through the fiscal reaction multiplier appears nowhere in `MacroSystem`."* It was produced by grepping
  **one file**. The fix is in `SimulationManager.cs`, and a tree-wide search finds it immediately. See
  `CLAUDE.md`, *"An absence claim greped from ONE FILE is rule 14 inverted"*, for what that cost.

  ✅ **FULLY IMPLEMENTED — `0386e83`, "Fix the cause: SWF returns now run through the fiscal reaction
  multiplier". BOTH candidate fixes shipped, not one**, and both are recorded here because a reader who
  finds one may otherwise assume the other was declined:
  1. **Inside the multiplier.** `SimulationManager.cs:2682` —
     `(theoreticalRevenue * effectiveCollectionEfficiency + swfReturns) * fiscalReactionMultiplier`,
     replacing `... * fiscalReactionMultiplier + swfReturns`, which had put the fastest-growing component
     of a net creditor's revenue permanently beyond the one mechanism that pushes back.
  2. **Returns treated as a stock, not budget revenue.** `SwfStructuralDrawPercentPerYear = 3f` — Norway's
     *handlingsregel*. The budget takes a smooth draw proportional to fund SIZE; the realised market
     return never reaches it. This also closed a **double-count**, where the fund kept the return and the
     government spent the same figure.

  🔴🔴 **SUPERSEDED BY THE 1000-TURN RUN — SEE `CLAUDE.md`, "A measurement is only comparable to another
  taken at the same horizon". NOTHING BELOW IS AN EQUILIBRIUM.** At turn 1000 all six countries are still
  climbing (USA 154.7, Sweden 11.2, Germany 80.4, France 108.8, Italy 165.6, Poland 45.6), so the
  "moved / did not move" reading below is an artifact of stopping at 500. **The drift is UNATTRIBUTED** —
  the same matrix has not been run on the commit before `0386e83`, and blaming the SWF fix without that
  run would repeat the error. Measurements retained below with their horizons; the word *equilibrium*
  stays out until a longer run earns it.

  ✅ **MEASURED 2026-08-11 — `DebtClampDiagnostic`, real Unity, seed 777, 120 turns, post-`0386e83`.**

  **THE GUARD NEVER ENGAGES.** `negativeTurns = 0/120` and `ceilingHits = 0` for **all six countries** —
  gross debt never goes negative at all, so `NetCreditorRunawayGuardPercent = 1000f` is **a backstop that
  has never been reached**, not something holding a runaway back. Per the original ruling's own words,
  *"if any country reaches it, that is a bug report, not a clamp"* — nothing does. Sweden shows
  `netCreditorTurns = 120`, i.e. net position (debt minus fund) is negative every turn while gross debt
  stays positive, which is exactly the state the −300% bound used to forbid.

  **THE SIX EQUILIBRIA, against their recorded baselines:**

  | | baseline | measured | move |
  |---|---|---|---|
  | USA | ~142% | 137.7% | −4.3 |
  | Italy | ~107% | 114.0% | **+7.0** |
  | France | ~90% | 92.9% | +2.9 |
  | Germany | ~35% | 37.9% | +2.9 |
  | Sweden | ~13% | 6.9% | **−6.1** |
  | Poland | ~26% | 27.7% | +1.7 |

  ⚠ **SWEDEN AND ITALY MOVED ENOUGH TO NAME THIS A RECALIBRATION, not a bug fix.** Sweden roughly halved,
  which is the expected direction and the expected country — it has the largest fund, so it is where
  putting SWF income inside the stabiliser bites hardest. Italy moved +7.0 with no fund to speak of,
  which is **not** explained by the SWF change and is the one number here that wants its own look.

  **FRANCE — the case that distinguishes a cause-fix from a symptom pushed further out.** It reached the
  old bound and sat near it. Now: `1 of 117` years with a rating notch move, largest move **1 notch**,
  largest year-over-year swing 7.6 points of GDP — and it is **no longer the outlier**, since Italy shows
  the same 1 of 117 and every other country shows 0. The thrash is gone, not relocated.

  🔴 **STILL NOT RUN: `BatchSimulationRunner` at 100 and 500 turns.** The above is a 120-turn diagnostic,
  which answers the guard and the equilibria but is not the full matrix the standing discipline requires
  for anything touching the fiscal engine. **Italy's +7.0 should be read against that matrix before it is
  explained.**
  - **The guard.** `DebtClampDiagnostic` reports runaway-guard hits, and the roadmap's "zero hits before
    and after" predates this commit. Until it is re-run, whether
    `NetCreditorRunawayGuardPercent = 1000f` is **a backstop that never engages** or **still holding a
    runaway back** is unknown — different artifacts, distinguished only by measurement.
  - **The six debt equilibria**, against their recorded baselines (USA ~142%, Italy ~107%, France ~90%,
    Germany ~35%, Sweden ~13%, Poland ~26%). ⚠ **If they moved, that is a RECALIBRATION and must be named
    as one**, not absorbed as a bug fix.
  - **France**, specifically: it reached the old bound, sat near it, and was the only country still
    showing year-over-year rating movement. It is the case that distinguishes a cause-fix from a symptom
    pushed further out.

  ⚠ **AND SAY WHAT HAPPENS TO THE GUARD.** Once the cause is fixed, `NetCreditorRunawayGuardPercent`
  becomes one of two very different artifacts, and the record must state which: **a guard that never
  engages** (the intended outcome — dead code kept as a runaway backstop, and per the original ruling *"if
  any country reaches it, that is a bug report, not a clamp"*), or **a guard still holding a runaway
  back**, which would mean the cause-fix did not work. Measuring that distinction is part of the job, not
  a follow-up to it.

  **Sequenced ahead of the C4 deficit-term work**, which reads the value this unclamps.

- 🔴🔴 **C4 SUPERSEDED 2026-08-11 — UNBOUNDED DEBT DIVERGENCE IS NOW THE FISCAL-ENGINE PRIORITY.** Ruled
  by Elias after the 1000-turn matrix. **The reason, recorded so the sequencing change is visible rather
  than implicit: C4's deficit-term investigation READS a series that never settles**, so it would be
  investigating the divergence by proxy and calibrating against a moving target. Measured at turn 1000:
  USA 157.3, Germany 81.3, Italy 166.3, Poland 48.9 — all still climbing.
  *(Superseded ruling, kept: "the C4 deficit term is the next fiscal-engine work after the SWF
  cause-fix" — correct when made, on a premise the matrix then replaced.)*

  ✅ **`0386e83` IS INNOCENT FOR ALL FOUR.** Pre-fix and post-fix trajectories match to within ~3 points
  at turn 1000. **The Fiscal Reaction Function has never equilibrated**, and every "equilibrium" quoted
  since 2026-07-22 was a waypoint. **Italy's "+7.0" closes** — never attributable to the SWF change, one
  of the four on the same path before and after.

  ✅ **AND `0386e83` IS VINDICATED, which the record should say as plainly as it has said everything
  else.** Pre-fix, **Sweden sits at −296/−288/−297 and France reaches −299 at every horizon** — flush
  against the −300% bound, exactly the pinning Elias's 2026-08-02 ruling gave as its first reason
  (*"France at −298% against a −300% bound is not a risk, it is already pinning"*). Post-fix they are at
  **+11.2 and +108.8**: real, unpinned values. **A ruling made on reasoning, confirmed by measurement
  nine days later.** Sweden's ~13% does not reproduce pre-fix either — **the fix exposed a broken
  calibration rather than breaking a good one.** The record has been wrong in the pessimistic direction
  all session; this one deserves stating.

  ✅ **DIAGNOSED — SATURATION, NOT A MISSING MECHANISM.** The FRF already contains a debt-**stock** term
  measured against each country's own `ComfortableDebtToGdpPercent`, correctly signed. It hard-clamps at
  **1.5**, reached 33.3 points above comfortable, and 1.5× effective revenue cannot cover interest at
  45.7% of spending. **Two feedbacks, asymmetrically bounded:** `GetDebtRiskPremium` already responds to
  the debt stock and already reaches the live interest path, so a *positive* feedback bounded only by
  `MaxDebtRiskPremium` runs against a *negative* one capped at 1.5. **That asymmetry is the mechanism.**
  No primary balance exists in the model, and Italy already runs an implied ~+75 primary surplus, so a
  primary-balance term was examined and **rejected on evidence**. Full reading in `CLAUDE.md`.

  ⚠ **SCOPING, RULED BY ELIAS 2026-08-11 — TURN 1000 IS A DIAGNOSTIC, NOT A TARGET.** A turn is 121 days,
  so turn 1000 is ~330 years and nobody's playthrough *(⚠ corrected 2026-08-12: a turn has been 365 days
  since `d8f55ce` — turn 1000 is ~1000 years, so the ruling holds a fortiori)*. **Calibration stays at
  turns 100–200.** The reason
  the long horizon matters is that it reveals whether a restoring force exists **at all** — a model
  without one is wrong at turn 150 too, just not yet visibly. **Judge any fix by whether the mechanism is
  present, correctly signed, and whether the asymmetry is defensible — never by whether turn 1000
  converges.**

  ✅ **DRIVER IDENTIFIED — INTEREST COMPOUNDING, not a pinned stabiliser.** Instrumented on Germany and
  Italy: the multiplier moves freely (0.62 early, ~1.27 and ~1.19–1.45 late, against a 1.5 cap) while
  interest grows from **6%→26%** of Germany's spending and **20%→46%** of Italy's. It leans correctly and
  is outrun. ⚠ Italy's 1.446 approaches the cap, so **the two hypotheses are stages rather than
  alternatives** — a fix aimed only at the cap arrives after the damage. Series in `CLAUDE.md`.

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

- ✅ **INSTANCE #12 — CLOSED ON `main` 2026-08-11. `ScreenEdgeCheck`: 55 captures, 0 clipped, exit 0**,
  all four edges zero on every screen. ⚠ **SCOPE (corrected 2026-08-12, Elias): "every screen" means
  every screen of the USA in the default capture state** — the guards enumerate screens; nobody had
  enumerated states, and no other country's screens have ever been measured. Four commits, each
  measured against a fresh capture:

  | | Commit | L | T | R | B | clipped |
  |---|---|---|---|---|---|---|
  | before | — | 0 | 0 | **841** | **663** | 54 |
  | `InnerWidth` 4th term + tab margins | `f3cbea4` | 0 | 0 | 0 | **663** | 54 |
  | two accessors | `b16b816` | 0 | 0 | 0 | 0 / **1508** | 16 (all Budget) |
  | `BudgetProcessHeaderHeight` | this | 0 | 0 | 0 | 0 | **0** |

  ⚠ **IT READ AS FIXED FOR HOURS WHILE `main` WAS BROKEN, and the reason is worth keeping.** The
  `clipfix2_*` captures showed 0 of 55 and were cited as evidence. They were taken while a closed
  session's uncommitted work sat in the shared tree, so they measured a build containing `InnerHeight`.
  When that work was committed it went to `stranded/politics-elections` — correctly, since unreviewed
  politics code sat beside it — and the layout fix went with it. **A capture is evidence about the tree
  it was taken from, not about the branch of the same name.**

  **What actually closed it: three accessors, each replacing a constant that stood in for measured
  content** — the label-clipping class's original signature, and the shape seven site-specific fixes
  never ended. Reserve and drawing now share one measurement apiece.

- ~~🔴🔴 INSTANCE #12 IS LIVE ON `main`~~ — superseded by the entry above; kept so the progression is
  legible rather than looking like it was always fine.

- ✅ **RULED 2026-08-11 — DO NOT EXTRACT the remaining layout work from `stranded/politics-elections`.**
  Elias's reasoning, which is the ruling: **instance #12 measures zero at both captured resolutions, so
  nothing is pulling that code across** — and extracting layout code with no failing measurement to
  verify it against is the exact failure mode this session spent its length on. Every extraction that did
  land (`InnerWidth`'s fourth term, the three accessors) was justified by a number that moved. Revisit if
  a capture shows something, or when item 10 lands.

- ✅ **RULED 2026-08-11 — `stranded/politics-elections` STAYS AS-IS until item 10 is scheduled.** It is
  pushed, so the work is safe off-machine; merging ~3,500 lines of unreviewed simulation code into `main`
  is precisely what the branch exists to prevent.

  **Full contents, enumerated so nobody has to check out the branch to find out what is on it** (commit
  `ca6c510`, 30 files):

  | Group | Files |
  |---|---|
  | **New data model** (6) | `Chamber`, `ElectoralFormula`, `ElectorateCohort`, `PoliticalParty`, `ThresholdRule`, `UnitedStatesSeed` |
  | **New simulation** (4) | `NationalVoteModel`, `SeatAllocation`, `UnitedStatesElectionCycle`, `UnitedStatesElections` |
  | **Modified, layout half now on `main`** (3) | `GameController`, `PoliSimWidgets`, `IconLibrary` |
  | **Modified, not extracted** (1) | `SimulationManager` |
  | **Python** (4) | `seat_allocation_check`, `usa_election_check`, `ledger_geometry_check`, `screenshot_edge_check` |
  | **Docs** (6) | `POLISIM_POLITICS_ELECTIONS_ROADMAP` (new), plus branch-side edits to `CLAUDE`, `CLAUDE_DESIGN_ASSET_REQUEST`, `MISSING_PREREQUISITES`, `POLISIM_MASTER_ROADMAP`, `POLISIM_SEED_DATA_MACRO_OVERHAUL` |
  | **Editor, since superseded on `main`** (6) | `CheckSuite`, `DeliveredAssetCheck`, `ImporterSettingsCheck`, `PartyMarkCoverageCheck`, `ScreenEdgeCheck`, `StatIconCoverageCheck`, `UiScreenshotCapture` — these differ only because `main` moved on; **nothing on the branch is newer** |

  ⚠ **Two of the Python scripts are already superseded**: `screenshot_edge_check` by `ScreenEdgeCheck`,
  and `ledger_geometry_check` by the 1440p capture ruling. `seat_allocation_check` is the surviving
  evidence behind Part D's Sweden rows; `usa_election_check` is scoped to item 10.

- ⚠ **CAPTURE RESOLUTIONS NOT COVERED — 1920×1080 and a deliberately narrow window (2026-08-11).**
  Two sizes are captured, 1600×929 and 2560×1419, and both are clean. **That brackets every window size
  that has ever produced a REPORT — which is a claim about report history, not about what players run.**
  1920×1080 is the single most common desktop resolution and has never been captured here; a narrow
  window is where the `LedgerRow` squeeze floor would engage if it ever does. Neither is blocking, and
  adding either is now a command-line argument rather than a code change (`-shotwidth=` / `-shotheight=`).

  *(⚠ RECONCILED 2026-08-12: everything below this line described the PRE-extraction state and was
  superseded within hours of being written — the instance-#12 closure entry above records the outcome:
  `InnerHeight` WAS extracted onto `main` (`f3cbea4`), the two accessors and
  `BudgetProcessHeaderHeight` followed, and `ScreenEdgeCheck` has read 0 clipped on every set since.
  Kept struck-through because its middle paragraph — "a capture is evidence about the tree it was taken
  from" — is the lesson rule 15 grew from.)*

  ~~**Measured, not inferred.** A fresh capture of current `main` (`p2main_*`) run through
  `ScreenEdgeCheck`: **55 captures, 54 clipped, exit 1.**~~ The 55th is the full-bleed menu screen,
  correctly never flagged — which also resolves the apparent "54 vs 55" discrepancy in earlier reports:
  **both sets contain 55 captures**, and 54 is the count of *clipped* screens, not of screens.

  ⚠ **Why the `clipfix2_*` captures looked clean, and why that was not evidence about `main`.** They were
  taken while the closed session's uncommitted changes were still sitting in the shared working tree, so
  they measured a build that included `InnerHeight`. When that work was committed it went to the stranded
  branch — correctly, since the politics code alongside it is unreviewed — and the layout fix went with
  it. **A capture is evidence about the tree it was taken from, not about the branch of the same name.**

- 🔴 **P4 REOPENED 2026-08-11 — THE CLIPPING CLASS IS NOT CLOSED, AND "GUARDS GREEN" DOES NOT MEAN
  "SCREENS DO NOT CLIP".** Two guards were built on 2026-08-11 and both are honestly scoped; the class
  produced a **twelfth** instance that neither can see, and both reported zero while it was on screen.
  *(Vindicated 2026-08-12: **instance #13** — the ECB sub-tab label, the first instance reached through
  the COUNTRY axis, both guards structurally blind again. Fixed by `SubTabRowHeight` (`d072286`);
  the sibling survey — constant-sized chrome under wrappable labels, named not fixed — is in
  `CLAUDE.md`. The class stays open.)*

  **Instance #12 — the FRAME, not the text.** Five `GUILayout` groups laid out wider and taller than
  `OnGUI`'s own `BeginArea`, so the area clipped them. Reported by Elias from live play (*"there are
  borders at the sides which cutoff the game"*). It reproduced in that morning's own capture set with
  nothing changed: **54 of 54 gameplay screens carried content on the last drawable pixel of the right
  edge, and 54 of 54 on the bottom** — captured, written to disk, and left unexamined at exactly the
  resolution the defect is visible at. Left and top were clean in every one, and that asymmetry is the
  diagnosis: flush on one side with the opposite side inset is a clip; flush on all four is a full-bleed
  background, which is why the menu screen is not a finding.

  ⚠ **STATE THE SCOPES, because the zeros were correct and were nearly read as an all-clear:**
  - `UiOverflowGuard` asks *does this text fit the rect it was handed?* Every clipped label here fitted
    its rect perfectly.
  - `UiContainmentGuard` asks *does this child rect sit inside its container?* — scoped to three
    composite widgets that lay out a stack inside a fixed rect.
  - Instance #12 is a **GUILayout group overrunning the one container everything is inside**, which is
    neither question. `CLAUDE.md`'s own scope note said a clean run is evidence about fixed-rect drawing
    and *"not about every label on screen"*. **#12 landed precisely in the excluded population.** The
    guards reporting zero is that note coming true, not a contradiction.

  **What caught it: `ScreenEdgeCheck`** (`Assets/Editor/`, ported to C# 2026-08-11 from the original
  `screenshot_edge_check.py`, **because Python is not installed on this machine** — the `py` launcher's
  registration points at a directory that does not exist, so the script cited in three documents as this
  class's detector could not be executed here at all. A detector that cannot run in the environment that
  ships is not a detector).

  ⚠ **WHAT IT ENUMERATES** (rule 14): for each PNG matching the pattern it is given, **exactly four lines
  of pixels** — the margin column and row on each side. Not the interior, not any screen outside the
  pattern, and **only the resolutions actually captured — which since 2026-08-11 is two, 1600×929 and
  2560×1419, both clean.** It reports FLUSHNESS, never overrun
  magnitude: clipped content stops at the boundary, so the pixels past it are absent from the capture and
  cannot be measured. **A clean run says nothing about how much slack a screen has left.** It flags
  **right and bottom only** — GUILayout grows rightward and downward, so that is where an over-wide group
  runs off; content clipped at the left or top would pass. Full-bleed on an axis (both sides flush) is a
  background rather than a clip, which is why the menu screen is correctly never a finding.

  **Verified in both directions before being trusted**, per the self-test discipline: against `run_*`
  (known clipped) it reports **54 of 55**, exit 1, with `L 0 / T 0 / R 841 / B 663` — an asymmetry far too
  large to be a threshold call; against `clipfix2_*` (known clean) **0 of 55**, exit 0. Those numbers
  reproduce the Python original's exactly, which is the evidence the port is faithful. It also reads
  `ScreenMarginFraction` from `GameController` rather than duplicating it — the original carried a copy
  with a comment saying *"if that constant changes, this must change with it"*, which is two statements of
  one fact.

  ⚠ **DO NOT BUILD A THIRD SITE-SPECIFIC GUARD.** Before writing a GUILayout-aware check, state what it
  would have to enumerate — and the honest answer is that the two existing guards hook *drawing*, while
  this defect lives in *layout*, one phase earlier and in a phase where (per the Repaint-gating lesson)
  rect dimensions do not yet exist. A real check would need: every `BeginArea`/`BeginScrollView` on the
  stack with its rect; every `GUILayout` group's *requested* min/max width from
  `GUILayoutUtility.current.topLevel` rather than from a drawn rect; and a comparison at the moment
  layout resolves, not at Repaint. None of that is reachable from the public IMGUI surface without
  reflection into `GUILayoutUtility`'s internals. **The pixel check is cheaper, already exists, already
  works, and asks the question the player actually experiences.** Prefer running it over building the
  reflective one, and if the reflective one is ever built, it must be justified against this paragraph.

- ⚠ **The four `mark_party_*` sprites are imported, corrected and guarded on `main` for a feature that
  exists only on `stranded/politics-elections` (2026-08-11).** `main` has no `PoliticalParty`, no
  `IconLibrary.GetPartyMark` and no party rendering at all — its Parliament screen still draws the four
  fictional `PartyArchetype`s with `emblem_party_*`. So the art is present, correct (RGBA32, verified),
  and covered by `PartyMarkCoverageCheck`, while **nothing on `main` draws it**. The check reports
  `PARTY SYSTEM NOT PRESENT` and exits 0 there, which is honest rather than green. This is the
  `menu_pattern_tile` shape — an asset landing ahead of its consumer — but recorded up front this time
  instead of after weeks of three documents calling it a gap.

- ✅ **The 0.35 squeeze floor — now confirmed at TWO resolutions (2026-08-11).** It was recorded as
  "never engages" from 106 Repaint geometries **all at 1600×929**, which was "confirmed at one
  resolution" stated as if it were "the floor is unreachable". Following Elias's 1440p ruling it is
  confirmed by render at 2560×1419 as well: 0 overflows, 0 escapes, 0 clipped edges.

  **A geometry conclusion drawn from captures is scoped to the capture resolution** — the same shape as
  rule 14, and the reason this entry existed at all. Still scoped to those two sizes, not to all.
  ⚠ **And scoped to the USA in the default capture state (correction 2026-08-12)** — the squeeze floor
  has never been rendered against the legacy spending screen or any non-USA ledger content.

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
