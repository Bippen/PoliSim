# Implementation brief - the roadmap sweep (PoliSim, 2026-10-16)

You implement ONE roadmap item of PoliSim (a Unity 6 political simulation) in YOUR OWN git worktree, as part of a sweep where a single
integrator (the main session) merges, runs Unity and writes the shared records. Your item, your worktree and your branch are named in your task.

## Read first (in this order)
1. `G:/UNITY/Projects/PoliSim/CLAUDE.md` - the project's rules, all of it (it is only rules, ~26 KB). Its rules bind you: sourcing (primary
   sources saved byte-exact with SHA256SUMS), [AUTHORED]/[FITTED]/[SOURCED] provenance, no invented numbers, DECLARED readings, checks that fail
   while a statement is false, the claim convention, CRLF/encoding traps, no Python on this machine (perl, PowerShell 5.1, Git Bash, dotnet 10).
2. Your task as the integrator wrote it, and `docs/RULINGS_2026-10-18.md` where it rules your item.
3. Your item's text in `docs/specs/USA_STAGE_PLAN.md` (or the feature-list row, for a non-US item) - its *Needs*, its body and its *Done when*.
   The plan is the authority for what the item must deliver. Rulings it cites are in the plan's section 4 and in `COMPLETED.md` (never read
   `COMPLETED.md` whole: `grep -n '^## NNN\.'` for a section and read that section only).
4. The code the card names. Read before you write; match the surrounding code's idiom, comment density and naming.

## Where you work
- ONLY in your worktree (e.g. `G:/UNITY/Projects/PoliSim-wt/us14`). NEVER write in `G:/UNITY/Projects/PoliSim` (the main tree: Unity uses it).
  Never run Unity (one job at a time on this machine; the integrator owns it). Never push.
- Do NOT edit the shared records: `COMPLETED.md`, `POLISIM_FEATURE_LIST.md`, `docs/specs/USA_STAGE_PLAN.md`, `ERRANDS.md`,
  `Assets/Editor/CheckSuite.cs`, `Tools/film_scope.tsv`, `Tools/review_ledger.tsv`. Return what they need as DRAFT TEXT instead (below).
  Exception: if your item requires a decision rule or range definition to be written into the plan BEFORE its measurement, return that text
  as `plan_prereg` - the integrator commits it before any run.
- New raw sources go in your own folder under `ElectionsData/<country>/raw/<topic>/` with a `SHA256SUMS.txt` (the existing convention there),
  fetched byte-exact (curl, never re-saved through a browser or an editor); record refused hosts as BILLED with the refusal. A source the
  plan says stays out of tree stays out of tree (`../PoliSim-captures/sources/...`) with its digest line in `out_of_tree.txt` as the existing
  rows do.
- Commit to your branch as you go (`git -C <worktree> add ... && git -C <worktree> commit -m "<item>: ..."`); the integrator merges your branch.

## The compile gate (use it after every code change)
`bash G:/UNITY/Projects/PoliSim/Tools/cgate/gate.sh <your worktree>` - compiles the whole
project (runtime + Editor) outside Unity against the main project's DLLs in ~10 s; prints GATE OK or the errors. Your branch must end GATE OK.
Unity-only behaviour (a check's result, a film) cannot be run by you: write the check so the integrator's named run proves it, and say exactly
what it must print.

## What you return (your final message, plain text, these headings)
- BRANCH and HEAD commit; `git -C <wt> diff --stat 7c585d8f..HEAD`.
- WHAT WAS BUILT, against the plan's done-when, item by item; what is NOT done and why (never claim what you did not do).
- CHECKS: each new/changed check's name, its CheckSuite group(s) (cheap / documents / simulation) and the exact registration line to add,
  and what a passing named run must print.
- UNITY RUNS OWED: named checks, bars (per `Tools/bar_tier.ps1`'s tiers), dump/trajectory diffs, films (geometries) - the integrator runs them.
- BASELINES: whether any simulation baseline/family/sentinel can move, and why not / which.
- DECLARED READINGS and ASKS: every choice the plan leaves open that you made (with the plan words it rests on), and every question for Elias
  the item ends in (with the measurement it rides on, if the measurement needs a Unity run say so).
- DRAFT RECORDS: a COMPLETED.md section body (heading `## NNN. <TITLE IN CAPS> (2026-10-16)`, the project's record style: What was owed /
  Built / Measured / Declared / Bars: PENDING_BARS), the plan's "Built" line for the item, the feature-list row update - as text.
- `plan_prereg` if any (see above).
Be honest and exact. A half-built item reported as half-built is useful; a half-built item reported as done is a defect.
