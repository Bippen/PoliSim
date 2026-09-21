# Reviews — the reports the review ledger stands on

A money path gets its adversarial review (§524's rule), and since 2026-09-21 the cheap bar fails where it did not (`COMPLETED.md` §546, `Assets/Editor/ReviewLedgerCheck.cs`). Each `reviewed` row of `Tools/review_ledger.tsv` names a file here.

**What a file here is.** One review: a short header saying what was reviewed, in what form and on which states, then the reviewer's report VERBATIM - and, where the review forced a rework, the second reader's report on the rework. A report is not edited after the fact; what was done about its findings is told in `COMPLETED.md`, not here.

**The form of a review** without Elias's word for a multi-agent workflow: one independent read-only reader on the change, and one on the rework if there is one. The reader is told what changed and why, what is claimed, and is asked to break it; it names every file it read.

**What the check asks of a file here:** that it exists, that it is a report's length, and that it names the file (or the baseline digest) its row is for. It cannot tell a real review from a written one. It turns a silent omission into a false statement in the record, and that is all a check can do.

Add a row with `Tools/review_row.ps1`.
