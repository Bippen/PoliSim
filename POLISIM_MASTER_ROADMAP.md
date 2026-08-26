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
    than "does this read well" (wrapping is exactly the shape rule 15's original `d44ab2d` finding also
    was — a composition question, not a containment one). Read the table's three rows as three
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
  uncalled" state this project repeatedly mistook for done. ~~Needs a visual look~~ — ✅ **the look
  happened**: the Derived panel converted to the row family (`397d829`) and was reviewed by eye at
  2560 in playtest 2; struck 2026-08-26 (ruling C3).
- **WAITING, NOT LIVE** — `MISSING_PREREQUISITES.md` slimmed to the live register (2026-08-26):
  **§S** the send package (§8 + §9 + the D1 verdict + the 1j-aware note — waiting on Elias's send),
  **D1** the batch of nine (waiting on Design's delivery, gate cleared 2026-08-26), **E2** (item 10,
  13 Sept), **E3** (a raster path whose output is comparable), and **§B's three seed quality debts**
  (a database re-sourcing session; none blocks a batch). All closed prose migrated to
  `COMPLETED.md` §23.
- ✅ **FISCAL-ENGINE — the whole arc CLOSED 2026-08-17** (sweep-empty `afe0f24` 08-16 → the
  stock-vs-flow mechanism report `bcbba47` → erosion R1–R3 `685ebd5` → maturity R4 `b05150f` →
  F1/R5 `720ccee`). The divergence graduated from diagnosis to measured mechanism limit, the two
  missing identity terms shipped one per baseline, and C4/A1/F1 all closed on it. Records:
  `COMPLETED.md` §§22/23. *(This bullet's "next: the report… C4 waits on it" framing had been
  stale by nine days at the 08-25 re-derivation and is struck in the 2026-08-26 consolidation,
  not silently. History: diagnosis 2026-08-11; the debt-to-zero bimodality FIXED 2026-08-02,
  `COMPLETED.md` §18.)*
- **IN FLIGHT — v2.0 (item 9-v2.0):** the IMGUI half is COMPLETE through the chrome placement track
  (2026-08-12 — row family, six placements, Division Records, every chrome sprite dispositioned by
  Phase 2's derived statement). Coverage machinery standing: six countries × two sizes captured, the
  reachable state axes pinned, rule 15 in the discipline. ~~**Next major track: the Canvas path**~~
  **The Canvas track CLOSED 2026-08-12** (`e0a510f`; §A.14 set 2 of 3, ELECTION NIGHT item-10-gated
  per R2) and **the folder-tongue pass closed 2026-08-16** (`9497673`, startable row 3) — **item
  9-v2.0 now has NO ungated work left.** What remains of it waits on item 10.
- **NOT STARTED, UNBLOCKED OR GATED** — ~~item 8 (save/load: scoped, zero persistence code exists —
  re-verified 2026-08-12, search `persistentDataPath|JsonConvert|CaptureSaveState` over
  `Assets/Scripts`, no hits); item 7 Phases 4–5 (deferred behind v2.0, now against 365-day turns);
  item 6 (Round 4, after 7/8);~~ ✅ **all three CLOSED 2026-08-16/17** (item 8 core + saves menu, item 7
  Phases 4–5, Round 4's five batches — this bullet outlived its own facts by nine days while the
  Master Sequence record below and `COMPLETED.md` §19 said so; corrected in the 2026-08-25
  re-derivation, not silently). **Item 10 (elections — gated: priced after Sweden votes 13 Sept
  2026) is the one that remains.**

- **Board state, RE-DERIVED 2026-08-25** — from `git log --since=2026-08-24 --format='%h %ad %s'`
  (18 commits, `a13dd7b` → `6804c6d`, all dated 08-24/08-25) read against CLAUDE.md's entries from
  "The country-selection capture-driver leak" through "The detail-pane width, ruled and built"; the GC
  gate re-checked with `curl https://api.github.com/repos/Bippen/PoliSim` (`"size": 9221`); the
  prompt-rate constants re-read at HEAD by grep (`MeetingChancePerDay`, `DecisionChancePerTurn`,
  `ElectionCycle`, `DaysPerTurn`, `BillDurationDays`, `CabinetPortfolio`'s six members). Nothing in
  this block is from summary memory; the roadmap had NO entry for any of it before this pass.
  - **The law system — SHIPPED, 50 of 50, one category (Crime & Justice).** MVP slice `ca11f9a`
    (08-24: a law is a NAMED PRESET over the existing dial space, reaching Parliament through the same
    gated-bill path every other bill uses) → marathon batches 1–3 (`de34b4b`/`c9e9e16`/`785da64`) →
    close-out `555f4cc`, which found and fixed **the composition architecture's one real bug**: dials
    are now a PURE FUNCTION of `Country.EnactedLaws` — every enacted law's delta summed from the 50
    baseline, clamped exactly ONCE (`RecomputeCrimeJusticeDialsFromEnactedLaws`), never nudged
    incrementally — so any enact/repeal history in any order lands exactly → STOPPED at 38 on the
    browser's own navigability condition → §7 written → Design delivered board 1i +
    `LAW_BROWSER_BOARD_RULINGS.md` (`315cca0`) → browser rebuilt against it, 15 review findings fixed
    (`dddec9f`) → batches 4–5 to 50 with the saturating composition re-run (`eb11b78`: 27 of 50
    enacted, FOUR dials clamp at once, full repeal nets exactly 50.0000 on all six) → detail pane's
    width built (`6804c6d`). Byte-identity for the no-law path holds by construction (`LawCatalog.All`
    is read only from the UI layer — grep, not argument). **Live residue, none blocking**: the
    category filter is inert until a second `LawCategory` exists (a content gap, reported as such —
    not a UI bug); five categories sit at 0; the wanted-effects log names eight axis-level gaps the
    six dials cannot represent. **→ Now scheduled: the ruled build order (below) names the
    couplings pass and the second-category pass as its items 2 and 3.**
  - **Playtest 1 package (five scoped, two fixed — the scoping file consumed to `COMPLETED.md` §21, 2026-08-26) — dispositions:**

    | item | state |
    |---|---|
    | rejected-bill seal · Budget's dead nested scroll | ✅ fixed 2026-08-18 (CLAUDE.md "First real playtest session") |
    | Turn → Year | ✅ 2026-08-24 (CLAUDE.md "Turn -> Year") |
    | Calendar Panel | ✅ `a13dd7b` 2026-08-24 |
    | Decision density | ✅ **CLOSED ON THE NUMBERS — ruled 2026-08-25 (Elias).** Measured at 50 laws, same method as the scoping: automatic prompts/yr UNCHANGED by construction (≈5; 5.87 at a full six-minister USA cabinet with the election reveal now ruled into the table); named enactable choices **19 → 69** (13 tax + 6 welfare programs, then +50 laws, multiple pending at once, 21-day resolution). The 08-18 ruling's own prediction confirmed: the table did not move, the menu did. CLAUDE.md "Decision density re-measured". **Whether it READS as closed is the next playtest's own named item — riding gates, Play** |
    | Portraits (D1) | 🟡 GATE CLEARED 2026-08-26 — the register side-by-side PASSED; the batch of nine now waits on Design's delivery, the verdict travels in the send package (`MISSING_PREREQUISITES.md` §D1/§S) |
    | Law system | ✅ above |
  - **The two-copy consolidation — rule 13** (`faecdce`, `0c2a747`, `bb6ad14`, finished 2026-08-25):
    G: is the working copy; the C: copy is `PoliSim-ARCHIVE-DO-NOT-OPEN-2026-08-16` with
    `ProjectSettings.RETIRED` (un-launchable, `.git` intact). Standing habit, stated in CLAUDE.md
    against rule 13's entry: every harness/tool invocation passes the explicit project path — the
    `/code-review` fall-back to `C:\Users\elias` is the instance.
  - **§7 (the law browser request) — OVERTAKEN, struck through in the request doc (`6804c6d`).**
    Never sent; the board arrived first, the rebuild consumed the captures it cited. A courtesy update
    to Design with the BUILT board's captures is a note, not a request.
  - **The ~23 Aug GitHub GC gate — CLOSED 2026-08-25**: `9221` KB (~9.0 MiB) against the 08-16 reading
    of ~746 MiB. GitHub's own maintenance collected the unreachable objects; no support ticket.
  - **Playtest 2 (2026-08-25, live, Sweden) — seven items, dispositions:**

    | item | state |
    |---|---|
    | 1 · ATTRIB (Sweden 2027-01-01, +1.5000) | ✅ FIXED — the first-touch window class; writer = foreign-policy "Send substantial aid"; approval recorder now opens at the pre-write value on every path, mirroring the debt twin's 08-18 closure. Reproduced red then proven green by `LedgerFirstTouchDiagnostic` (new, stays as coverage); RT 12/12. CLAUDE.md "Live playtest 2" |
    | 2 · Surplus display | ✅ VERIFIED + BUILT — hypothesis refuted (the row already showed the net-of-interest real balance); "Primary deficit/surplus … excl. interest" added as the labeled second line from the same report. Sweden's outsized measured surplus was a SEED question → ✅ **RESOLVED by the recalibration pass (2026-08-26)**: re-measured +14.15% year-1 primary at the harness (the live-session 32% was the same defect read off the Budget display), recalibrated to −0.64% against the real −0.7 |
    | 3 · Compass labels | ✅ FIXED (two iterations — label-vs-label AND label-vs-dot, leader lines) — verified both sizes, `play2fixb*_07b` |
    | 4 · Sweden budget depth | ✅ BUILT (ruled: decomposition now, Sweden first) — 24 sourced utgiftsområde lines (regeringen.se, vårprop 2026, retrieved 08-25); all-discretionary + not-byte-identical deviations stated with measured reasons (CLAUDE.md "Item 4 BUILT"); recalibration ✅ **SHIPPED as build-order item 1 (2026-08-26)** — the revenue artifact absorbed (targets re-anchored, CE re-solved), the flags flipped, the level question answered with the mandatory block; the other four countries' decomposition passes now decompose CORRECT totals, unscheduled behind item 2 |
    | 5 · Riksbank independence | ✅ C NAMED (ruled: C now, B the destination) — the deliberate-choice paragraph on the Federal Reserve tab + `PolicyDecision` doc; B recorded beside item 10 with its two named gates (output-gap fix; item 10's machinery) — see Step 4's block |
    | 6 · Law pros/cons | ✅ BUILT (ruled: neutral derived via the declared table) — `CrimeJusticeCouplings` read by the Apply* formulas themselves; byte-identical 6/6 diffs; "Expected effects" in the detail pane with the coupling gaps visible, logged as the couplings-pass input (CLAUDE.md "Item 6 BUILT") |
    | 7 · Law-page clutter at 50 | ✅ BUILT — Design answered with Screen 1j ("Law browser at 50"), implemented same-day 2026-08-26 (CLAUDE.md "Board 1j implemented"; §7.1 migrated to `COMPLETED.md` §24). Residue: the eye review of the `board1jc*` sets rides the Access row |

    Parliament with real parties/mandates = item 10 (13 Sept), stated in the playtest and already
    where the work is.

## The ruled build order (2026-08-26) — five scheduled passes, in sequence

*Terminal rulings, rule 4 (the consolidation pass). Each runs under the full standing bar when
its turn comes; nothing is dated.*

1. ✅ **The recalibration pass — SHIPPED 2026-08-26** (it grew from "Sweden's level question" to
   the full fiscal seed recalibration all six countries needed): the five EU pairs re-anchored to
   one-basis real figures (Eurostat/ECB/CBO, sourced and dated), the ~20%-of-GDP mandatory
   transfer block seeded (pension + residual per the terminal ruling), Sweden's UO10/11/12
   flipped mandatory with its PotentialGDP re-solved (614.25), Italy Debt Crisis re-premised to
   stabilization (≤145 by t30), the full bar green, and the SIXTH baseline discontinuity
   recorded. CLAUDE.md "The fiscal seed recalibration ships" is the record; the year-1 primary
   balances now land on the real 2025 structural positions. **The other four countries'
   decomposition passes now decompose CORRECT totals** and stay unscheduled behind item 2.
2. ✅ **The Crime & Justice couplings pass — SHIPPED 2026-08-26.** The gap list consumed by four
   terminal rulings: SentencingSeverity→PrisonPopulationRate at S=1.6 (NRC-2014-anchored parity
   with the admissions channel — Truth in Sentencing's pane line +16 → +40/100k); the budget
   edges LINE-RESIDENT AND FEEDING G per the ruling (real Justice/HomelandSecurity/Migration/
   PublicServices lines, neutral-anchored, the incarceration variable cost at 1.0 GDPpc/inmate
   completing sentencing→prison→budget); BorderEnforcement's second sim edge DECLINED with
   reasons recorded (single-edge ruled honest). Wired-inert control 6/6 byte-identical; no-law
   path byte-identical 6/6; decomposition measured exact; full bar green at all four sizes.
   CLAUDE.md "The Crime & Justice couplings pass ships" is the record; the wanted-effects log
   (`LawCatalog.cs`, eight axes) stays a research archive drawn on as needed.
3. ✅ **The second LawCategory content pass — SHIPPED 2026-08-26 (pass 3).** LaborMarket, 50
   laws in five charter batches (catalog 100, 50/50). Ruled COEXISTENCE ("keeps sliders" — the
   deliberate anti-precedent to C&J's read-only conversion) shipped as the base+offset two-book
   split: bills own `Country.*Base`, laws sum deltas on top, one clamp at composition — order
   invariance, exact repeal-to-bill-base, cross-category isolation and the Sweden minimum-wage
   gate all proven (`LaborLawCompositionDiagnostic`, new). `LaborCouplings` is the second
   declared table (ten constants moved verbatim; the per-category-table generality finding
   reported, not absorbed); per-dial magnitude scales put Kaitz-point and week dials on the
   shared grid. Board 1j's chip row and category cell returned exactly as promised — the
   category filter genuinely narrows for the first time (1i's five drawn categories never
   entered the enum; no hatched chips render). Density 119 named enactables vs 69. Full bar
   green: byte-identity 6/6 (couplings-era baselines stand), RT 12/12 with both-category laws
   crossing saves, equivalence 117/117, captures 4 sizes 99/99 with 0 guards (the 1280 floor
   sweep's 22 overflows fixed on the codebase's own ladders). CLAUDE.md "The second law category
   ships" is the record.
4. **The Riksbank-B gate-1 fix** — the Taylor-path output-gap distortion (the −14.5% structural
   gap pins the suggested rate to the 0-floor regardless of realized inflation; evidence:
   `COMPLETED.md` §22, the Q5 §3 table). Its stated vehicle (Q5) shipped without it; scheduled
   here so option B is buildable when item 10 opens (13 Sept). B's second gate — item 10's own
   appointment machinery — still applies to B itself.
5. **Tariff-to-stock** — the F1-shaped fix: tariff revenue reaches the Budget display accumulator
   and never the debt stock (CLAUDE.md "What remains dark"); enters as a fix entry per the F1
   precedent rather than staying a stated model property.

**Queued at fired or named triggers (ruled 2026-08-26), not scheduled:**

- **The causal-graph screen — trigger FIRED 2026-08-25**: the ledger carries a third stat's terms
  (Approval, Consumer Confidence, Debt), and the term IDs ARE the derived stat→stat edge list.
  Queued per the fiscal-chain precedent; derived, never authored.
- **Per-scenario term accumulation** (the epilogue's named v1 upgrade) — trigger: the first
  scenario whose epilogue reads wrong without it.
- **Investment deepening (R-Q5e)** — return trigger: a capital stock ships, or I/GDP measures
  cyclical (both conditions recorded with the deferral, `COMPLETED.md` §22).
- **StatNodeId/icon promotion** for R4-1's two Society rows (youth unemployment, life
  expectancy) — the icon ask joins the next Design asset batch after D1's nine land.

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

⚠ ~~THE CONSEQUENCE, STATED PLAINLY: the game stays on turn-stepped macro for the whole of v2.0, and
that is not a temporary state with a known end date.~~ ✅ **DISCHARGED 2026-08-16 — the sequence ran
to completion in its ruled order (v2.0 → item 8 → Phases 4–5) and item 7 is CLOSED; the hybrid
description below is HISTORY.** *(Original block kept as the record of what shipped between
2026-08-03 and 2026-08-16:)* *(⚠ CORRECTED 2026-08-12: this block was written
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

# MASTER SEQUENCE II — the roadmap's spine (canonical per Elias's enumeration, 2026-08-17)

*The first Master Sequence closed 2026-08-17 with every item done and the board at all-gates —
its record follows below this block, kept as it is. Every gate and date here was VERIFIED at
HEAD during transcription; the contradictions found are flagged inline rather than absorbed.*

## Step 1 — Deepen the sandbox: the coupling graduations

- **Q3 productivity → potential: ✅ DONE (`d1cb1de`)** — the re-rooting kind; byte-identical
  bar met exactly (39/39, 6/6, zero moved). Trajectory movement deferred to Q5 BY DESIGN.
- **Q1 Gini → Approval: ✅ DONE (this pass)** — the force kind, gap form per R-Q1a
  (−0.05 × (Gini − BaselineGini), 1.0 equilibrium pt/Gini pt per R-Q1b, no new ceiling per
  R-Q1c with the absence handed to step 2). ⚠ *Transcription flag: the brief's "new Country
  field snapshotted" was already false at ruling time — `BaselineGini` has existed since R4-2;
  Q1 adds NO field, and save/load is untouched-confirmed instead.*
- **Q2 real-wage → ConsumerConfidence: ✅ DONE (2026-08-18)** — R-Q2a = form A WITH the
  single-book rider (effective confidence the only confidence read or displayed; the stored
  field the policy-drift base, named as such), R-Q2b = 0.5%C/pp (band 0.25–0.75), R-Q2c =
  the shared realized-growth helper (Q5's seam). Derivation in
  `COMPLETED.md` §22 (the Q2 report, consumed 2026-08-26); the ship record with the full bar is CLAUDE.md's "Q2
  ships" entry — including **the FIFTH fixed reference** (`WageGrowthGapAtPeriodOpen`),
  found by the equivalence bar (live-gap form failed the @8%shock row at 11.8%, causally
  proven by the s=0 probe) and fixed by the established anchor pattern; and **the standing
  single-book equipment** (no stored quantity may diverge from its presented value —
  BusinessConfidence inherits by default if it ever gains an effective form).

**STEP 1 IS CLOSED (2026-08-18): three graduations, the templates demonstrated in all three
variants** — re-rooting (Q3, byte-identical), force/containment-clean (Q1, one moved field),
force/baseline-active (Q2, recalibration by construction with the off-switch control). The
queue's remainder (Q5–Q10) waits at its named triggers. **Step 2 is NEXT.**

## Step 2 — Causality legibility (the sandbox's explanatory layer)

Scoped AFTER step 1 lands — its job is explaining the web steps 1 and 5 densify. Inherits by
name: the approval no-ceiling absence (R-Q1c), the FRF/erosion/maturity decomposition story,
the coupling graph. The scoping pass produces a RULING PACKAGE — surface (trace panel? tooltip
chains? ledger annotations?), depth, and cost — before any build. Design work; Elias's ruling.

**✅ v1 SHIPPED (2026-08-18)** — R-S2a–e ruled and built: the approval ledger (terms recorded
at the boundary under the Σ==Δ self-audit; events by observation at eleven sites), the trace
panel on the LedgerRow grammar (click a chip; equilibrium framing; dated events; the
confidence single book as the second section), the preview-parity diagnostic as standing
equipment (7 exact-asserted terms × 6 countries), and the ledger in the save shape with
explicit RT assertions. The full bar — including the observation gate's three catches (the
one-ulp codegen story, the first-boundary open, the detector's own false positive) — is
CLAUDE.md's "Step 2 v1 ships" record. Deferred items carry named triggers there. ⚠ The
`s2usa*_93*` captures await Elias's eyes (built-not-confirmed).
**Step 3 (challenge-mode scoping) is UNBLOCKED and NEXT.**

## Step 3 — Challenge-mode scoping (gate: NONE — scoped-when-scoped)

Authored scenario starts with win/lose conditions, NOT an election clock (ruled). Scoping
produces the ruling package: the scenario format (seed deltas + objectives + fail states); the
first slate — **creditor start "inherit the fund" (closes R3's creditor-branch coverage gap AS
CONTENT), Italy debt start, Poland convergence run**; scoring posture; the minimum playable
slate. **FA-cadence playtesting graduates from a riding gate to real work here.**

**✅ THE SLICE SHIPPED (2026-08-18)** — R-S3a–f ruled and built: the `ScenarioDefinition` format
(the deliverable), the four objective forms, `ScenarioEvaluator` on the `CheckElection` hook, the
IMGUI verdict with margins and a legibility-powered epilogue, the ledger-style persistence (id +
counters, definition looked up by id), and the per-scenario FA cadence multiplier defaulting to
1.0. **"Inherit the Fund" closes R3's creditor-branch coverage gap BY EXERCISE — both arms of the
symmetric erosion term observed in one run** (+6.2 on a negative stock at t1, −1.4 on a positive
one at t12), and the measurement corrected the scenario's own premise (the structural deficit
dominates erosion ~7:1; the no-policy run loses 1-of-3). Full record, the bar, and the three
capture-pass defects (a capture named for a state it did not show; desk ink; copied GUIStyles
sharing GUIStyleState) are in CLAUDE.md's "Step 3's slice ships" entry. ⚠ The `s3usa*_94*`
captures await Elias's eyes. **The remaining five scenarios are content work behind the format**;
the `Sustained` objective form is built but unexercised, and the first scenario to use it is also
its first test.

**The ruling package (2026-08-18) is CONSUMED — R-S3a–f all ruled and built** (the slice above;
the package's derivation and dispositions migrated to `COMPLETED.md` §21, file deleted 2026-08-26).
**R-S3e's residue is SUPERSEDED by ruling (2026-08-26):** the three-rate FA-cadence playtest gave
way to the 08-25 decision-density ruling — the built per-scenario multiplier is the lever, and the
question that remains is the riding gate "does decision density READ as closed", not a rate sweep.

**THE TWO REMAINING SCENARIOS — live content backlog (ruled 2026-08-26: keep, build when
elected). Specs migrated from the package's §2 before deletion:**

- **Poland convergence.** *Deltas:* the seeded 3.0%/turn potential, 59% debt. *Objectives:*
  sustained real-wage growth with inflation in band N consecutive turns. *Fail:* inflation > 6%
  three turns running. **Why hard:** growth is the easy half — the tightness → wage → (Q2)
  sentiment → consumption loop plus the Phillips curve means a convergence boom overheats itself,
  and the Taylor rule answers with rates.
- **The Unequal Recovery.** *Deltas:* elevated Gini, a hostile seat composition. *Objectives:*
  Gini back to baseline without losing a confidence vote. *Fail:* approval < 30. **Why hard:**
  every lever that closes the Gini gap (welfare, minimum wage, tax) runs through Parliament, and
  each failed bill charges approval — while the Q1 Gini term is itself pushing approval down
  until it closes. This is the scenario that proves Step 2's output is load-bearing.

## Step 4 — Item 10: the political game (gate: 13 Sept 2026, Sweden votes)

Opens as ONE package: the seed-data refresh from the real result; the Italy allocator pricing
(constituency D'Hondt — the 70-seat error's fix) with the Sweden 2014 six-seat error explained
in the same pass *(branch-side claims, recorded here and VERIFIED AT STEP 4 — the stranded
branch's work is proposals to verify, never merged as-is)*; the collision-map disposition
executed (PartyArchetype retires, `emblem_*` → `mark_*`, renderers re-key,
`PartyMarkCoverageCheck`'s "PARTY SYSTEM NOT PRESENT" honest-nothing flips to real accounting);
ElectionRecord designed against the real model (R2's gate dissolves); election night (Canvas
3 of 3); **the R5 hex exchange and E2's branch accounting unblock here.**

**Riksbank independence (playtest-2 item 5, ruled 2026-08-25): option B — independence with
appointment influence — is the DESTINATION, built beside this item.** The Fed Chair mechanism is
already the generalization point (`Country.CurrentFedChair` non-null is the entire gate;
seeding Sweden a governor enables it mechanically today). **Two gates, both named:** (1) the
**output-gap distortion fix** — the recorded finding that the Taylor path's −14.5% structural
gap pins the suggested rate to the 0-floor regardless of realized inflation; an "independent"
Riksbank following that path would be model-artifact-driven, so the fix precedes the build
(~~Q5/Step-5-adjacent~~ — Q5 shipped without it; **now SCHEDULED as build-order item 4,
2026-08-26**, so B is buildable when the second gate opens); (2) **this item's own gate** —
appointment is political-game material (candidates, an appointment cadence, the reveal), so it
ships with item 10's machinery, not before. Until both open, **option C stands as the ruled
present state: the player-set rate is a deliberate gameplay choice, named as such in the
slider's own text and docs** (the Italy-scenario precedent — the premise as authored text, not
an apology), keeping Sweden/Poland's full monetary agency, which option A would have spent with
nothing gameplay-shaped in return. **⚠ Playtest pressure recorded (2026-08-26, Elias's Editor
session): C's naming does not satisfy in play — the felt verdict was "still not independent"
despite the stated-choice text. B rises in priority when item 10 opens; no build before its two
gates.**

## Step 5 — Q5: the cyclical pair (labour hoarding; investment deepening)

First water through Q3's pipe — the deferred trajectory movement arrives here, entering
potential AND wages coherently. Force kind; own baselines, one per term if separable. ⚠ *Q5
also revives the residual of Q4: once realized productivity growth ≠ trend, "do wages read
trend or realized?" becomes a live ruling — decided inside Q5's derivation.*

**✅ DONE (2026-08-18)** — R-Q5a–e ruled and built: **R-Q5a = B1** (additive cyclical force
through wages), **R-Q5b = two channels** stated separately in code though numerically
indistinguishable at h = 0.4 (the reported finding the ruling asked for), **R-Q5c** = h = 0.4
pp/pp on the unemployment gap, **R-Q5d** = R-Q3b amended (potential reads trend alone; the
Productivity stat and real wages read trend + cycle), **R-Q5e** = investment deepening deferred
(no capital stock anywhere in the model, and measured I/GDP flat at 19.5–20.9% — nothing
cyclical to deepen from either way). **The model's first closed feedback loop, and its gain was
MEASURED rather than trusted**: derivation predicted 0.075×h ≈ 0.03; the measured realized gain
(h=0 vs h=0.4 builds, real day loop, three structurally different economies) came in at
0.0297–0.0300 — agreement within 1%, stable by ~20× against Okun's own 0.7/turn reversion, and
the margin is structural (h would need to reach ~13, 32× the ruled value, before the loop could
threaten it). Full record — the link-by-link chain, the s=0 control run twice, the 14/39
byte-identical matrix with its decomposable cascade, the 500-vs-100 anomaly signature — is
CLAUDE.md's "Q5 ships" entry. **Wage Boom Management is now authorable** and is the natural next
content pass: the first exercise of Step 3's still-unexercised `Sustained` objective form, with
its difficulty a measured claim rather than a hoped-for one.

⚠ **CORRECTED, not silently — this claim was accurate as written (the loop is real) and wrong
about a SEPARATE constant nobody had measured against it yet.** The very next content pass
measured the scenario against `UnemploymentReversionSpeed` (0.7/turn, pre-existing, unrelated to
Q5) and found it forecloses the premise regardless of the loop — dominates every tested lever
including the interest rate's absolute floor. DROPPED; see `COMPLETED.md` §22 (the report,
consumed 2026-08-26)
and CLAUDE.md's "Wage Boom Management — measured and dropped" entry. The `Sustained` form WAS
exercised, on a synthetic diagnostic, and passed cleanly.

⚠ **The Disinflation was measured next, per this entry's own pointer, and ALSO DROPPED — for
the identical constant, from the opposite direction.** `UnemploymentReversionSpeed` prevents
sustaining tightness below NAIRU (Wage Boom) exactly as it prevents holding slack above NAIRU
(Disinflation): ten configurations across four countries and every tested lever, up to a 5-point
one-time hike and an auto-driven Eurozone climb to 8.6%, moved 30-turn terminal inflation by
under a point in every case. **Two drops on one root cause is now a named model-balance
finding**, not two isolated results — see `COMPLETED.md` §22 and
CLAUDE.md's matching entry.

**✅ Italy Debt Crisis SHIPPED (2026-08-18)** — measured next, on the opposite risk profile
(the debt identity, not the unemployment gap), and it SURVIVES: seven same-seed configurations
spread 52.63%–109.60% debt-to-GDP by t30 on the player's own instrument choice, spending cuts
compounding (−0.16 → −1.90 pp/pp as the cut deepens) while VAT hikes plateau (−0.43 → −0.90
pp/pp); no debt term in the misery index, so the approval-survival question that killed
Disinflation independently is cleared with margin. Authored as `ItalyDebtCrisis()`: Terminal +
**the `Sustained` form's first real exercise** (which found and fixed a genuine non-stickiness
defect in `ScenarioEvaluator`, shipped since Step 3) + NeverBreach. `GameController.cs`'s generic
verdict-margin line now reports a Sustained objective's streak. The fiscal chain's panel-section
deferral (Step 2) fires here, named rather than built — the data exists, the trigger is now live.
Full record including a root-caused, understood, driver-only capture-vs-diagnostic discrepancy
(the capture driver's shared turn clock, not a scenario or evaluator defect) is
`COMPLETED.md` §22 (the report, consumed 2026-08-26) and CLAUDE.md's "Italy Debt Crisis ships"
entry. Format
verdict: **subset, confirmed** — no new `ObjectiveKind` needed. Two of five scenarios remain:
Poland convergence, The Unequal Recovery.

**Two carry-overs from the first real playtest session (2026-08-18), recorded here as LIVE,
QUEUED, UNBUILT work — not built this pass:**

- ✅ **The fiscal legibility panel — SHIPPED 2026-08-25, the trigger closed.** ~~Step 2 named it
  as deferred, with the trigger "the first playtest asking why did the deficit move"; Italy Debt
  Crisis's own playtest session is that playtest: no panel section exists for erosion, the
  maturity rate-lag, or the debt path itself.~~ Built as `StatTracePanel`'s THIRD section on the
  Debt-to-GDP chip, on a debt ledger (`DebtAttribution`/`DebtLedgerRecorder`, the approval
  ledger's exact shape) that observes the daily stock write — so the debt step decomposes EXACTLY
  (primary balance · the FRF's revenue effect at the frozen stance · interest at issuance · the
  maturity lag · −π·b erosion · clamp/rounding · dated events), the ratio's identity in two exact
  terms, the self-audit at every boundary. Bar: 600/600 byte-identical, 0 ATTRIB across 600
  audits; RT 12/12 with the ledger crossing; parity 7/7 + ledger untouched; captures both sizes
  98/98 0/0/0, pinned on Italy mid-scenario. **Three pre-existing defects the bar surfaced, all
  fixed**: every law vote wrote approval outside the ledger since 08-24 (24 → 0 ATTRIB on the RT
  harness); the trace panel never measured against its host's height; the driver's Italy block
  had drifted past `EndTurn` and its approval trace had been silently absent since Step 3. Record:
  CLAUDE.md "Step 2's third section ships". ⚠ The `fiscal1600s4_*`/`fiscal2560s_*` `93c`/`95d`
  captures await Elias's eyes (riding gates, Access).
- ⚠ **The `EndTurn`-as-absolute-turn-number capture artifact is not new to Italy.** Confirmed by
  looking: `94c_scenario_verdict` ("Inherit the Fund") has shown the SAME shape since Step 3
  shipped it — "SCENARIO FAILED" reached exactly at its own mid-run start, not a fresh-game one.
  There it was by design (that capture block applies zero player policy on purpose); Italy's own
  instance of the same mechanism was investigated fresh and found to be the identical root cause
  (`ScenarioEvaluator` compares `EndTurn` absolutely; the capture driver runs several blocks on one
  continuous clock before either scenario starts). **Both known, both driver-only, neither a
  scenario-balance or evaluator defect** — not queued as work, recorded so a future reader does not
  re-discover the same thing as if it were new.

**STEP 5 CLOSES.** Master Sequence II's explicitly-named items (Q1–Q5, Step 1–3) are all
shipped or scoped; the spine's remainder is Step 4 (13 Sept) and Step 6 behind it, plus scenario
content behind Step 3's format, plus the queue's own remainder (Q6–Q10) at its own triggers.

**SPINE RE-CHECKED 2026-08-25 — UNCHANGED.** Steps 1, 2, 3 and 5 done; Step 4 gated on 13 Sept
2026 (~2.5 weeks out at the check); Step 6 behind it. The fortnight since 08-17 — the first
playtest, Turn → Year, the Calendar Panel, the entire law system to 50/50, the two-copy
consolidation — touched none of the four things Step 4's package names (seed refresh, the Italy
allocator, the collision map, election night) and nothing Step 6 waits on. The one addition that
COULD have collided — laws entering Parliament — reuses the existing gated-bill path verbatim and
adds no political-system code, exactly as Playtest 1's law-system scoping predicted (§5 of the
package, consumed to `COMPLETED.md` §21). Step 4's
date rides regardless. *(Sources: `git log --since=2026-08-24`; CLAUDE.md 10615–11620; this file's
own Step 4 text, re-read.)*

## Step 6 — Story mode (gate: item 10 shipped)

Scoped fresh on the political layer: authored multi-beat arcs with memory on the
minister/interrupt/ceremony skeleton. Nothing pre-scoped now beyond the gate.

## Riding gates (close when the world moves) — verified at HEAD 2026-08-17

| gate | status at transcription |
|---|---|
| **~23 Aug** — GitHub GC re-check | ✅ **CLOSED 2026-08-25** — `curl https://api.github.com/repos/Bippen/PoliSim` → `"size": 9221` KB (~9.0 MiB), down from the ~746 MiB read on 08-16. GitHub's own maintenance ran; no support ticket needed |
| **Delivery** — D1's portraits | ✅ **GATE CLEARED 2026-08-26** — the Editor register side-by-side PASSED (the painted plate belongs beside the existing register). Design may proceed with the batch of nine per the approved §5 PoC; the verdict travels in the next send package. Now waiting on Design's delivery, import checks standing |
| **Delivery** — E3's rasterizer | the gate is the sharpened form: a raster path whose OUTPUT is comparable (the module tessellates; `RenderSpriteToTexture2D` renders blank — probed); `StripCutDiffCheck`'s compare machinery finished and waiting |
| **Access** — the Editor checklist | ✅ **THE THREE GAP ENTRIES CLOSED 2026-08-26** (Elias's live session): folder tongues VERIFIED (hover face, spine shift, real click on the deferred-painted tab); save/load layer 3 + F5/F9 + saves menu VERIFIED; portrait register side-by-side PASSED — **D1's batch of nine unblocked**. Remaining in this row: the capture-set reviews only. **PLUS the capture-set reviews awaiting Elias's eyes (built-not-confirmed, rule 15's third layer)** — Step 2's `s2usa*_93*`, Step 3's `s3usa*_94*`, and *added 2026-08-25* **the shipped law browser — review target UPDATED 2026-08-26 to the `board1jc1600/2560_06f*` sets + `06g`** (Board 1j + the Expected-effects band; the earlier `panewidth*` sets are superseded by the 1j rebuild — 0 guard violations, but composition — does it READ — is the eye's question alone), and *added 2026-08-25* **the fiscal trace** (`fiscal1600s4_93c_trace_debt` / `_95d_italydebt_trace_debt` and the `fiscal2560s_*` pair — the debt section on the USA's warm-up period and on Italy mid-scenario; note the approval trace's own `93_trace_approval` now ends at the tab rather than the screen edge under the panel's new host-height cap — the diff against `s2usa*_93*`, stated) |
| **Play** — FA cadence (~3.65/turn, intended-or-inherited) | graduated: the per-scenario multiplier shipped with Step 3 (R-S3e); the three-rate sweep SUPERSEDED by ruling 2026-08-26 (C5) — the felt-pacing question now rides the decision-density gate below |
| **Play** — the next playtest: **does decision density READ as closed?** | Its own item, by name (ruled 2026-08-25, Elias). Fifty enactable laws answered the MEASURABLE half of the 08-18 gap (choices 19 → 69; prompts unchanged by construction); whether a player FEELS the gap closed is a playtester's question, not a constant's, and no measurement this side of a playtest can answer it. Closes when a playtester says so either way |
| **Play** — the creditor scenario | ✅ CLOSED 2026-08-18 — "Inherit the Fund" shipped as Step 3's slice; both arms of the symmetric erosion term observed in one run. Row closed 2026-08-26 |
| **Rulings (Elias)** — the queue remainder | Q6–Q10 at their named triggers; **✅ Q4 RESOLVED-BY-R-Q5d (confirmed 2026-08-26, C6)** — Q5 split trend from cycle and decided the residual there, closing the "moot until" condition; ~~A3 at its trigger~~ struck 2026-08-26 (C1 — A3 was RESOLVED 2026-08-02, the re-listing a two-authors artifact; `COMPLETED.md` §23); F2 stands recorded |

*The closed first sequence follows, kept as the record it is.*

This is the one authoritative order, replacing whatever each original document separately suggested. It exists because Political Systems Overhaul Part B depends on Continuous Time Phase 0, and because building new Roadmap features or converting existing systems to daily granularity while Parliament's gating is mid-rollout would mean touching the same code for two different reasons at once — exactly the kind of overlap this project's discipline exists to avoid.

1. **Part A (Cabinet). DONE** — see `COMPLETED.md`. *Limitation: 3 of 6 portfolios implemented.*
2. **Part C (UI/graph restyling). DONE** — see `COMPLETED.md`.
3. **Continuous Time Phase 0. DONE** — see `COMPLETED.md`. Calendar/UI only; changed no economic math.
4. **Part B, PILOT (Tax Policy tab). DONE** — see `COMPLETED.md`.
5. **Part B, full rollout (5a–5f). DONE 2026-08-02** — see `COMPLETED.md` sections 10 and 16. 5e's Phase C
   batches 4–6 were the last live part, and Elias's review confirmed them. Scope absorbed 5f.
6. **Resume Roadmap work (Round 4)** — ✅ **DONE 2026-08-17. THE ARC IS CLOSED: five batches, all
   shipped** (R4-1 C3 youth-U + life expectancy · R4-2 C2 Gini + real wages · R4-3 C1 housing ·
   R4-4 the three cabinet portfolios · R4-5 C5 productivity). See `COMPLETED.md` §19 for the arc
   summary and CLAUDE.md's five "Round 4 batch" records — R4-5's carries the arc verdict, the
   consolidated WRITE-BACK RULING QUEUE, and the post-Round-4 board. The scoped plan, rulings
   package and batch table that lived here moved to COMPLETED §19 per the standing finished-items
   pattern. Still OUT of the closed arc, by its own scoping: C4/credit-rating follow-ons
   (sequenced behind the stock-vs-flow mechanism report, §F1 chain) and item 10's
   collision-mapped territory (gated 13 Sept).

   *(The scoped plan, batch table, stacking posture and rulings package that lived here moved to
   `COMPLETED.md` §19 on close-out, 2026-08-17. The five CLAUDE.md batch records remain the
   detailed authority.)*
7. **Continuous Time Migration — Phases 1 through 5** (the actual daily-granularity conversion of each system's math, safest-first, core macro engine last). This is deliberately positioned after the political-systems work — it's a separate concern (simulation granularity, not who can change policy) and touching the same files for two unrelated reasons in the same window is worth avoiding.
8. **Save/load — ✅ SCOPED, RULED, BUILT, GATE-GREEN AND LIVE-VERIFIED.** The full design record —
   the four `JsonUtility` failure classes verified against real code, the two Elias rulings
   (serializer: Newtonsoft `com.unity.nuget.newtonsoft-json`; scope: ALL THREE LAYERS — core sim
   state, pending bills/interrupts with their counters, UI draft values), the two save-blocking
   gaps (`SimulationRandom`'s counting-shim ruling; `PeriodClosingValues`' ValueTuple key) and the
   `UAC1001` demonstration — migrated to `COMPLETED.md` §26 (2026-08-26). Build chain: mechanism
   report 2026-08-16 (CLAUDE.md "Save/load mechanism report"; version policy RULED A — refuse-load
   with a plain message, `SaveVersion` bump on model swaps, no migration machinery pre-release) →
   core built gate-green the same day (F5/F9, `SaveLoadRoundTripDiagnostic` 12/12; CLAUDE.md
   "Save/load BUILT and gate-green") → the saves menu the same day (79 captures × both sizes;
   CLAUDE.md "The saves menu") → **the layer-3 live checklist VERIFIED 2026-08-26** (F5/F9 + the
   saves menu in Elias's Editor session). Nothing of item 8 remains open.

9. **NEW (2026-08-01) — Macro Data & Release Calendar Overhaul (Steps A–D).** Full spec was
   `POLISIM_MACRO_OVERHAUL_DIRECTIVE.md` — **every step done; the directive consumed to
   `COMPLETED.md` §25 and deleted 2026-08-26** (A/B/D in §§6/9, Step C as Round 4 in §19); every
   real-world figure it depended on is in `POLISIM_SEED_DATA_MACRO_OVERHAUL.md`. Appended rather
   than renumbering 1-8, which are referenced throughout this document and `CLAUDE.md`.

   **The four-step split's own reasoning** (make failure ATTRIBUTABLE — Step A proved the
   machinery inert with zero new variables before Step C added stats on top) is recorded with the
   consumed directive in `COMPLETED.md` §25; the step records are §§6/9 (A, B, D) and §19 (Step C
   as Round 4's five batches). Elias's D-first sequencing change and the A4 caller-check lesson
   ("check callers before believing a feature exists") live in §6 and this file's own
   consolidation rules.

   **THE CRITICAL CORRECTNESS RISK — still binding on everything downstream**: the player-facing
   UI reads the PUBLISHED (lagged, possibly-revised) series; every internal system — Okun's Law,
   the Phillips Curve, the Fiscal Reaction Function, sector integration — must keep reading LIVE
   values. A leak makes the model consume its own stale output, and the effect may not appear for
   hundreds of turns. The one-directional rule (`PublicationSystem` writes `Country.Published`,
   reads `Country.State`, never the reverse) is the enforcement, checked across 55 call sites.

   **`[GAP]` figures are Elias's to source, never to invent** — still binding for all future seed
   work; the seed doc's variant-axis warnings govern any re-sourcing (`MISSING_PREREQUISITES.md`
   §B carries the three surviving quality debts).

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
   | — | *The FRF sweep* — ✅ **RUN AND CLOSED EMPTY 2026-08-16 (`afe0f24`)**, in the daily regime as sequenced. Seven-point grid (S ∈ {1.5, 2.5, 4, 6, 10} at [0.5,1.5]; the [0.8,1.5] floor arm; the [0.5,1.25] wall-direction probe), two seeds, six countries, real Unity, every point measurably reached. **No pair converges within the revenue-capacity wall**: the current pair is defensible at the ruled 100–200 window (no saturation, near-flat slopes); S=2.5–4 strictly mildews the deep divergence at no in-window cost but the climb never equilibrates; S≥6 oscillates; the loosened floor spirals five countries to −300…−1010%; the tighter cap is near-inert in-window. Values untouched. The harness that fitted the pair no longer exists on disk — its four-sig-fig stability claim at this exact pair stands as the one recorded tool-disagreement. **Next fiscal-engine item: the STOCK-VS-FLOW MECHANISM REPORT, its own pass with its own ruling** | closed — the mechanism report is Elias's to schedule |
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

   ~~Not blocking, but must be settled before art is commissioned: the three modals that render in
   TWO places via `drawOwnFrame` each need both a framed standalone and an unframed embedded
   treatment.~~ ✅ **ABSORBED (confirmed 2026-08-26, ruling C2)** — the `ui_frame_ornate`
   Canvas-path ruling (2026-08-12) means the IMGUI modals' framed treatment dies with the Canvas
   rebuild, and the dual-siting answer (frame/title/plate as separate sprites the embedded path
   skips) was delivered in pass 2 (`COMPLETED.md` §24).

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

Migrating from turn-based to true daily-granularity continuous time with Pause/1x/2x/3x speed
controls. Nearly every tuned constant in the game is implicitly calibrated against a full-turn step.
This is the largest single risk in the project's history — do not attempt it as one pass.
*(⚠ CORRECTED 2026-08-16: this section was written when a turn was 121 days and still said so;
since `d8f55ce` a turn is `SimulationManager.DaysPerTurn` = **365** days, and every conversion since
Phase 1 derives from that constant rather than typing a number — the correction below follows the
same rule.)*

## The translation methodology — do not guess new constants

Identify which mathematical shape a constant is before touching it (denominators are
`DaysPerTurn`, never a typed number — 365 today, and a future turn-length change must land in one
constant, which is the discipline that made 121→365 a one-line edit):

1. **Linear/additive rates**: `rate_per_day = rate_per_turn / DaysPerTurn`
2. **Multiplicative/compounding rates**: `rate_per_day = (1 + rate_per_turn)^(1/DaysPerTurn) − 1`
   (`MacroSystem.PerDayReversion` is the standing implementation for reverting quantities)
3. **Probabilities**: `p_per_day = 1 − (1 − p_per_turn)^(1/DaysPerTurn)`
4. **Hard clamps/ceilings do NOT shrink** — a ceiling bounds the state itself, not a per-step
   increment. Only the *speed of approach* changes (via #1 or #2 above). Treating a ceiling as
   something to also divide by the day count is a likely first-attempt bug.
5. *(Added by Phases 1–4 in practice:)* **Sensitivities and target-shapers take NO transform** — a
   constant that maps a LEVEL to an offset, or shapes a reversion TARGET, has no time dimension;
   scaling it changes what a policy position *means*. **Annual-rates-applied-to-stocks take the
   POWER slice** (`(1+x)^(1/DaysPerTurn)`, exact at constant rate — Phase 4's population factor).
   And per Phase 3: **a constant that is a POLICY STANCE stays frozen for its period.** Ask the
   stance-vs-flow question of every constant before assigning any shape.

## The validation bar: aggregation-equivalence

Before any system's daily version is trusted: simulate `DaysPerTurn` consecutive days and confirm
the result is within ±3-5% of what the existing, already-validated single turn-level step produces
for the same inputs. This is the ground truth every phase below must pass before moving to the next.

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

> ✅ **ALL FIVE PHASES ARE DONE — MASTER SEQUENCE ITEM 7 CLOSED 2026-08-16** (`22e2b49`, Phase 5's
> own record below). **The hybrid simulation is over**: the daily calendar now carries daily
> sectors, infrastructure, labor, crime, fiscal flows, demographics AND the core macro engine —
> every economic quantity moves on the day its history point records. The turn boundary remains
> what a boundary is for: resolutions, plans, stances, elections and events. The per-day constants
> all derive from `DaysPerTurn`; the four-fixed-reference anchor set (plan G, FRF stance, Okun's
> references, the identity's attractor) is the period's frozen plan, re-adopted at each boundary.

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
- **Phase 4: Demographics. ✅ DONE 2026-08-16 (`37c9003`).** The throwaway diagnostic ran FIRST and
  earned its gate (9/9 — the two turn-length statements agree exactly at 1.0; the measured population
  step applies exactly one year per turn; its one first-run FAIL was the probe's own float tolerance,
  a probe defect). Aggregation-equivalence **61/61 on the first conversion** (demographics max drift
  0.0042%); full trajectory matrix vs the untainted `pre4_18fe08d` baseline (2 seeds × 100/500/1000
  turns, all six countries, every EconomyState field): LFPR ≤0.0033%, `PotentialGrowthRate`
  byte-identical, GDP below every top-14, no macro leak — every large relative spike traced to its
  raw values and confirmed a zero-crossing/clamp-boundary metric artifact. **Two additions to the
  shape taxonomy** (now in the translation table above): the annual-rate POWER SLICE, and
  target-shapers/sensitivities taking no transform. **One finding**: `History.Append` had sat on the
  turn boundary since Phase 0 — the multi-resolution buckets had never received a daily offer; moved
  to `AdvanceDay`, with the bucket-divergence assert now standing in `AggregationEquivalenceCheck`.
  Full record in `CLAUDE.md` "Continuous Time Phase 4".
- **Phase 5: The core macro engine. ✅ DONE 2026-08-16 (`22e2b49`) — and with it ITEM 7 CLOSES.**
  The no-feedback diagnostic gated first (4/4: PotentialGDP independence, published-corruption
  immunity asserted dynamically, the interest chain's indirection pinned both directions). The
  conversion's story is the phase: **four first-try shapes failed the bar, every failure measured
  and kept**, and all four resolved into ONE pattern — Phase 3's **fixed period reference** (Okun's
  reversion against period-open unemployment; the growth increment against period-open GDP; the
  identity's attractor at period-open PotentialGDP via the new AFFINE POWER SLICE; and inflation
  expectations staying AT the boundary outright, because "adapt to the closing print" is a boundary
  semantics with no faithful daily form). No constant VALUE changed — the FRF pair untouched per
  the up-front ruling. Equivalence **81/81 near-exact** (GDP 0.0001%, unemployment 0.006% at an 8%
  drive); matrix vs `pre5_0034eff` at both seeds × 100/500/1000 with both diff columns: 13 of 30
  fields byte-identical, bounded mean-reverting transients, **the four-country debt signature
  REPRODUCED within ~1 point** (USA 155.9, Germany 80.1, Italy 165.9, Poland 46.0; Sweden/France
  settled shapes intact) — not fixed, not worsened, per the pass condition. Save/load round trip
  re-proven 12/12 for `FiscalPeriod`'s three new anchors. **The hybrid simulation is over: every
  economic quantity moves on the day its history point records.** Full record in `CLAUDE.md`.

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

- ✅ **The net-creditor bound ruling (2026-08-02) — FIX THE CAUSE, keep a non-binding guard — is
  CLOSED, VINDICATED AND MIGRATED (2026-08-26).** Both candidate fixes shipped in `0386e83` (SWF
  returns inside the multiplier; returns treated as a stock with the 3%/yr structural draw — which
  also closed a double-count); the −300% symmetric bound retired; `DebtClampDiagnostic` measured
  the guard NEVER ENGAGING (0/120 negative turns, all six countries) while pre-fix Sweden/France
  sat pinned at −296…−299 — *"a ruling made on reasoning, confirmed by measurement nine days
  later."* The Italy "+7.0" question closed with the 1000-turn supersession (never attributable to
  the SWF change). Full progression: `COMPLETED.md` §§13/18/23, CLAUDE.md's fiscal-arc entries,
  and git history for the struck-through stages this entry used to carry.

- ✅ **The unbounded-divergence block (C4 superseded 2026-08-11 → diagnosis → sweep-empty) —
  CLOSED 2026-08-17 AND MIGRATED (2026-08-26).** The chain — the 1000-turn supersession ("every
  equilibrium quoted since 2026-07-22 was a waypoint"; `0386e83` innocent for all four climbers
  AND vindicated on the pinning it un-pinned), the saturation diagnosis (two feedbacks
  asymmetrically bounded; interest compounding the driver; stages, not alternatives), the FRF
  sweep run empty (`afe0f24`) — graduated to a measured mechanism limit and was ANSWERED by the
  stock-vs-flow report's shipped terms (erosion, maturity, F1 — `COMPLETED.md` §§22/23). **Two
  scoping rulings from this block STILL BIND: calibration stays at turns 100–200** (t1000 is a
  diagnostic, never a target — judge a fix by whether the mechanism is present and correctly
  signed, never by t1000 convergence), **and the word "equilibrium" stays banned without a run
  that earns it.** Full progression in CLAUDE.md's fiscal-arc entries and git history.

- ✅ **The deficit-term defect (2026-08-02) — CLOSED with F1 (2026-08-17), struck 2026-08-26.**
  The residual the floor was hiding traced to the divergence; the erosion/maturity terms and the
  anchor re-run at HEAD closed it — C4 and A1 closed with it (`COMPLETED.md` §23).

### RESOLVED 2026-08-02 — all three section A decisions

Full reasoning migrated whole to `COMPLETED.md` §23 (2026-08-26), kept in full deliberately so none is reopened later.

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
  `COMPLETED.md` §23 (the F register: closed 2026-08-17, migrated 2026-08-26).
- **SWF emergency drawdown — standalone tier-3 bill**, reusing 5d's mechanism. Not bundled into the
  annual budget, not fully exempt like the Fed/Eurozone carve-out. **Still to build — see below.**
- **Cabinet appointments stay unilateral.** Parliament gates *policy*; appointments are *executive*, and
  reshuffling already carries an approval cost. **No code change — this confirms current behaviour.**

*Five 2026-08-01 resolutions and two 2026-07-31 ones moved to `COMPLETED.md` section 11.*

### Live work from the 2026-08-02 visual review

- ✅ **P2 — the currency unit bug (review item 3): BUILT 2026-08-02 (`628d78e`) AND SEEN — struck
  2026-08-26.** Item 3 PASSED on re-review the same day (`COMPLETED.md` §16), which discharged the
  "NOT YET SEEN" proviso this entry stayed live for; the entry simply never closed with it.
  `UiFormat.Money`'s required unit parameter and `MoneyFormatDiagnostic` (6/6) stand as the
  permanent fix.

- 🔴 **P4 — the label-clipping CLASS: OPEN as a watch item (no single gate; entry compressed
  2026-08-26).** The 2026-08-02 entry's recommendation — one `PoliSimWidgets.MeasuredLabel` helper
  (measure in the rendering style, shrink never truncate, leave margin) — was IMPLEMENTED and the
  known sites swept; the class has kept producing instances on NEW AXES since: #12 the frame
  itself; #13 the ECB sub-tab, the first reached through the COUNTRY axis; and the 2026-08-26
  **width-less-label class** (`CalcSize` ignores wordWrap without an explicit width — six
  instances, fixed under the minimum-window ruling, CLAUDE.md). The sibling survey
  (constant-sized chrome under wrappable labels) is named-not-fixed in CLAUDE.md. The class
  closes only by a capture-matrix pass at all supported sizes showing no new instance; rule 15's
  paired-detector correction is its standing discipline. The entries below record the instance
  history.

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

- ⚠ **CAPTURE RESOLUTION NOT COVERED — 1920×1080 (entry rewritten 2026-08-26).** The
  deliberately-narrow-window half of this entry DISCHARGED into the 2026-08-26 minimum-window
  ruling: **1280×720 is the ruled minimum supported size** and the capture matrix now runs FOUR
  sizes (1280×720 / 1640×707 / 1600×900 / 2560×1440 — the free-aspect overflow class that
  motivated it is fixed and on film). **1920×1080 — the single most common desktop resolution —
  remains the one uncovered size**: startable today, minor, a command-line argument rather than a
  code change (`-shotwidth=` / `-shotheight=`).

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
  bill, so identical counts prove it inert until used. ~~Needs a visual look~~ — ✅ satisfied:
  pinned on film in the state-axes pass (a pending bill of every introducible type, 7/7 on Sweden
  — its seeded standing fund is this bill's real precondition); struck 2026-08-26 (ruling C3).

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
| ~~`POLISIM_MACRO_OVERHAUL_DIRECTIVE.md`~~ | Step 9's spec. **DELETED 2026-08-26** — every step (A–D) done, so it shrank to nothing and went, exactly as the rule prescribes; pointers in `COMPLETED.md` §§6/19/25 | — |
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
