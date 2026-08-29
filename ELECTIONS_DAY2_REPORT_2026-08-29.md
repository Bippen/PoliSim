# Elections Day-2 — the four verdicts, the re-backtest, and the gate (2026-08-29)

**Anchor verified:** HEAD `c23802a` == origin/main, `PoliSim.slnx` discarded (machine churn).
Discipline v2; R-N2 held throughout Parts 1–3; R-SP1 push per part.

# THE HEADLINE — R-EL13's GATE: **FAIL**

**Part 4 does not run. Nothing was wired.** Three of four countries improved, some of them
sharply; **Italy regressed, 5.61 → 6.69 pp**, and R-EL13's gate requires that *no* country
regress. The ruling did exactly what it was written to do: a model that got better in most places
and worse in one does not go live because a schedule wanted it to.

| country | Day-1 MAD | best Day-2 | layer | verdict |
|---|---|---|---|---|
| GERMANY-8 (like-for-like) | 5.78 | **4.66** | +§8 | IMPROVED |
| SWEDEN | 3.25 | **1.75** | +§8 | IMPROVED |
| POLAND | 6.99 | **3.84** | +§8 | IMPROVED |
| **ITALY** | 5.61 | **6.69** | +§8 | **REGRESSED** |
| *Germany-9 (§27 demo, not in gate)* | *n/a* | *4.55* | *+§8* | *see note* |

**The seat half of the gate PASSES, unchanged:** Sweden, Germany, Poland-real, Italy-PR and
USA-EC all still reproduce with **total absolute seat deviation 0**, and the deliberate
national-Poland signature run still reads exactly **70**. Wiring was gated on both halves; the
vote half failed, so the gate failed.

## A flaw in my own test, found and corrected before reporting

The first run put Germany's A at 6.96 against Day-1's recorded 5.78 and would have made §27 look
better than it is. Cause: I had added **SSW** to Germany's party list (needed for §27, since SSW
stands in exactly one Land), so run A covered nine parties while Day-1's 5.78 covered eight —
not a like-for-like comparison. Corrected by running **GERMANY-8**, Day-1's exact party set,
whose run A recomputes to **5.78 — matching Day-1 to the digit**, which is also the proof that
this harness reproduces Day-1 rather than merely quoting it. The nine-party Germany is retained
as the §27 demonstration and is **explicitly excluded from the gate**, labelled as such in the
harness output.

# Why Italy regressed — the finding, not tuned away

Italy 2018 → 2022 is the most volatile pair in the set, and §8 with a **uniform** loyalty is
exactly wrong for it:

| party | 2018 prior | 2022 actual | change | §8's effect |
|---|---|---|---|---|
| FdI | 4.35 % | 29.27 % | **×6.7** | damped DOWN to 10.01 (dev −19.25) |
| M5S | 32.68 % | 17.38 % | **halved** | held UP at 29.48 (dev +12.10) |

Loyalty at 60 asserts that ~60 % of the electorate votes as it did last time. In Italy 2022 that
is simply false — the largest party of 2018 halved and a 4 % party became the largest. The layer
is not wrong; **the global constant is wrong**, and the spec says so itself: §5 and §8 make
loyalty a **per-voter-group** attribute, not one number for a nation. Italy is the counter-example
that proves the constant has to be derived rather than assumed.

**What I did NOT do:** re-fit the loyalty constant to make Italy pass. The kickoff is explicit —
"if a single declared-parameter re-fit is obviously needed, that is a finding to report, **not** a
licence to run it before the gate" — and a constant tuned until the gate opens would make the gate
meaningless. `Loyalty = 60` was chosen a priori from the spec's own five-point scale (its middle
rung, "Lean"), applied uniformly to all four countries, and never varied.

# Part 3 in detail — where the layers helped

**§8 loyalty is a large, real improvement in three countries**, and it corrects the deviations
Day-1 named it for:

| named Day-1 deviation | before | after | via |
|---|---|---|---|
| BSW +10.18 pp (Germany, named as loyalty) | +10.18 | **+0.94** | §8 |
| TD +15.93 pp (Poland) | +15.93 | **+2.58** | §8 |
| KO −16.69 pp (Poland) | −16.69 | **−9.19** | §8 |
| M −9.97 pp (Sweden) | −9.97 | **−3.54** | §8 |
| KD +8.16 pp (Sweden) | +8.16 | **+3.86** | §8 |
| **CSU +7.36 pp (Germany, named as regional structure)** | +7.36 | **−3.68** | **§27** |
| CDU −9.31 pp (Germany) | −9.31 | −9.69 | §27 (≈flat) |

**§27 regional structure did what it was predicted to do and nothing more.** The CSU deviation —
the one Day-1 attributed to regional structure — is corrected by candidacy alone: the CSU contests
one Land of sixteen, and once the model knows that, its share falls from 13.62 % predicted to
2.57 % against 6.26 % actual. No parameter was fitted; the fix is two sourced facts (per-Land
electorate weights and per-Land party availability, both from the official `kerg2.csv`). Germany's
overall MAD improves 6.96 → 5.20 on the nine-party set.

**But §27 and §8 do not compose cleanly yet** — Germany's D (both layers) is 5.01, *worse* than
C (§8 alone) at 4.55. The reason is stated rather than patched: the regional run damps each region
toward the **national** prior, because per-Land 2021 results were not fetched. Damping a region
toward a national prior is the wrong prior for that region. **That is the next data item, not a
model defect.**

# Part 1 — the verdicts as executed

- **R-EL10 (France):** recorded as **structurally out of scope, with the reason**, in the data
  bill and the roadmap; a named future item, "France constituency model", added unsized and
  unstarted. No placeholder, no approximation.
- **R-EL11 (Italy sub-national):** billed in the data bill as "before Italy is playable, not
  before the model is trusted". Not worked today.
- **R-EL12 (Nebraska LB3): RESOLVED — NOT ENACTED.** Introduced 9 Jan 2025 at the Governor's
  request; **cloture failed 31–18 on 8 April 2025**; **indefinitely postponed 17 April 2026** at
  sine die. Companion amendment LR24CA never floor-debated and is not on the 2026 ballot; the
  citizen-initiative route was withdrawn June 2026. **The district method stands and §32-1038 is
  unchanged**, so no variant was needed — but R-EL12's forward half is recorded: if it ever
  changes it becomes a **dated rule variant**, and `ElectoralCollege.Jurisdiction` is already
  shaped for that (WTA and district-method are two constructors over one type). ⚠ The finding has
  an **expiry**: the 110th Legislature convenes January 2027 and the Governor intends to keep
  pursuing it before 2028. ⚠ Sourcing gap stated: the Legislature's own host refused connections
  and the Internet Archive is tool-blocked, so the verdict rests on three agreeing independent
  lines (the Legislature's news service, the aggregator's action history, press) rather than the
  journal of record.

# Part 2 — what was built

`RegionalVoteModel.cs` (pure, unwired): the national vote as a weighted sum of regions, each with
its real electorate size and its real party availability, plus a loyalty-aware variant. Sourced
data: `ElectionsData/germany/land_votes_2025.csv` — per-Land absolute Zweitstimmen from the
official `kerg2.csv`, whose sums were cross-checked against the national figures exactly
(valid 49,649,512; CDU 11,196,374; CSU 2,964,028). The file's own zeros carry the candidacy facts:
**CDU = 0 in Bayern, CSU = 0 in the other fifteen, SSW nonzero only in Schleswig-Holstein.**

**Deliberately NOT done:** per-region electorate *positions*. Fitting those against regional
results would be circular in a backtest, so every region runs the national electorate and any
improvement §27 shows is structure, not tuning. The type carries an optional override for the day
a non-circular source exists.

# R-N2 — held

Trajectory dump exit 0, the six baselines **byte-identical to `traj_v31bf_*` 6/6 by SHA-256**, all
eight checks exit 0. Nothing of the election system is reachable from any gameplay path;
`ElectionNoise` remains a named stream nothing in the live game draws from.

# RULINGS NEEDED

1. **Loyalty must be derived, not assumed — how?** The honest options, in ascending cost:
   (a) per-country loyalty from measured volatility (e.g. the Pedersen index of the previous two
   elections — sourced, not fitted); (b) per-voter-group loyalty as §5/§8 actually specify, which
   needs the voter-group layer to exist first; (c) accept a global constant and accept that
   volatile elections model badly. **My recommendation is (a)** — it is sourced, cheap, and turns
   Italy from a failure into a test.
2. **Per-region priors** (Germany 2021 by Land, and the equivalents) — the item that would let
   §27 and §8 compose. Cheap: the same `kerg2`-style open data for the previous cycle.
3. **Re-run the gate after (1) and (2)?** R-EL13's gate was written for today's run. Say whether
   the same gate re-applies to the next attempt, or whether wiring wants a different bar.
4. **13 September** is 15 days out and the gate did not open. The Swedish minimum is unaffected —
   the allocator is exact and proven, the seed data is sourced — but the vote model does not wire
   today, so Sweden's gate remains a re-seeding exercise, not a playable election.

# State at close

Commits: Part 1 + 2 + 3 below; R-N2 proven at the boundary; **Part 4 skipped by rule, with no
revert handle because nothing was wired.** Parts 5's records follow.
