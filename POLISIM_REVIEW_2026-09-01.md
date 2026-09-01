# PoliSim — Review: detach the documents, speed the process (2026-09-01)

**A review, not a build.** F2 was not started. No check was written (R-N5). No document was rewritten
ahead of its ruling. The surgical changes actually made are in §C, each with why it is safe.

⚠ **Every count here was measured in the run that reports it, and says how.** Where a figure could not be
measured, it says so rather than estimating. Two premises in the brief were checked before use; one is
wrong (§A.0).

⚠ **R-T2 was respected for four findings and BROKEN for a fifth, which has been retracted** — see the
retraction at the head of §A.1. A shell probe carried a finding alone, the probe was malformed, and the
finding was false. **The retraction is left standing rather than deleted**, because a review auditing
stale claims may not quietly drop its own.

⚠ **This review's own process failed once, in the class it audits.** Writing this file by shell heredoc
died on `unexpected EOF while looking for matching quote`. It was rewritten as a single self-contained
file write and succeeded. That is the §B.4 rule earning itself inside the review that proposes it.

---

## §A.0 — Two premises in the brief, measured before use

| the brief says | measured | verdict |
|---|---|---|
| "the current 25 checks" | `CheckSuite.Suite` holds **25**; `CheckSuite.Simulation` holds **9** | ✅ **correct** |
| "three instances now, four Unity runs lost" | the records support **at least nine** Unity runs lost to invocation errors, plus two capture families re-filmed | ⚠ **understated ~3×** (§B.3) |

*How the check counts were measured:* the `Suite` array (`CheckSuite.cs:153–242`) and the `Simulation`
array (`:320–359`), counted by name. ⚠ **The obvious one-liner undercounts `Suite` to 24**, because
`ChromeV2CoverageCheck` contains a digit and `[A-Za-z]*` drops it; the four `*Diagnostic` members of the
simulation group are missed the same way. **A count is the easiest thing in this project to get wrong,
which is the review's whole subject.**

### The first finding is in the code, not the documents

`Assets/Editor/CheckSuite.cs:131` — the comment whose declared job is to enumerate that list — opens:

> *"the checks named in `Suite` (**TWENTY-ONE** since the NINTH sweep …)"*

**`Suite` holds 25.** ⚠ **No existing check can catch this, and per R-N5 none should be written for it.**
`PhantomGuardCheck` and `CommentClaimCheck` verify that a *name* in a comment resolves; every name here
resolves. **The stale thing is the number, and nothing in this project counts.**

This inverts the brief's framing usefully. The brief assumes documents go stale against code. Here the
**code comment** went stale while `POLISIM_FEATURE_LIST.md`'s "25 checks … and the 9 simulation checks" is
**correct**. **Coupling is not a property of markdown. It is a property of transcribed numbers, wherever
they live** — and that is why the convention in §A.5 binds comments as well as documents.

---

## §A.1 — Five demonstrated stalenesses, each confirmed against the code

> ⚠ **RETRACTION, 2026-09-01, same session.** This section originally carried a SIXTH finding — that
> `ERRANDS.md` E-3's *"the working copy is LF"* was false and the tree was CRLF. **It was the review that
> was wrong, and the document that was right.** The probe behind it, `grep -c $'\r' <file>`, returned a
> count equal to the file's TOTAL line count for **every** file tested — the signature of an empty pattern
> matching every line, not of carriage returns. Re-measured with `tr -cd '\r' | wc -c` and `od -c`, **every
> file in the tree is LF**, and `CLAUDE_DESIGN_BOARD_1I_NOTE.md` hashes to the exact digest
> `SEND_PACKAGE.md` publishes for it — which is the independent confirmation the retracted finding never
> had.
>
> ⚠ **The corroborating fact was real and the inference from it was not.** `core.autocrlf=true` is
> genuinely set; it governs what a FRESH CHECKOUT would produce, not what this working copy contains.
>
> **This is R-T2 being proved by breaking it** — *a shell probe is the weakest evidence and may not carry a
> finding alone* — inside the review that quotes R-T2 as its own standard. The retraction is left in place
> rather than deleted, because a review that audits stale claims may not quietly drop its own.

| # | where | the claim | what is true | how confirmed |
|---|---|---|---|---|
| 1 | `Assets/Editor/CheckSuite.cs:131` | "TWENTY-ONE" checks | **25** | counted the `Suite` array |
| 2 | `POLISIM_BACKLOG.md:1031` | `MomentumTracker.Apply` at "`CampaignRun.cs:341` and `:451`" | lines **377** and **487** | `grep -n "momentum.Apply" Assets/Scripts/Elections/CampaignRun.cs` |
| 3 | `POLISIM_BACKLOG.md:1020` | "254 `.cs` files under `Assets/`" | **293** | `find Assets -name "*.cs" \| wc -l` |
| 4 | `POLISIM_MASTER_ROADMAP.md:495` | "`ls *.md` matches this table exactly: TWENTY files … no orphans" | root holds **21**; the orphan is **`POLISIM_FEATURE_LIST.md`**, which appears **0 times** in that document | `ls *.md \| wc -l`; `grep -c FEATURE_LIST` → 0 |
| 5 | `POLISIM_V2_SCREEN_SPEC.md:14`, `:776` | "the one screen not yet built, **1h election night**" | built — `Assets/Scripts/UI/ElectionNightScreen.cs` exists; HEAD `94c0b9c` is *"F1 COMPLETE: board 1h filmed from the MODEL"* | `ls` + `git log -1` |
| ~~6~~ | ~~`ERRANDS.md:18`~~ | ~~*"hash it AS LF"*~~ | ⚠ **RETRACTED — the document was correct; every file is LF** | `tr -cd '\r' \| wc -c` → 0 on every file; the published digest reproduces |

### Finding 4 is the most structurally revealing

The roadmap's document-set table exists *specifically* to prove nothing dangles. It is now **behind the
root it enumerates, and the file it is missing is the file that now governs the project.** The mechanism
is plain: `POLISIM_FEATURE_LIST.md` was created by `1340738`; the roadmap has not been touched since
`1985842`, which is earlier. **A completeness proof is worthless the moment it is not regenerated, and it
fails silently** — nothing went red.

### What the retracted finding 6 still teaches, and it is not what it claimed

The claim was wrong. **The class it pointed at is real, and the review demonstrated it by falling into it.**

E-3 is an **outward-facing errand**: Elias pastes a package and hashes the readback. Its line-ending
sentence has been rewritten by hand at least once already — `MISSING_PREREQUISITES.md:18` records the
earlier inversion (*"that warning would have made a CORRECT readback look wrong"*) and repaired it **by
flipping the assertion rather than deriving it.** ⚠ **The instruction is currently TRUE. Nothing in it
computes anything, so nothing keeps it true**, and the next person to reason about `core.autocrlf` — as
this review did — can flip a correct instruction into a wrong one with an argument that sounds right.

⚠ **An assertion about the environment that is not computed from the environment is the defect, whether it
currently reads true or false.** That is the whole finding, and it survives the retraction intact.

---

## §A.2 — The claim census: three counts per document

**Instrument:** `Tools/claim_census.sh`, added by this review. ⚠ **It is not a check** — it never fails,
never gates a bar, R-N5 does not apply. It reports. Re-run and every figure below is re-derived:

```
bash Tools/claim_census.sh              # the table
bash Tools/claim_census.sh --per-marker # the marker breakdown
```

**LIVE = every root `*.md` except `COMPLETED.md`, `CLAUDE.md`, `ELECTIONS_PROTOTYPE_LOG.md`** — the same
ruling `DocumentClaimCheck.cs` makes in its `Historical` set, adopted rather than reinvented.

**UNIT, stated because it governs every number here:** the *claim-line* — a non-blank line outside a
fenced code block that is not a pure separator. ⚠ **A line may carry more than one class, so the three
columns are NOT a partition and deliberately sum to more than the total.** Honest about the instrument
rather than tidy.

| document | claim-lines | DERIVED | TRACKING | INSTR | DERIVED-only |
|---|---|---|---|---|---|
| CLAUDE_DESIGN_ASSET_REQUEST.md | 677 | 126 | 102 | 46 | 69 |
| CLAUDE_DESIGN_BOARD_1I_NOTE.md | 134 | 18 | 8 | 6 | 17 |
| ELECTIONS_ARCHITECTURE.md | 97 | **31** | 15 | 8 | 22 |
| ELECTIONS_CAMPAIGN_SPEC.md | 584 | **0** | **0** | 79 | 0 |
| ELECTIONS_GAP_TABLE.md | 97 | **62** | 43 | 15 | 26 |
| ELECTIONS_PLAY_CALIBRATION.md | 397 | 55 | 53 | 35 | 48 |
| ERRANDS.md | 54 | 4 | 15 | 11 | 1 |
| LAW_BROWSER_BOARD_RULINGS.md | 94 | 12 | 5 | 5 | 11 |
| MISSING_PREREQUISITES.md | 368 | 120 | 134 | 41 | 55 |
| POLISIM_BACKLOG.md | 964 | **214** | **362** | 139 | 62 |
| POLISIM_COHORT_SPECLET.md | 133 | 17 | 23 | 14 | 14 |
| POLISIM_FEATURE_LIST.md | 150 | 26 | 42 | 20 | 18 |
| POLISIM_MASTER_ROADMAP.md | 495 | **137** | 124 | 60 | 64 |
| POLISIM_SEED_DATA_MACRO_OVERHAUL.md | 1038 | 83 | 56 | 71 | 71 |
| POLISIM_TAX_SPECLET.md | 101 | 10 | 15 | 9 | 9 |
| POLISIM_UI_V3_DIRECTION.md | 148 | 11 | 31 | 9 | 8 |
| POLISIM_V2_SCREEN_SPEC.md | 590 | 68 | 38 | 40 | 49 |
| SEND_PACKAGE.md | 88 | 15 | 7 | 7 | 13 |
| **ALL LIVE** | **6209** | **1009** | **1073** | **615** | **557** |

### The answer to question 1, as a number

**1,009 claim-lines across 18 live documents assert a fact about the code — 16% of live document prose.
557 of them assert *only* that**: they rule nothing and track nothing. **That 557 is the coupling** — the
part of the corpus that can go wrong without anybody being wrong about the work.

### The two extremes are the whole argument

- **`ELECTIONS_CAMPAIGN_SPEC.md`: 584 claim-lines, DERIVED = 0, INSTRUCTION = 79.** Zero backticked
  members, zero `.cs` paths, zero hashes, zero code counts. **It cannot go stale.** It is also the one
  document nobody may edit — installed verbatim, cited by § number. **The target state already exists in
  this repo as a worked example.**
- **`ELECTIONS_GAP_TABLE.md`: 62 DERIVED of 97 claim-lines — 64%, the highest density.** Every row states
  what the code currently holds. ⚠ **Its rows have already been hand-patched twice** (*"Re-derived
  2026-08-31 (C-0.2): the row read …"*) — staleness repaired by the same operation that produced it.

⚠ **`ELECTIONS_ARCHITECTURE.md` shows the pattern at its purest.** Of its own inventory it says: *"This is
a 2026-08-29 inventory kept as the gap table's EXISTS column, **not a description of HEAD** — four of its
entries no longer exist."* **The document knows it is stale and manages the staleness with prose instead
of cutting the link.** That sentence is a permanent tax on every future reader.

---

## §A.3 — Where each document's DERIVED claims should live

Three destinations, per claim class rather than per claim.

### The "Generated" mechanism already exists — and has never been pointed at a document

The brief cites `ValkretsMandateColumnDiagnostic` as the precedent. **Measured: it only calls
`Debug.Log`. It writes nothing.** The figure is re-derivable, but the last mile — getting it *into* a
document — is a human retyping it, which is exactly where staleness enters.

**The real precedent is `ElectionsDataCatalogGenerator` + `GeneratedCatalogCheck`**, and it is complete:

- the generator writes the file and stamps it `// GENERATED by … DO NOT EDIT BY HAND.` plus a `SHA-256:`
  of its **source** (`ElectionsDataCatalogGenerator.cs:192–208`);
- the check re-derives that digest every run and fails on drift, comparing **the digest, not a re-parse** —
  *"a second parser would be a second thing to keep true"*.

⚠ **Measured: no live document contains a generated-block marker of any kind.** The word "regenerated"
appears in six live documents and in every case means *a human rebuilt it by hand*. **The mechanism the
project needs is already built, proven, and pointed only at C#.**

### Per document

| document | DERIVED | destination for the bulk of it | why |
|---|---|---|---|
| `ELECTIONS_GAP_TABLE.md` | 62 (64%) | **GENERATED** | It is a status table over 44 spec sections. Its EXISTS/BUILT column is a query, not a judgement. The Class and rationale columns stay authored. |
| `ELECTIONS_ARCHITECTURE.md` | 31 (32%) | **REFERENCED**, then mostly **DELETED** | Its inventory section already admits it does not describe HEAD. An architecture document should state the *rulings* (R-EL1, R-N2, the §40 divergence) — all INSTRUCTION, all timeless — and point at the code for what exists. |
| `POLISIM_BACKLOG.md` | 214 | **REFERENCED** for names, **DELETED** for counts, **GENERATED** for closure state | Its own rule already says *"the repo outranks this file… the log wins and the row is corrected"*. Findings 2 and 3 are both here and both are decoration inside a row whose point survives without them. |
| `POLISIM_MASTER_ROADMAP.md` | 137 | **GENERATED** (the document-set table) / **DELETED** (the rest) | Finding 4 lives here. The table is a `ls *.md` result typed by hand. Everything else is superseded status — see the ruling below. |
| `MISSING_PREREQUISITES.md` | 120 | **REFERENCED** | 38 commit hashes, the most of any document. A hash pins *history* and belongs in the historical records; a live tracker needs the row's state, not its provenance. |
| `CLAUDE_DESIGN_ASSET_REQUEST.md` | 126 | **GENERATED** | ⚠ Already 90% there by hand: it **pastes `PartyMarkCoverageCheck`'s literal output** (*"=== Party marks: 53 seeded part(ies), 1 with a resolving mark…"*). Make the paste a generated block and the coupling is gone. |
| `POLISIM_V2_SCREEN_SPEC.md` | 68 | **KEEP, with vintages** | Its measured px figures are *design intent at a stated resolution*, not readings of the code — and §24 already rules *"every number in this document is suspect until derived or confirmed"*. Finding 5 is a status claim, not a measurement, and should be **deleted**. |
| `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` | 83 | **KEEP** | ⚠ **Its DERIVED claims are about the world, not the code** — sourced figures with source, vintage and basis. No demonstrable staleness was found in it. **It is already correct practice.** |
| `ELECTIONS_CAMPAIGN_SPEC.md` | 0 | **KEEP, never edit** | The model. |
| `ERRANDS.md` | 4 | **REFERENCED — urgently** | Only 4 DERIVED claims, and E-3's uncomputed line-ending sentence is one of them. Smallest coupling, highest cost per claim. |

### The one ruling this review asks for first

⚠ **`POLISIM_MASTER_ROADMAP.md` is the document to rule on, and the brief mis-states its status.**
Commit `1340738` retired **`POLISIM_MASTER_LIST.md`** (verified: absent from root) — **not the roadmap.**
The roadmap survives, has demoted itself twice (*"The live list is no longer kept here"*), carries **137
DERIVED claims and 495 claim-lines**, and **is provably unaware of the file that governs the project.**

**It is the largest single block of coupling with the least remaining authority.** The recommendation is
to **retire it the way the master list was retired** — its standing constraints migrated to
`POLISIM_FEATURE_LIST.md`, its history to `COMPLETED.md`. That single ruling removes **137 of the 1,009
DERIVED claims — 14% of the whole coupling — without writing a line of code.** ⚠ **Recommendation only.**

---

## §A.4 — What decoupling would retire

**Measured: of the 25 checks in `Suite`, exactly three read live markdown at runtime** (as opposed to
merely naming a `.md` file in a comment):

| check | binds on | if DERIVED claims move out |
|---|---|---|
| `DocumentClaimCheck` | all live `*.md`, two clauses (MEMBER GONE, WRONG OWNER), both ratcheted at **0** | **SHRINKS, does not retire.** Its subject is the *Referenced* destination — a document naming `BuildParties()` still needs that name to resolve. Under the convention it gets *more* valuable and cheaper, because the count-and-line-number claims it cannot see would be gone. |
| `PreWiringPremiseCheck` | `POLISIM_BACKLOG.md` + records | **SHRINKS.** Written because a done-when was a grep. Generated blocks remove the premise it re-checks. |
| `DesignNotificationCheck` | `CLAUDE_DESIGN_ASSET_REQUEST.md` | **STAYS WHOLE.** Its subject is an *outward-facing ask going stale against a decision* (S-39) — a TRACKING failure, not a DERIVED one. **Decoupling does not touch it.** |

### The plain answer the brief asked for

⚠ **Not one of the 25 checks can be retired by decoupling, and this must be said plainly.** Two shrink;
one is untouched; the remaining 22 read code, assets or artifacts and never had a document coupling to
cut.

**And the sharper finding: not one of the five stalenesses in §A.1 was catchable by any existing check.**

- Findings 1, 3, 4 are **counts**. Nothing in the project counts.
- Finding 2 is a **line number**. Nothing resolves line numbers.
- Finding 5 is a **status claim** ("not yet built"). Nothing checks status against `git`.
- The retracted sixth would have been an **environment fact**. Nothing reads git config — and nothing checked the REVIEW's probe either.

`DocumentClaimCheck` is green on all six, correctly — its two clauses are deliberately narrow, and the
narrowness is well-argued in its own doc comment (a wider scan yields *"~580 candidates, most of them BCL
and Unity names this repo has no standing to judge"*).

**So the coupling cannot be closed by checking. The 25 checks are already at the sensible limit of what
scanning prose can prove.** ⚠ **The only move left is to stop writing the claims** — which is why this
review's recommendation is a convention rather than an instrument, and why R-N5 is exactly right.

---

## §A.5 — The proposed convention (one page)

> ### The test
> **The code can change freely and no document becomes wrong** — only *incomplete*, and only where
> TRACKING has genuinely moved.

**1. A live document may say:**
- what is **ruled**, **owed**, **forbidden**, or **conventional** (INSTRUCTION — timeless);
- what is **open**, **whose** it is, and **what it unblocks** (TRACKING);
- **where a fact lives**: *"the party count is whatever `BuildParties()` seeds"*.

**2. A live document may not say — outside a generated block:**
- a **count of code things** ("53 parties", "254 `.cs` files", "TWENTY files", "twenty-one checks");
- a **line number** (`CampaignRun.cs:341`);
- a **measured figure** presented as current;
- a **build status** a `git log` could answer ("the one screen not yet built");
- an **environment fact** (`core.autocrlf`, an install path, a byte size).

⚠ **This binds code comments too.** Finding 1 is in `CheckSuite.cs`, and `POLISIM_FEATURE_LIST.md` — a
document — got the same number right.

**3. Where generated blocks go.** Follow the existing, proven mechanism, unchanged in shape:

```
<!-- GENERATED by <Tool>. DO NOT EDIT BY HAND. source-digest: <sha256> -->
… emitted lines …
<!-- END GENERATED -->
```

Same three properties as `ElectionsDataCatalogGenerator`: a **DO NOT EDIT** stamp, a **digest of the
source**, and **drift detected by comparing the digest, never by re-parsing** — a second parser is a
second thing to keep true.

**4. How a session updates records without transcribing a fact.**
- Move a row's **state**; never restate the evidence. `✅ CLOSED at <hash>` is a pointer.
- Never **edit a board forward** — re-derive it, which is already rule 9. **Finding 4 is a board that was
  neither re-derived nor edited: it was simply left.**
- When a figure is genuinely wanted in prose, **name the command that produces it** instead of its result.
- ⚠ **Never repair a stale fact by flipping it.** E-3's line-ending sentence has been flipped by hand before, and this review nearly flipped it again. **Derive it or delete it.**

**5. The three historical records are exempt and stay exempt.** `COMPLETED.md`, `CLAUDE.md` and
`ELECTIONS_PROTOTYPE_LOG.md` exist to say what *was* true. Naming a since-deleted member is them working
correctly — already `DocumentClaimCheck`'s ruling, and it should govern the convention too.

---

# PART B — Where the time goes

## §B.1 — What is measurable, and what is not

⚠ **The headline is an absence, and it is the most important finding in Part B.**

**No wall-clock duration is recorded anywhere for a capture bar, the check bar, the four-width matrix, or
a film run.** The records log capture counts, overflow counts and byte-identity to four decimals, and not
one figure for how long the bar that produced them took. `COMPLETED.md:7906` describes the closing gate in
full — *"81 captured, 0 failed"*, *"exit 0 over all four sets (324 captures)"*, *"6 of 6 byte-identical"* —
**with no duration at all.**

**Unity run cost has never been measured and cannot be recovered retrospectively.** Per the brief, this is
stated rather than guessed.

Every duration figure that exists in the repo came from **diagnosing a failure**, not from measuring a
success:

| figure | what it measures | source |
|---|---|---|
| **~40 s** | warm-up per Unity launch — the only per-launch cost figure in the repo | `COMPLETED.md:3496` |
| **67 s** | a full capture pass, once invoked correctly | `CLAUDE.md:5999` |
| **over an hour** | the six-country trajectory suite | `POLISIM_BACKLOG.md:871` |
| **51 min** | a capture pass wedged under `-batchmode -nographics` | `CLAUDE.md:5983` |
| **24 min** | one trajectory diff wedged on `Start-Process -Wait` | `CLAUDE.md:8904` |
| **20+ min**, ≥3 recurrences | the post-simulation re-indexing hang | `CLAUDE.md:2755`, `:2763` |
| **~36 h** | the only aggregate effort figure in the repo | `CLAUDE.md:9328` |

### The three largest costs, with figures

**1. Hangs and lost Unity runs — the largest measurable sink.** Summing only the durations actually
recorded: **51 + 24 + 20 = 95 minutes** in three incidents, against a class the records say recurred
*"5+ occurrences"* and which was expensive enough to change project policy — `CLAUDE.md:5830` grants
blanket permission to force-kill `Unity.exe`, *"which cost significant time across 5+ occurrences"*.
⚠ **This is a floor, not a total**, because most instances carry no duration.

**2. Records maintenance — and it has demonstrably displaced validation.** The single clearest measurement
in the repo, `POLISIM_BACKLOG.md:871`:

> *"The explanation needs the **trajectory suite** … and **the budget that remains cannot carry both it
> and the records of the work already done.** Nothing is landed."*

**An hour-long validation run was skipped so the write-up could be finished.** Corroborating shape: of the
34 commits in the 2026-09-01 run, roughly **thirteen are residue-bookkeeping commits** each moving one
integer (`a0f4593` *"M-S6 closed at c631505 - residue 17 -> 16"*, and twelve of that form). **Two of those
closure rows landed in the wrong place and each cost a corrective commit** (`b3357d9`, `cc07f15`).

**3. Session start-up — measurable in size, not in minutes.** The four records total **29,908 lines**.
`CLAUDE.md` opens with a `⚠ READ FIRST` block plus a **7-row era table** stating which recorded numbers
are comparable across which recalibrations — *"When quoting a number from this file, check which era it
belongs to."* ⚠ **Every figure read out of a 14,552-line file must first be checked against that table.**
The discipline compounds this deliberately: rule 9 requires boards be **re-derived, never edited forward**,
so a session re-derives state it cannot trust. `CLAUDE.md:14314` records a session **re-counting its own
root documents**.

⚠ **How long that takes is not recorded and I will not estimate it.**

---

## §B.2 — A measured constraint the brief did not know about

**The check bar cannot run while the Unity Editor is open.** Measured this session, not assumed:

```
Aborting batchmode due to fatal error:
It looks like another Unity instance is running with this project open.
… Application will terminate with return code 1
```

⚠ **This has a direct consequence for the review's own deliverable** — see §C. It also means every "one
green bar per commit" costs a session either an Editor close/reopen or a wait, **and that cost has never
been measured either.**

⚠ **Incidental finding, recorded because it wasted a run here:** `CLAUDE.md` names the editor at
`G:\UNITY\Unity Hub\6000.5.4f1\Editor\Unity.exe` in **14 places**. That path **does not exist**; the
installed editor is **6000.5.6f1**, matching `ProjectSettings/ProjectVersion.txt`. All 14 are in
`CLAUDE.md`, which is historical and therefore *correct to be stale* — ⚠ **but a session reading it for an
invocation path gets a failed run.** **No live document carries the stale path** (verified), so the
convention is already working; this is an argument for **§B.4's start-up command emitting the path** rather
than for editing history.

---

## §B.3 — The self-inflicted class, re-measured

**The brief's "three instances, four Unity runs lost" is understated roughly threefold, and two of its
three categories are the same incident.**

| # | what failed | Unity runs lost | source |
|---|---|---|---|
| 1 | Capture run with `-batchmode -nographics`, which the tool's own doc **forbids in bold**; `WaitForEndOfFrame` never fires. *"reproduced identically three times running"* | **3 wedged + 1 re-run** | `CLAUDE.md:5990`, `:5997` |
| 2 | **The same mistake again** weeks later on the `cc1b` film, then `-shotwidth` omitted so 1280 silently filmed at the 1600 default | **2** | `COMPLETED.md:5871–5876` |
| 3 | PowerShell parameter named `$args`, shadowed by the automatic variable → Unity launched with `-logFile` alone and **every bar step "passed" in seconds** | **1 whole bar** | `CLAUDE.md:13400` |
| 4 | `cleanup && capture` — `rm` hit a file lock, `&&` short-circuited, **the Unity run never happened while reporting exit 0** | **1** | `CLAUDE.md:5800` |
| 5 | Unity output piped to `/dev/null`, hiding `Unity.dll failed to load` — *"through two wasted attempts"* | **2** | `CLAUDE.md:5729` |
| 6 | `-shotheight=800` instead of 720 → **13 spurious overflows**, needing a revert-and-refilm bisect | **≥2 re-films** | `COMPLETED.md:6583` |
| 7 | The "1280" campaign film actually shot at 1918×953 — the evidence was void | **1 family re-filmed** | `CLAUDE.md:14486` |
| 8 | S-20: films exited 0 with 8 captures and **every frame was the desk** | **1 family re-filmed** | `COMPLETED.md:7746` |
| 9 | Committing on a **red bar**, twice in one session | 0 Unity; 2 bar re-runs | `COMPLETED.md:10987` |
| 10 | A placement script's walk-back never matched and **ran to the top of the file**, splicing the row to line 1 | 0 Unity | `cc07f15` |
| 11 | Same class: a closed row appended past a moved landmark, landing under the wrong heading | 0 Unity | `b3357d9` |

**At least nine Unity runs lost to invocation errors, plus two capture families re-filmed.**

### Three corrections to the brief's accounting

- ⚠ **"Red-bar commits" and "the line-1 splice" are one class, not two.** `cc07f15`'s own message says
  *"the previous commit was made on a red bar"* — **the splice caused one of the two red-bar commits.**
- ⚠ **The lost-variable incident is not the splice.** The splice was a *regex that never matched*
  (`cc07f15`). The genuine lost-variable failure is `$args` shadowing, and it cost **a whole bar**.
- ⚠ **"Broken string quoting, twice" was understated and still live.** Measured in the working tree at the
  start of this review: **24 occurrences of SQL-style `''` escaping leaked into prose across 4 files** —
  `COMPLETED.md` (9), `POLISIM_BACKLOG.md` (7), `ERRANDS.md` (6), `CLAUDE_DESIGN_ASSET_REQUEST.md` (2) —
  every one a doubled possessive (`Elias''s`, `era''s`, `4''s`). Introduced by at least four commits, all
  2026-09-01. **Repaired in §C.** *(And a 25th instance occurred inside this very review — see §C.)*

### The pattern the numbers actually show

**The dominant failure is not carelessness. It is that a wrong invocation and a right one look identical
in their output.** Incident 3 *"passed in seconds"*. Incident 4 *"reported exit code 0"*. Incident 8
*"exited 0 with 8 captures"* of the wrong screen. The project has already named this —
`POLISIM_BACKLOG.md:1119`: *"Three shell probes were wrong on 2026-09-01 and **every one of them looked
right in its output**."*

⚠ **And the single most expensive pattern is a repeat: `-batchmode -nographics` passed to a capture tool
whose own class comment forbids it in bold — twice, weeks apart, five runs between them.** The knowledge
was written down, in the right place, and did not survive contact. **That is an argument for a mechanical
rule, not a better-written warning.**

---

## §B.4 — Proposals

### P1 · No shell state across tool calls; every file edit is one self-contained operation

**The brief's candidate rule, and the measurement supports it — including from inside this review.**

> **No shell state may cross a tool call.** Every invocation is complete in itself: no variable set in one
> call and read in another, no `cd` relied on, no `&&` chaining a cleanup to a run.
> **Every file edit is one self-contained operation, matched on TEXT, never on a line number or a walked
> position.**
> **Prefer a direct file write to a shell heredoc for any content containing prose.**

Each clause is bought by a measured incident: `$args` shadowing (3), `cleanup && capture` (4), the
walk-back splice (10, 11), the `''` corruption (24 instances), **and this review's own heredoc failure.**

**Cost: zero — it is a written rule.** ⚠ **It is also the only proposal here that addresses the class
which has cost the most Unity runs.**

### P2 · One green bar per commit — with the precondition stated

Already ruled in `POLISIM_FEATURE_LIST.md`. **What is missing is the precondition**, measured in §B.2:
**the bar cannot run while the Editor is open.** The rule should read *"green for the tree being
committed, from a run made with the Editor closed"* — otherwise it is unsatisfiable and gets skipped,
which is how red-bar commits happen. **Cost: one sentence.**

### P3 · The start-up command

Replace *"read to find out where we are"* with **one command that prints the state**: HEAD and subject ·
the working tree · the top of `POLISIM_FEATURE_LIST.md` · the open `ERRANDS.md` rows · the claim census
totals · **the editor path from `ProjectVersion.txt`** (§B.2) · **whether the Editor is open** (P2's
precondition).

⚠ **Every one of those is a `git`/filesystem query.** No transcription — which makes it §A.5 clause 4 in
executable form. **Cost: ~1–2 hours.** ⚠ **The saving cannot be quantified** because start-up time was
never measured; it is proposed on the strength of the 29,908-line corpus and the era table, and **that
weaker basis is stated rather than hidden.**

### P4 · Generated blocks, starting with the two documents that already paste output by hand

`CLAUDE_DESIGN_ASSET_REQUEST.md` already pastes `PartyMarkCoverageCheck`'s literal output;
`ELECTIONS_GAP_TABLE.md` is a status table over the code. **Both are hand-generation waiting to be
mechanised**, and the generator+digest+drift-check pattern already exists. **Cost: ~half a session for the
first block.** Do **one** and judge it before doing more.

### P5 · Records generated from git

Roughly thirteen of one run's 34 commits were closure rows moving one integer, and **two of them landed in
the wrong place**. A closure row is `git log --grep` plus a template. ⚠ **But the residue counter those
rows served is retired with the audit era**, so much of this cost has already gone. **Propose only: emit
the `COMPLETED.md` section skeleton from the commit range** — never the prose.

### P6 · Unity batch incrementality

⚠ **This proposal cannot be ranked, and that is the honest answer.** The brief asks what is safely
incremental; the ranking criterion is time returned per hour spent — and **the run costs are unmeasured
(§B.1)**. Making the trajectory suite per-country might save 50 minutes between gates or 5; **nothing in
the record distinguishes those.**

**Therefore the first move is not incrementality, it is instrumentation:** have the bar print elapsed
wall-clock per step. ⚠ **This is not a check** — it prints, it never fails, R-N5 does not apply.
**Cost: minutes. It is the precondition for every other Unity-cost decision**, and its absence is why the
brief's own "over an hour" is the only figure available.

**Stated for the record:** coverage at a gate is not to be reduced. The four-width matrix at a track close
and the trajectory suite at a BASELINE family stay whole. **Only between-gate runs are candidates**, and
only once the numbers exist.

---

## §B.5 — Ranked, with the declines

| rank | proposal | cost | returned | why |
|---|---|---|---|---|
| **1** | **P1** the mechanical rule | ~0 | the largest measured class — ≥9 lost Unity runs, 2 red-bar commits, 24 corruptions | **A written rule against the failure mode that has cost the most. Nothing else has this ratio.** |
| **2** | **P6a** time the bar | minutes | unlocks every Unity-cost decision | Cannot rank P6 without it. Cheapest unblock in the review. |
| **3** | **P2** the bar's precondition | one sentence | the red-bar class | Makes an existing rule satisfiable. |
| **4** | **A.3's ruling** retire the roadmap | one ruling + a migration | **137 of 1,009 DERIVED claims — 14% of the coupling** | Largest coupling reduction available, and no code. |
| **5** | **P3** the start-up command | 1–2 h | unquantified | ⚠ Ranked on a weaker basis than the four above, and said so. |
| **6** | **P4** generated blocks | ~half a session each | ~60–120 DERIVED claims per document | Real, but slower than the ruling above it. Do one, judge, then decide. |

### Declined

- ⚠ **P6b — making the trajectory suite incremental now.** **Declined until timed.** It may be the biggest
  win in the project or nearly nothing; committing engineering to it before P6a is guessing.
- ⚠ **P5 in full — generating closure rows.** **Declined.** The residue counter they served is retired,
  so this now builds a tool for a workflow that just ended. **A proposal that saves five minutes a session
  and costs a session to build is a proposal to decline** — this is one.
- ⚠ **Any new check for the five stalenesses in §A.1.** **Declined under R-N5.** The retracted sixth is not at
  two instances after all, and a check written on a probe this review got wrong would have been the worst possible use of R-N5.
- ⚠ **Rewriting the documents in this pass.** **Declined by the brief**, and correctly: the convention
  should be ruled before 1,009 claims are touched.

---

## §C — The surgical changes made

⚠ **CRITICAL: none of these is committed, and the reason is the project's own rule.**

**"One green bar per commit, no exceptions."** The bar **cannot run** — the Unity Editor is open and
refuses a second instance (§B.2, measured). **So the changes sit in the working tree, unstaged, for Elias
to commit behind a green bar.** ⚠ **This is the rule working, not the review failing.** Committing these
without a bar would be exactly the defect `COMPLETED.md:10987` records twice.

**Verify with `git diff`; revert with `git checkout --`.**

### Change 1 — the `''` corruption repaired (4 files, 24 occurrences)

Every doubled possessive left by PowerShell `''` escaping leaking into prose. Repaired by a single
text-matched substitution; **verified to zero** (`grep -c "''" *.md` → no matches). ⚠ **Line endings were
checked before and after: the files are LF and stayed LF, and the edit is content-only** — `git diff --stat`
shows **25 insertions, 27 deletions**, not a whole-file rewrite.

*Safe because:* no code touched, no claim altered, purely the repair of a mechanical corruption.

### Change 2 — an orphan fragment removed from `ERRANDS.md`

Two lines duplicating the tail of the preceding bullet — residue of an edit that spliced rather than
replaced, the same class as findings 10/11 in §B.3. Removed by **matching on text, not line number**,
which is P1's rule applied to its own evidence.

*Safe because:* the complete sentence survives intact one line above; the fragment carried nothing unique.

### Change 3 — `Tools/claim_census.sh` added

The instrument behind every figure in §A.2. ⚠ **Not a check**: it never fails, never gates, R-N5 does not
apply. It exists so this report's counts are **re-derivable rather than transcribed** — the report
practising the convention it proposes.

### Deliberately NOT done

- ⚠ **E-3's line-ending instruction is NOT flipped.** The review's claim that it was wrong has been
  **retracted** — it reads true. **Flipping a correct instruction on a bad probe is exactly the defect
  this review exists to end**, and it came within one edit of doing so. What it needs is to be *computed*
  rather than asserted, and that is a ruling.
- ⚠ **Findings 1–5 are not corrected.** Each sits in a document whose convention is not yet ruled, and the
  brief forbids rewriting ahead of the ruling. **Correcting them by hand now would be the transcription
  this review exists to stop.**

---

## What Elias is asked to rule

1. **The §A.5 convention** — adopt, amend, or reject. Everything else waits on it.
2. **`POLISIM_MASTER_ROADMAP.md`** — retire it as `POLISIM_MASTER_LIST.md` was retired? **14% of the
   coupling, one ruling, no code.**
3. **E-3's line-ending sentence** — make it COMPUTE the line ending and the digest, or say nothing about
   line endings. ⚠ **Not because it is wrong — it is right — but because nothing keeps it right**, and
   this review nearly flipped a correct instruction on a malformed probe.
4. **P1**, and **P2**'s precondition — both cost nothing and address the largest measured class.
5. **P6a** — time the bar, so P6 can be ranked at all.
6. **§C's three changes** — commit behind a green bar, or revert.

---

# §D — Absorbed from the retired `POLISIM_REVIEW_ADDENDUM.md` (2026-09-01)

⚠ **The addendum is deleted and this section is what survived it.** Two of its claims did not: its
finding 6 is **retracted** (see the retraction at the head of §A.1), and its bar figures — 349.9 s cold,
`ArtifactIdentityCheck` at 243.6 s / 75 % — are **superseded** by `Logs/bar_timing.tsv`, which is produced
by `BarTiming` on every run and therefore re-derivable rather than transcribed. **A one-off stopwatch
reading in a document is exactly what the claim convention forbids; an appended log is what replaces it.**

Everything below was **re-measured in this run** before being carried over.

## D.1 — Two corrections to Part B, both accepted

⚠ **"Cannot be recovered retrospectively" is not "cannot be measured", and §B.1 treated them as one.**
The record genuinely holds no duration for any successful run — that headline stands. But the cost of a bar
was always available to anyone willing to spend one bar finding out. **A measurement that has not been
taken is not an unmeasurable quantity**, and this project applies that distinction to everything else.
§B.2's decline of P6 rested on the conflation and is withdrawn in D.3.

⚠ **§B.2's Unity constraint was real but mis-attributed.** The refusal —
*"another Unity instance is running with this project open"* — fires on **any** concurrent Unity process
against the project, not specifically on an open Editor. This review's own attempt collided with a batch
bar left running in the background, and the report generalised the message into *"the bar cannot run while
the Editor is open"*, then declined to commit on that basis. **The honest rule is narrower and more
useful: one Unity process against the project at a time, Editor or batch — so Unity work cannot be
parallelised across agents or background tasks.**

## D.2 — What `ArtifactIdentityCheck` is actually doing

Re-measured this run: **876 `traj_*.csv` artifacts, 3.7 GB**, re-read and re-identified **on every bar**.

⚠ **This is not an argument for weakening the check.** Those artifacts are the evidence base for the
trajectory gate and their identity is exactly what must not drift. It is an argument about **when** the
question is asked.

## D.3 — P6, now rankable, and it outranks everything

| proposal | measured basis | time returned |
|---|---|---|
| **P6a — `ArtifactIdentityCheck` incremental between gates, full sweep at gates** | ~92 % of the cheap suite, from `Logs/bar_timing.tsv` | **the bar falls to roughly a tenth of its cost on every commit** |
| P6b — per-country trajectories | still unmeasured; the suite was not run | **still unrankable** |

⚠ **Coverage at a gate is untouched**, which the brief forbids reducing. The reduction is **between**
gates, which the brief asks for: the artifact set is append-only historical evidence, so between gates
*"is this the same file"* is answerable from digests, and at a gate the full sweep runs as it does now.

**On the brief's own test — time returned per hour spent — P6a is the highest-value item in the review and
ranks above P1.** P1 is still right and still costs nothing; P6a returns minutes per commit for an
afternoon.

### The cost it removes, measured

`D-17` rules that a closure record is its own commit, because a commit cannot contain its own hash. Over
this session's window that produced **fourteen record-or-fix commits, twelve of them changing one to three
lines** — **each requiring a full green bar**, whose ~92 % is re-identifying 3.7 GB of CSVs that no
document edit can touch.

⚠ **D-17 is not the fault and should not be struck** — the citation must be true, and the two-commit shape
is the only one where it is. **The fault is that the bar has one price regardless of what changed.**

## D.4 — Where the session's output went

Re-measured this run over the audit-era and F1 window (`git diff --numstat 12a4833^..88d8899`, split by
path):

| | lines added |
|---|---|
| **Editor tooling** (checks, diagnostics, generators) | **1 627** |
| **`Assets/Scripts`** — the game | **619** |
| markdown (records, register, feature list) | 1 976 |

⚠ **72.4 % of the C# written was instrumentation.** ⚠ **The markdown grew more than the game and the
tooling combined.**

**This is the strongest argument in the review for R-N5 and for the audit era ending when it did**, and it
is a measurement rather than an impression. It is **not** an argument that the instrumentation was wasted —
several checks caught defects the day they were built. **It is an argument that the ratio cannot persist**,
which is what the feature list exists to change.
