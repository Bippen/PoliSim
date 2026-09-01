# Review addendum — the bar, TIMED (2026-09-01)

**This addendum exists because `POLISIM_REVIEW_2026-09-01.md` §B.1 says the bar's cost "cannot be
recovered retrospectively" and declines P6 on that basis. That is right about the RECORD and wrong about
the QUESTION.** The cost could not be recovered from history; it could be **measured by running it**, which
is what this addendum does. Every figure below was produced by a stopwatch around a `Start-Process -Wait`
in this review, not read from a document.

---

## 1 — The bar, measured

⚠ **Three independent measurements, and the first was taken cold. Read the RATIO, not the seconds.**

| measurement | full bar | `ArtifactIdentityCheck` alone | its share |
|---|---|---|---|
| **first, this review** (disk cold) | 349.9 s cold · **323.5 s warm** | **243.6 s** | 75 % |
| **second, the forked reviewer** | **147.3 s** | **136.1 s** | **92 %** |
| **third, this review, after both** | **159.6 s** | **148.7 s** | **93 %** |
| `MetaTextCheck` alone, for scale | — | 26.4 s | — |

⚠ **The absolute cost varies 2.3× — 150 s to 350 s — and it is DISK CACHE, not the code.** The check reads
3.7 GB; the first read of that in a session comes off the disk and every later one comes out of the OS page
cache. **Anyone quoting a single number for this bar is quoting their cache state.**

⚠ **The share converges where the seconds do not: 75 % → 92 % → 93 %.** My own first figure is the
outlier and it **understated** the finding — measured cold, the *rest* of the bar was slow too. **The
honest statement is that one check is about 92 % of the bar**, and the two later measurements, taken by
different agents, agree.

**Recompilation is not the cost either.** Cold minus warm on the first pair is **26 s** — the whole Unity
compile of a changed assembly. The bar costs what it costs whether or not anything changed.

### What it is doing

```
THE ENUMERATION: 876 traj_*.csv artifact(s) in '../PoliSim-captures/trajectories'; 876 checked
```

**876 files. `du -sh` on that directory: 3.7 GB.** The bar re-reads and re-identifies **3.7 gigabytes of
historical trajectory artifacts on every run** — including the fourteen runs this session that existed
only to prove a two-line document edit was safe.

⚠ **This is not an argument for weakening the check.** Those artifacts are the evidence base for the
trajectory gate and their identity is exactly what must not drift. It is an argument about **when** the
question is asked, which is §3.

---

## 2 — Two corrections to the main report

**§B.1's headline stands, narrowed.** *"No wall-clock duration is recorded anywhere"* is **true and worth
keeping** — the repo's only duration figures come from diagnosing failures, never from measuring a
success. ⚠ **But "cannot be recovered retrospectively" is not the same as "cannot be measured", and the
report treats them as one.** The cost of a bar is available to anyone willing to spend one bar finding out.
**A measurement that has not been taken is not an unmeasurable quantity**, and that distinction is exactly
the one this project applies to everything else.

**§B.2's constraint is real but mis-attributed.** Unity's refusal —

```
It looks like another Unity instance is running with this project open.
```

— fires on **any** concurrent Unity process against the project, not specifically on an open Editor. The
review's own attempt collided with a **batch bar this session had left running in the background**, and the
report generalised the message into *"the bar cannot run while the Editor is open"* and then declined to
commit its changes on that basis. ⚠ **Three bars ran during this review** while the report said they could
not. The honest rule is narrower and more useful: **one Unity process against the project at a time —
Editor or batch — so Unity work cannot be parallelised across agents or background tasks.**

---

## 3 — What the timing does to the ranking

### The measured cost of D-17

`D-17` rules that an item's closure record is its own commit, because a commit cannot contain its own hash.
Measured from `git log` over this session's window:

- **14 record-or-fix commits** matching `closed at <hash>` or `Fix:`
- **twelve of the fourteen changed 1–3 lines**; the largest changed 26

**Each one required a green bar. Fourteen bars is between 35 and 80 minutes** depending on cache state —
**Unity time spent proving that two-line document edits had broken nothing**, on a bar whose ~92 % is
re-identifying 3.7 GB of CSVs that no document edit can touch.

⚠ **D-17 is not the fault here and should not be struck.** The citation must be true, and a two-commit
shape is the only one where it is. **The fault is that the bar has one price regardless of what changed.**

### P6, now rankable

The main report declines P6 (batch incrementality) as unrankable without a timing. With the timing:

| proposal | measured basis | time returned |
|---|---|---|
| **P6a — `ArtifactIdentityCheck` reads a manifest between gates, the full 3.7 GB sweep at gates only** | ~92 % of the bar, on three measurements that agree on the share | ⚠ **the bar falls to roughly a tenth of its cost on EVERY commit — the other twenty-four checks share ~11–80 s depending on cache** |
| P6b — per-country trajectories | not measured; the suite was not run in this review | unrankable, still |

⚠ **This does not reduce coverage at a gate**, which the brief forbids. It reduces it **between** gates,
which the brief asks for: the artifact set is append-only historical evidence, so between gates the
question *"is this the same file"* is answerable from a manifest of digests, and at a gate the full sweep
runs as it does now.

**On the brief's own ranking test — time returned per hour spent building — P6a is the highest-value
proposal in the review and should be ranked above P1.** P1 (no shell state across tool calls) is still
correct and costs nothing; P6a returns roughly four minutes per commit for what looks like an afternoon.

---

## 4 — Where the session's output actually went

Measured with `git diff --numstat 12a4833^..HEAD`, the audit-era and F1 window:

| | lines added |
|---|---|
| **Editor tooling (checks, diagnostics, generators)** | **1 615** |
| **`Assets/Scripts` — the game** | **607** |
| markdown (records, register, feature list) | 1 258 |
| **total** | **3 504** |

⚠ **72.7 % of the C# written was instrumentation. The game received 607 lines — 17.3 % of everything
written.** `COMPLETED.md` alone grew **835 lines**, more than the game did.

**That is the strongest argument in this review for R-N5 and for the audit era ending when it did**, and it
is a measurement rather than an impression. ⚠ It is not an argument that the instrumentation was wasted —
§A.3 of the main report shows the checks caught defects the same day they were built, and F1's own record
has two such catches. **It is an argument that the ratio cannot persist**, which is what the feature list
exists to change.

---

## 5 — One more instance of the self-inflicted class, found by the review

The main report counts the class and re-measures the brief's *"three instances, four Unity runs"* as **≥9
runs**. ⚠ **Add 24 more, in committed documents.**

`git diff` on this review's repairs shows **24 occurrences of `''`** — PowerShell's single-quote escape —
**left in four markdown files by this session's own edits**, including `POLISIM_BACKLOG.md`'s D-18 sheet and
`COMPLETED.md`. They were written by PowerShell here-strings and committed without being read back.

**This is the same class as the broken quoting and the lost variable, and it is the worst-behaved member of
it**, because nothing failed: no compile error, no red bar, no exit code. **The defect was invisible to
every guard in the project and survived into the permanent record.** Repaired in this review, verified to
zero.

⚠ **It is also the evidence P1 needed.** The rule *"every file edit is one self-contained operation"* is
not about tidiness — a here-string carrying escapes into a document is precisely what happens when the edit
and the shell that performs it are not the same thing. **And it is now the second time a quoting failure
has appeared inside the review auditing quoting failures**, the first being the report's own heredoc.

---

## 6 — ⚠ RETRACTION: finding 6 was wrong, and it was asserted in a pushed commit message

**Commit `88d8899`'s message says `ERRANDS.md`'s E-3 was wrong to hash "AS LF" because
`core.autocrlf=true`. That is FALSE and the retraction belongs here, not in a quiet edit.**

**Measured, decisively:** `tr -cd '\r' | wc -c` per file returns **zero carriage returns in every file in
the tree**. The working copy is **LF**. `core.autocrlf=true` is real and governs a **fresh checkout**, not
this working copy. **The instruction was right; the review was wrong.**

The probe that produced the false finding was `grep -c $'\r'`, which the shell expands to an **empty
pattern** — it matches every line, and it returned a count equal to each file's line count. ⚠ **A number
that equals the line count is the signature of a broken probe, and it was read as a measurement of
carriage returns.**

### Three things this costs, and they are worth more than the finding was

1. ⚠ **R-T2 was broken inside a review that quotes R-T2 as its standard.** A shell probe is the weakest
   evidence and may not carry a finding alone. This one carried a finding all the way into a pushed commit
   message under my name, and into a report given to Elias as fact.
2. ⚠ **The correction came within one edit of FLIPPING a correct instruction** — the precise failure the
   convention page forbids two paragraphs after stating it. The instruction has now been flipped by hand
   once already, in the opposite direction, which is how a true statement and a false one become
   indistinguishable.
3. ⚠ **The claimed independent confirmation does not exist.** The retraction cited
   `CLAUDE_DESIGN_BOARD_1I_NOTE.md` hashing to the digest `SEND_PACKAGE.md` publishes for it. **That digest
   is not in `SEND_PACKAGE.md`** (`grep` for the file's actual sha256 returns nothing, in either document).
   **The retraction is correct on the byte count alone** — but its corroboration was itself unverified, in a
   retraction *of* an unverified claim.

**Ruling 3 survives all of this and is strengthened.** The fault was never the value. **It is that a line
ending was written down at all instead of computed where it is used**, and a true transcription fails
exactly as a false one does the day the environment moves. `ERRANDS.md` now states no line ending and the
glance derives it.
