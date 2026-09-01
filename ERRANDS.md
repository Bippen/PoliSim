# ERRANDS — the outward-facing actions, waiting on Elias

**What this file is.** ⚠ **A session never takes an outward-facing action**: it does not register an
account, send a package, paste a document, or publish anything. Those are Elias's, always, and the
convention predates this file (E2: *sending is Elias's*). What changed on 2026-09-01 is where they go —
**here, one line each, with exactly what he does** — instead of into the tail of a report nobody re-reads.

**The rule for adding a row:** one line, the action in the imperative, and the thing that becomes possible
when it is done. If a row cannot say what unblocks, it is not an errand; it is a wish.

**The rule for removing a row:** it is done, or it is withdrawn with a reason. Not "it went stale".

---

| # | the errand | what it unblocks | opened |
|---|---|---|---|
| **E-1** | **Register at `itanes.it`** and download the **complete dataset** from the **2013** and **2018** political-election pages (`itanes.it/2024/12/12/itanes-2013-…` and `itanes.it/2024/12/10/itanes-2018-…`). Both are free for non-commercial research; both are behind *"Log in / Register to access"* with no anonymous download link. ⚠ **An account in your name is yours to create** | **Per-group loyalty, and with it the FdI ceiling.** `LoyaltyModel`'s non-circularity invariant needs 2013+2018 to predict 2022, and only 2022 is on the open Dataverse — the one wave available is the one wave the invariant forbids. What is wanted from each file is a single weighted cross-tab: **vote choice by the six age bands §137 used**, with that wave's own weight variable. Nothing else is missing (`COMPLETED.md` §149) | 2026-09-01 |
| **E-3** | **Paste the RETURN package** in `SEND_PACKAGE.md` to the Design project — now carrying THREE things in one paste: the derived mandate column and the correction that no built cartogram exists (row 6), the two whole stat-icon files with their digests (row 9), **and Elias's GO on the seven Swedish party marks plus the forty-five-mark batch after them** (row 2, answered 2026-09-01). ⚠ **Read each artifact back and hash it AS LF** — the working copy is LF, and the old package's CRLF instruction would have made a correct readback look like a failed paste | **The valkrets cartogram, the two §E4 stat icons, and the mark batch.** ⚠ **Nothing on our side waits on the word any longer**: every call site, coverage check and fallback path is built, so the marks are CONSUMED when they land rather than queued (`COMPLETED.md` §158) | 2026-09-01 |

---

## Not errands, recorded so they are not mistaken for one

- **The §V sitting** is not an errand — it is a review, and it has its own package
  (`PoliSim-captures/sv_index.html`). An errand is an action with a named unblock; a sitting is a
  judgement.
- **The open decision sheets** in `POLISIM_BACKLOG.md` §D are not errands: they cost nothing outward-facing, and each carries a recommendation a session has already taken as strikeable so that nothing waits on them. ⚠ **D-14 is NOT one of them and this line used to say it was.** It was explicitly *not* self-taken — **Elias RULED it (a)**: the D-2 (c) revert stands and F-A and F-B are split out as their own measured items, which is executed (`COMPLETED.md` §153).
  rulings, they cost nothing outward-facing, and each carries a recommendation a session has already
  taken as strikeable so that nothing waits on them.

---

## ✅ DONE

| # | the errand | answered | what it released |
|---|---|---|---|
| **E-2** | **Say GO on the seven Swedish party marks**, and the forty-five-mark batch after them, on board 3a's ruled vocabulary — five silhouettes × four cuts × two fills | ✅ **GO — Elias, 2026-09-01** | ⚠ **The asking is folded into E-3** so one paste carries the GO, the mandate column and the icon files. **Nothing on our side waits on it**: every call site, coverage check and fallback path is built, so the marks are consumed when they land rather than queued. The hairline *"no published colour"* swatch stays as the honest fallback until they arrive |

---

## The shelf's standing re-verification (added 2026-09-01)

⚠ **Enrolment can rot, and both enrolments are censuses rather than proofs.** Two things get re-verified
every shelf cycle rather than assumed:

- **the stripper's enrolment** — the checks that read C# source are enrolled or exempt-with-a-reason.
  `CommentImmunityCheck` fails the suite on an unenrolled one, so the re-verification is *reading the
  count in its output*, not re-deriving it. ⚠ Its one soft spot is a source reader that does not name the
  `*.cs` pattern.
- **the ledger's enrolment** — every ceiling or ratchet constant reports. `RatchetSlackCheck` fails on an
  unreported one. ⚠ Its soft spot is a bound whose NAME ends in neither *Ceiling* nor *Ratchet*.

**Neither soft spot is a hole to be lived with quietly**: each is written where the check declares it, and
closing either needs a lexer rather than a regex — which is the honest reason they are named instead.

---

## ⚠ E-4 · PUSH — and it is the one thing turning the bar red (added 2026-09-01)

`UpstreamCheck` reports **more than 10 commits ahead of `origin/main`**, above its own threshold, with the
sentence it exists to say: ***"That work exists on one disk."***

⚠ **A push is outward-facing, so this session does not take it.** That is not a technicality here: the
whole point of the threshold is that somebody with the authority to publish decides to publish, and a run
that pushed on its own to clear its own red check would be the purest form of tuning to pass.

| # | the errand | what it releases | what it costs to leave |
|---|---|---|---|
| **E-4** | **`git push origin main`** — nothing else; every commit is small, green at the time it was made, and independently reviewable | the bar returns to fully green, and `UpstreamCheck` stops being the only red check in the suite | ⚠ **the risk is a disk, not a merge.** There is no conflict to resolve and no review gate to pass — the work is simply unbacked |

⚠ **Until it is done the bar reads 22 of 23 with `UpstreamCheck` red, and that red is CORRECT.** It is
recorded here rather than worked around, and no ceiling was moved to hide it: the check is measuring
exactly what it says, and the answer is an action only Elias can take.

---

## ⚠ E-5 · Does the "as published" band come back? (added 2026-09-01, found by `DocumentClaimCheck`)

⚠ **Design was asked to draw a screen this project had already cut, and nobody told them.**
`CLAUDE_DESIGN_ASSET_REQUEST.md` row **E17** named a renderer deleted at RIDE-1 and described the
*"as published"* graph block — the date axis, release markers, the PRELIMINARY/FINAL badge, the dashed
revision frame, the 1yr/5yr/All pager. **P-A2 (2026-08-29) removed that whole band from Statistics ›
Domestic as a DISPLAY cut.** The row has been corrected to say so; **what has NOT been decided is whether
the band should return.**

| # | the question | why it is Elias's | what is true either way |
|---|---|---|---|
| **E-5** | Should the *"as published"* band come back to Statistics › Domestic — and therefore be drawn — or is P-A2's cut permanent? | It is a **display** judgement about how much of the publication mechanism the player should see, and the cut was made on a playtest finding. Nobody but Elias can say whether the playtest still rules | ⚠ **The MECHANISM is untouched and stays untouched.** `PublicationSystem` publishes; the election model's section-19 reading takes Published and never State (`PerceivedPerformanceHarness` asserts it); the PRELIMINARY and revision conventions live on the main graphs. **B6's honesty channels survived the cut — they moved, they were not lost** |

⚠ **Until it is answered E17 asks Design for nothing**, and that is now what the row says rather than what
a reader had to infer.