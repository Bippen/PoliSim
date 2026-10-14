# PoliSim — the gameplay audit: the protocol

**Ruled by Elias, 2026-10-13** (recorded in `COMPLETED.md` §803). This is INSTRUCTION: the brief as he gave it, saved so the audit can be run again. Below the line is his text, kept as given; a run of the audit reads it from here. What a run found goes in `docs/audit/AUDIT_LEDGER.md` (one row per finding) and `docs/audit/AUDIT_FOR_ARCHITECT.md` (the S1/S2 list and the questions marked ARCHITECT); the variant matrix and the run log of each run are written beside them.

PS-6 is PARKED while the audit runs (Elias's ruling H5, 2026-10-13: after the audit, US-14, then US-32; the rulings H1-H5 are in `docs/specs/USA_STAGE_PLAN.md`).

---

GAMEPLAY AUDIT. Park PS-6 until the audit is done. Save this brief as docs/audit/AUDIT_PROTOCOL.md so it can be run again.
Goal: play every gameplay feature in every variant, find bugs and discrepancies, produce one fix list, and ask Elias what to do as you go. Audit first: no gameplay code changes before Phase 4.

## PHASE 0: INVENTORY (both directions)

- From the code, list every player-facing feature and system, then check the list against POLISIM_FEATURE_LIST.md, COMPLETED.md and the specs.
- Features that are specified but missing, and features that are present but unspecified, both become ledger rows.
- Mark each country, start point and role as startable or not built. "Not built" is not a bug.
- Post the variant matrix and a run-time estimate, then carry on without waiting.

## PHASE 1: HARNESS

- Run through BatchSimulationRunner, using the same entry points the UI calls (no private shortcuts).
- Play styles:
  - idle: no input;
  - historical: follows the record;
  - fuzz: random legal actions;
  - exploit-seeker: maximises approval or money by any legal means;
  - extreme: extreme policy.
- Seed and log every run. The same seed run twice must give an identical trajectory.
- Commit only harness and ledger files, each on a green bar. Keep ProjectAuditorSettings.asset out of every commit.

## PHASE 2: MATRIX

Variants, with pairwise coverage across:
- country: every startable one;
- start: every start point and the three challenge scenarios; [the scenarios were retired by Elias's ruling - GA-0-14, GA-0-40, §825]
- role: head of government, coalition partner, opposition, created party;
- outcome: win, loss, hung or coalition, no-confidence or early election, term limit, game over;
- play style: the five above;
- length: to the first election, two cycles, 30 years;
- interruptions: save/load at random ticks, Pause/1×/2×/3×.

Run the full cross on country × role × outcome.

Areas, foundations first:
1. Save/load and determinism. Report any difference between 1× and 3× on the same seed, and any difference between save→load→continue and an uninterrupted run.
2. Money paths: budget acts, tax, pensions, benefits, the fund, debt and fiscal rules, vetoes.
3. Elections: every system, thresholds, seat allocation, presidential rounds, election night.
4. Office: cabinet formation, confidence votes, early elections, caretaker government, term limits, opposition play, losing and regaining power, game over.
5. Campaigning: actions, funds, polls, events, approval.
6. Energy: fleet, load blocks, zones and area prices, ETS, both ledgers, carbon-rate indexation.
7. Macro and the six society-stat families.
8. Cabinet ministers, parliamentary gating, statutes.
9. Events and the challenge scenarios. [The scenarios were retired - GA-0-14, GA-0-40, §825.]
10. Every UI state on dry film against its LOOKED AT board: clipping, number format, colour neutrality, stale or wrong-country text.
11. 30-year stability and tick time.
12. Exploit sweep.

Invariants on every run:
- no exception, NaN or Inf;
- stock-flow identities hold (debt change = deficit);
- every stored value equals its presented value;
- seat totals equal the chamber size;
- vote shares sum to 100;
- election dates are legal;
- a coalition holds a majority or is a declared minority;
- energy supply = demand + losses.

## PHASE 3: LEDGER

docs/audit/AUDIT_LEDGER.md, one row per finding, with these columns:
- ID and area;
- variant: country, start, role, seed, tick;
- type: bug, spec, record, board, exploit or unspecified;
- severity;
- expected, plus its authority (§, ruling, board or source);
- actual, plus evidence (run id, log line, capture);
- repro;
- proposed fix;
- decision needed: none, Elias, or architect.

Severity:
- S1: crash, softlock, save corruption, NaN.
- S2: a wrong number on a money path, or a wrong election or office result.
- S3: a discrepancy a player would notice.
- S4: text and polish.

Evidence rules:
- No finding rests on a shell probe alone (R-T2).
- Reproduce every S1 and S2 finding from its seed twice.
- A visual finding stays "film-observed" until Elias sees it in the Editor.

## QUESTIONS AS YOU GO

- At the end of each area, post a report of at most 20 lines (counts by severity and the top five findings), then call AskUserQuestion with up to four questions, recommended option first.
- Put the clear bugs in one question: "Fix all N clear bugs?" A clear bug is code that contradicts its own spec with no constant or model change.
- Mark modelling, sourcing and ruling questions ARCHITECT and collect them in docs/audit/AUDIT_FOR_ARCHITECT.md: at most 100 lines, the S1/S2 list plus those questions. Don't ask Elias them.
- An unanswered question parks only its own item; the audit carries on (queue discipline).

## PHASE 4: FIXES (only rows Elias marked "fix", S1 first)

- One finding or one tight group per commit; one green bar per commit; R-SP1.
- Any simulation change gets a full trajectory diff. Declare any sentinel move in the commit.
- Never tune a constant to make a finding pass. A constant change is a question.
- Re-run the finding's repro seed and its area's runs, then close the row with the commit hash.
- Add a bar check only under R-N5. Otherwise the repro lives in the ledger.
- Reviews as usual: money paths and BASELINE families.

Parallel agents are allowed for read-only work (reading code against spec, analysing logs). Runs go through one BatchSimulationRunner queue, because Unity locks the project.
