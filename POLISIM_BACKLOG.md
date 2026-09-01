# PoliSim — the backlog register

**What this is.** THE single ordered register of open work. Built 2026-08-31 (C-0.1 of the clearance
list) from every source that held a backlog: the clearance list, the elections work list (retired at C-G1, 2026-08-31; its 46 rows are closed and its record is `ELECTIONS_PROTOTYPE_LOG.md`),
the Playtest-1 work list (retired at C-G1, 2026-08-31; every row points at its `COMPLETED.md` section), `MISSING_PREREQUISITES.md`, `POLISIM_MASTER_ROADMAP.md`'s live-work section and
trigger shelf, `ELECTIONS_PLAY_CALIBRATION.md`, `CLAUDE_DESIGN_ASSET_REQUEST.md`, `ELECTIONS_GAP_TABLE.md`,
and the riders recorded this week that no item owned.

**The rule this file exists to enforce:** every open item appears **exactly once**, here. A source
document may describe an item; it may not also queue it. Work that is *finished* goes to `COMPLETED.md`;
work that is *waiting on a named party* keeps its detail in `MISSING_PREREQUISITES.md` but its ROW is here.

**The repo outranks this file.** Where a row says open and `git log` says done, the log wins and the row is
corrected, not re-worked. Every row marked closed cites its commit.

**Columns.** ID · what · done-when · owner · class · depends on.
**Owner:** CODE (a session) · ELIAS · DESIGN · CALENDAR.
**Class:** SAFE (no trajectory move expected) · BASELINE (moves a trajectory; needs a new explained family,
per country) · RULING-BLOCKED (cannot start until someone rules) · WATCH (a standing guard, never a task) ·
TRIGGER (real work whose trigger has not fired) · DEFERRED (recorded, deliberately not built).

---

## 0. Rulings taken 2026-08-30, before this pass ran

These four changed what the pass builds. Recorded here so no row below reads as still-open.

| ruling | what was ruled | what it unblocks |
|---|---|---|
| **R-CL1** | **The player has a party.** The player picks one of the country's real seeded parties at country selection; personal approval and party approval are separate stocks; losing office is not game over. | the rail cell, the win/lose rule, C-B5's gate, and the play-calibration list's whole premise. Executed as Track R. |
| **R-CL2** | **`eu_position` is ruled in as the openness axis** for the Trade bill's vote, recorded as a named ruling with its stretch stated — EU integration standing in for trade openness is an approximation and is tagged as one. | C-B3 |
| **R-CL3** | **§38 carry-over is BUILT**, `SaveVersion` 2 → 3, with the standing electorate gap stated rather than papered over. | C-D4 |
| **R-CL4** | **The two `StatNodeId` members go in now**, and a missing icon becomes a reported GAP rather than a check failure, on `PartyMarkCoverageCheck`'s own precedent. | §E4, C-F1 |

---

## 0b. Pre-rulings for the rest of the list (Elias, 2026-08-31)

**Each is strikeable; all are binding unless struck.** They exist so no session stalls mid-track waiting
on a ruling — the process correction of the same date moved review to **track** boundaries, and these are
what make that safe.

| row | the ruling |
|---|---|
| **C-C8** | If the model holds no fact about a pair, the page **shows the absence explicitly** and the gap becomes a C-F1 line. Trade volume comes from the map's own data; compass positions compare as drawn. **No relations score, no derived affinity presented as a fact.** Prev/next browsing; three country pages minimum. |
| **C-C9** | Compute incrementally at the turn boundary, same seed, player-independent events shared. ⚠ **If the per-turn cost exceeds a stated budget, report the cost and ship behind a flag rather than optimising blind — a MEASURED cost is the deliverable, not a fast one.** |
| **C-C10** | If the divergence cannot be attributed to enacted changes within tolerance, **report the residual as a named finding rather than forcing the sum. An honest residual beats a false identity.** |
| **C-C11** | **No constant moves.** Where the literature disagrees, report the range. Where the model sits outside every sourced estimate, **say so plainly — that is the finding.** |
| **C-C12 / C-C13** | **Documents only.** C-C13 recommends **5-year cohorts** unless the sourcing says otherwise, and carries the collision map plus the "one demography, two consumers" join. |
| **C-C14** | Remove the authored ±5–10 %; **do not re-roll, do not stabilise.** The point with its scope stated, matching C-C1. |
| **C-D1** | Source SCB per-valkrets marginals **if reachable under the cross-check gate**; otherwise bill the exact series and close as billed. **Never derive from data that does not exist.** |
| **C-D2** | Measure the pool a playable eight-party field needs; **propose, do not apply.** |
| **C-D3** | ⚠ **RULED: the model carries BOTH språkrör.** The debate seats the one the party's own statutes or its published campaign materials put forward; **if neither resolves it, seat neither and state the absence. Never silently drop a real named person.** |
| **C-D4** | Build the §38 carry-over **if the persisted `ElectionRecord` supports it cheaply**; otherwise defer by name with its trigger. |
| **C-D5** | Implement the swing column now that a record persists. |
| **C-C5** | Stays blocked on its bill. **If ECB reference rates are reachable under the cross-check gate, take them** — one vintage, one stated derivation, all three pairs — and unblock it; otherwise leave it billed and continue. |
| **S-14** | ⚠ **RULED, ruling-first row: campaign money is a SEPARATE PURSE in national units, and the two never transact unless Elias rules otherwise. Do NOT build the join.** |
| **S-15** | Content item, filed; **not improvised inside another item.** |

**Unchanged and explicitly not to be sped up:** measure the premise before fixing it · evidence must bind
where the change lives (C-C2's precedent) · BASELINE trajectory changes explained per country with their
causation **demonstrated, not asserted** (C-C7's precedent) · never tune to pass a gate · never invent a
figure · C-G1 deletes nothing that is not first migrated.

---

## D. DECISIONS — five questions, one sitting (prepared 2026-08-31)

**Every one of these is prepared to the edge and stopped there. No option below has been taken.** Each
sheet is: the question in one sentence · the options with what each costs and forecloses · the
recommendation and its basis · **the one line to write to rule it.** Writing that line into this file *is*
the ruling.

---

### D-1 · The campaign pool

**The question.** A mandate-proportional split of the campaign pool bankrupts five of eight parties — do we
raise the pool, keep chests equal, change what a campaign costs, or wait for real figures?

| option | cost | what it forecloses |
|---|---|---|
| **a. Raise the pool to the measured floor, 88.2 M kr (×4.59)** | one item; ⚠ re-opens 2a-iv and both PEND lines, because every party's affordable set changes | nothing permanently — but it scales one `[AUTHORED-DRAFT]` figure by another, and makes the next sourcing job harder to justify |
| **b. Keep chests equal; treat *mandatbidrag* as a shape the game does not yet fund** | nothing — today's state | the sourced funding SHAPE stays unused; the campaign keeps a known-wrong equality |
| **c. Scale a party's OFFICE PLAN to what it can afford** | one item, inside W-B4's staging | nothing; it can be undone. ⚠ Changes AI behaviour, so 2a-iv is re-measured |
| **d. Source Kammarkollegiet and stop scaling authored numbers** | unknown — the register is public but its API does not answer an ordinary request | nothing; it is the standing bill either way |

**Recommendation: (c).** Basis, measured at C-D2: the tension is **8.4×** between the pool a party needs on
its mandate share (C 4.63 M, MP 38.98 M), and the driver is the **office network** — V and MP spend
1.91 M kr on offices against 0.10 M on payroll — which is a *personality choice uncorrelated with seats*.
⚠ **(c) removes the tension at its source and invents no money.** (a) is the only option that authors a new
number, and it authors a big one.

> **To rule it, write:** `D-1 RULED: (c) — scale the office plan to what a party can afford.` *(or a / b / d)*

---

### D-2 · The tax channel's remaining calls

**The question.** C-N4's disposable-income term is built on a **US** MPC and reaches households only — do we
source a Swedish anchor, give corporate tax a channel, and give the tax base a per-country share?

| option | cost | what it forecloses |
|---|---|---|
| **a. Take all three** | three items: a Swedish/euro-area MPC · a corporate-tax channel · per-country `BaseShareOfGdp`. All three BASELINE | nothing; each is separable |
| **b. Source the MPC only** | one sourcing item (Riksbank WP 365 / KI 2021:25) | leaves corporate tax at a 0.000 multiplier and the tax base uniform across six countries |
| **c. Per-country base share only** | one sourcing item, six figures | leaves the MPC American |
| **d. Nothing yet — the channel works and its limits are recorded** | nothing | nothing; every limit is already written at its call site |

**Recommendation: (b), then (c), then the remainder of (a).** Basis: ⚠ **the MPC is the one number in the
term that is foreign**, and the term's whole defence is that it is sourced. The per-country base share is
the next-largest honesty gap — **a +10-point rise moves consumption by −2.68 % of GDP in all six countries
identically**, because `BaseShareOfGdp` is per-tax-type. Corporate tax comes last because it is a
*different channel* (firms, not households), not a refinement of this one.

> **To rule it, write:** `D-2 RULED: (b) first, then (c). Corporate tax is a separate item.` *(or a / c / d)*

---

### D-3 · The tax spec-let

**The question.** Three of the six countries do not fit a bracket table — do they get a stated
approximation, or does `TaxLine` get pluggable schedules?

| option | cost | what it forecloses |
|---|---|---|
| **a. Brackets as data; DE / FR / IT approximated, said so on screen** | ~8 sessions | ⚠ the three are misrepresented for as long as it stands, with an `[AUTHORED-DRAFT]` note as the only defence |
| **b. Pluggable schedules — formula, quotient familial, three layers** | ~11 sessions | nothing; it is the correct shape |
| **c. Neither yet** | nothing | nothing — ⚠ and note the spec-let is **downstream of D-4 anyway** |

**Recommendation: (c) now, (b) when it runs.** Basis: ⚠ **a bracket schedule applied to a single average
income is arithmetically identical to a flat rate**, so the whole item is blocked on the cohort substrate
whichever branch is chosen — and given that wait, buying (a)'s speed costs three misrepresented countries
for no time gained. Germany's tariff is a *formula* (§32a EStG); no bracket table represents it.

> **To rule it, write:** `D-3 RULED: (b) pluggable schedules, when D-4 unblocks it.` *(or a / c)*

---

### D-4 · The cohort spec-let

**The question.** Do we build the five-year cohort substrate — which unblocks the tax spec-let, the voter
groups, C-D1 and the Italy FdI ceiling — or leave the electorate a single group?

| option | cost | what it forecloses |
|---|---|---|
| **a. Build it (P-I2)** | ~11–13 sessions, ⚠ of which retiring eight `EconomyState` scalars is the half that can go wrong quietly. **The largest BASELINE family attempted** | nothing. It is the unlock for four other items |
| **b. Build it with single-year cohorts** | +2–3 sessions; 606 numbers a year instead of 126 | nothing, but it invites single-year *rates*, which is a demography build rather than a game substrate |
| **c. Not yet** | nothing | ⚠ **C-D1, the voter groups, per-group loyalty and the FdI ceiling all stay blocked** — that is one chain, and this is its first link |

**Recommendation: (a).** Basis: it is the single highest-leverage item on the register — **four items
downstream of one ruling** — and five-year is what the sourcing fits (`demo_pjan` publishes single-year
ages, so the choice is fitness rather than availability; none of the four consumers can tell 42 from 43).

> **To rule it, write:** `D-4 RULED: (a) build the 5-year cohort substrate as specced.` *(or b / c)*

---

### D-5 · The player's campaign: war chest and win/lose

**The question.** The eight Track E screens are unreachable because nothing in the game builds a
`CampaignSnapshot` — do we wire the campaign to the game loop, and is losing office game over?

| option | cost | what it forecloses |
|---|---|---|
| **a. Wire the campaign; game over ONLY on leaving office** | one large item; ⚠ needs D-1's war-chest figure, and re-reads `ScenarioEvaluator` for collisions | nothing; it is R-CL1's own destination |
| **b. Wire the campaign; keep today's approval-threshold game over** | slightly less | ⚠ contradicts R-CL1, which already ruled that losing office is not game over |
| **c. Rail cell only; campaign still harness-only** | small | the eight screens stay unreachable — the cell would point at nothing |
| **d. Not yet** | nothing | Track E's eight screens stay built-and-unreachable, which is where they have been since W-G1 |

**Recommendation: (a), after D-1.** Basis: ⚠ **R-CL1 already ruled that losing office is not game over**, so
(b) reopens a settled question. (a) needs a war-chest number, which is D-1 — **so D-1 is genuinely first**,
not merely tidier. The model half of R-CL1 is already done: the party exists, persists and survives a save.

> **To rule it, write:** `D-5 RULED: (a), sequenced after D-1.` *(or b / c / d)*

---

---

### ⚠ THE SELF-RULED SHEETS, RE-READ AGAINST THE REPO — 2026-09-01

**Why this exists.** D-6 through D-12 were decided and logged strikeable during an unattended run. ⚠ **A
self-ruled decision nobody ever re-reads is an unreviewed decision**, so each was re-read against the repo
as it now stands — not re-litigated. Verdicts: **HOLDS** (still reads right on the evidence) ·
**RECONSIDER** (something has changed; what) · **NEEDS ELIAS**.

| sheet | verdict | why, against the repo of 2026-09-01 |
|---|---|---|
| **D-6** · aging-step rate sources, (a) uniform cohort-change ratios | ✅ **HOLDS** | Its premise was *no US components of change by age exist*, and §142 did not disturb it: what §142 found is a **projection** file (`np2023_d1_mid.csv`, stock by single year of age for future years), not `q(x)` and not migration by age. ⚠ **A projected stock separates mortality from migration no better than an observed one**, so (a)'s stated cost — `Survival` is deaths and net migration together — is unchanged, and so is its reversibility |
| **D-7** · immigration age profile, (a) DHS LPR as a named US proxy | ⚠ **RECONSIDER — the decision holds, its STATUS does not** | The 21 US numbers and the EU five's `agedef=REACH` profiles are still right and still sourced. But §141 reverted the step they were re-pointed into: `CohortStepRateTable` is now **UNWIRED ENTIRE** in `UnwiredSubsystemCheck` (2 entry points uncalled, named in 0 game files). ⚠ **The sheet reads as though the lever has somewhere to go, and it does not** — it is a precondition of the stage-3 rebuild, not a description of live code |
| **D-8** · MPC held at 0.67 | ⚠ **RECONSIDER — for a reason D-13 created after it** | Its own basis is intact: the spending multiplier is invariant to the constant, and every lower value moves the tax multiplier further from Romer & Romer. ⚠ **But D-13 has just shown the enforced denominator is not the quoted band's quantity on the spending side, and the tax side has the same shape**: Romer & Romer's −2 to −3 is per **exogenous** tax change of 1 % of GDP, while our tax impulse is the **realised** revenue change, net of the endogenous response. **The comparison D-8 rests on has not been checked the way D-13 checked the spending one.** Sized as a rider below, not re-decided here |
| **D-9** · per-country tax base shares | ✅ **RULED BY ELIAS 2026-09-01 (a)** | See the sheet above; no longer self-ruled |
| **D-10** · wire `TacticalVoting`, (a) | ✅ **HOLDS, and is now the loudest row in a check** | `UnwiredSubsystemCheck` reports `TacticalVoting.cs` **UNWIRED ENTIRE** in every run. Nothing since has given coverage or momentum a route to the ballot, and no cheaper bridge appeared |
| **D-11** · give door-to-door a job, (c) | ✅ **HOLDS — and §134 strengthened it** | (a)'s premise was *"the office operation is the ground game"*. §134 then scaled office plans to what a party can keep and **dropped six offices, landing on V, MP and L**. ⚠ **So for exactly the small parties, (a) would retire the verb and leave nothing behind it.** The measured defect is unchanged: three local actions hold the largest hour costs against the smallest reaches while §33 scores per hour |
| **D-12** · stage 3's time base and the two inseparable rates | ⚠ **RECONSIDER — both halves now govern code that does not exist** | §141 reverted the step, and `BaselineDeathRate` — the seeded split (2a) rested on — is **gone from the tree** (`grep`: no occurrences). The reasoning is untouched and both halves should bind the rebuild, but ⚠ **the sheet is written in the present tense about a step with no caller**, which is how a decision quietly becomes a claim |

⚠ **The pattern across the three RECONSIDERs is one pattern, not three:** D-7, D-8 and D-12 are all sheets
whose *decision* survives and whose *world* moved underneath them — twice by §141's revert and once by
D-13's finding. **None is re-decided here.** Re-deciding a sheet because its subject was reverted would
be re-litigating; restating what it now binds is the review's whole job.

**Rider opened by this review (§11):** **R-D8** — apply D-13's test to the tax side. Romer & Romer
normalise on the exogenous tax change; the harness divides by the realised balance change. Cheap: the same
runs already hold both quantities. ⚠ **It is a measurement, not a re-decision of D-8**, and it must not
move a constant.

---

### D-6 · The aging step's rate sources ⚠ DECIDED AND TAKEN (R-N1), strikeable · ✅ **HOLDS** (re-read 2026-09-01)

**The question.** Eurostat publishes life tables, fertility and migration by age for the EU five and
**nothing for the USA** — so does the aging step take the best data for five countries and something else
for the sixth, or one uniform method derived from each country's own published stock?

⚠ **Measured before deciding, not assumed.** `demo_frate`, `demo_magec`, `demo_mlifetable`, `migr_imm8`
and `migr_emi2` were all fetched and all answer for the EU five. For the USA: SSA's actuarial life table
refuses an ordinary request (**HTTP 403**), the Census PEP API needs a key, the CDC data portal carries
life expectancy and state tables but **no national q(x) by single year of age**, and the Census
`nc-est2024-alldata-*` files are stock by age/race/sex with **no components of change by age**.

| option | cost | what it forecloses |
|---|---|---|
| **a. One uniform method — cohort-change ratios from each country's own two consecutive published stocks** | nothing beyond the fetch; already done | ⚠ **mortality and net migration are not separable**, so the immigration lever cannot hook through the survival array and needs an additive age-profiled term |
| **b. Eurostat life tables for the EU five; the USA on cohort change** | one item | ⚠ two demographic models wearing one name — the USA's levers would behave differently from the other five for a reason no player could see |
| **c. Keep hunting for a US life table (HMD registration, NCHS PDF extraction, UN WPP token)** | unknown; three dead ends already | nothing, but it blocks the whole chain D-4 opened on an administrative errand |
| **d. Bill the rates and stop stage 2** | nothing | ⚠ the substrate that cannot age is a table, not a model, and four items wait behind it |

**Recommendation, TAKEN as an R-N1 decide-and-log: (a).** Basis: the project's own idiom permits an
asymmetry **only when the alternative is inventing data** — C-B3 gave the USA the fiscal axis because
GPS 2019 has no EU item, and there was no second option. Here there *is* a uniform option, and it is
derived from each country's own publisher with no third-party recall. ⚠ **Its cost is real and is
written at the call site**: `Survival` is deaths and net migration together, so re-pointing the
immigration lever is a separate stage with its own hook. **Reversible** — if a US life table becomes
readable, the survival array splits into mortality and migration without changing the step's shape.

⚠ **The decision paid for itself immediately.** Under (a) the step can be **hindcast against a published
year it was not fitted to**, and that assertion caught a real double-count in the open 100+ band —
~50 % in all six countries — that no long-run plausibility check would have shown. Options (b) and (c)
have no such test available for the USA.

> **To strike it, write:** `D-6 STRUCK: (b)` *(or c / d)* — the survival arrays are re-derivable from
> data already fetched, so a strike costs one item and no re-fetch.

---

### D-7 · The immigration lever's age profile ⚠ DECIDED AND TAKEN (R-N1), strikeable · ⚠ **RECONSIDER: its status, not its decision** (re-read 2026-09-01)

**The question.** D-6 made the survival ratio deaths and net migration *together*, so the immigration
lever has nothing inside the step to scale and must add people **on top** of it — across which age bands?

⚠ **Measured before deciding.** `migr_imm8` under `agedef=COMPLET` gives single-year detail for Sweden,
Germany, Italy and Poland and **a total with no detail for France** — so the obvious fetch would have left
four countries sourced and two on a stand-in. Under **`agedef=REACH`** all five reconcile to their own
published totals exactly. For the USA there is no equivalent series at all: the Census PEP `alldata`
files are stock only, and the API needs a key.

| option | cost | what it forecloses |
|---|---|---|
| **a. Sourced for the EU five; the USA on DHS LPR new arrivals as a named proxy** | one fetch and one XLSX parse; done | ⚠ the US figure is a **subset** of immigration — it excludes temporary and unauthorized entry — so it is a proxy for the SHAPE and says nothing about the level |
| **b. Sourced for the EU five; the USA uniform across 15–64, marked interim** | less | ⚠ *"migrants are young"* is the spec-let's own warning, and a uniform split **quietly ages the population wrongly** — the USA would drift differently from the other five for a reason no player could see |
| **c. Sourced for the EU five; the USA borrows the EU five's mean profile** | none | ⚠ it reads as sourced and is not: it is another continent's migration attributed to the USA |
| **d. Bill it; the immigration lever stays dead** | nothing | ⚠ **a third dead lever**, after S-18's interest rate and C-C11's tax dials — and this one the spec-let predicted in writing |

**Recommendation, TAKEN as an R-N1 decide-and-log: (a).** Basis: the profile is used **as a shape only**,
and a real US age distribution of real arrivals is a better shape than a flat line (b) or another
continent's (c) — **and unlike both, its limitation is checkable by anyone who opens the source.** DHS
Table 8 New Arrivals FY2024 gives 581 290 arrivals by age band; the two allocations needed above 65
(where DHS publishes wider bands) govern **9.7 %** of the profile and are made in proportion to the USA's
own population.

⚠ **Both levers were then PROVEN live before being wired** — C-N3's method applied before the fact rather
than after. A +0.1 M migration setting delivers 0.1 M in all six (23–47 % of it into ages 0–24, which is
the shape doing its job), and fertility ×1.5 raises births by exactly half. **The fertility assertion was
proven able to fire**: set to ×1.0 it goes red in every country.

> **To strike it, write:** `D-7 STRUCK: (b)` *(or c / d)* — the EU five are sourced either way; only the
> USA's 21 numbers change.

---

### D-8 · The MPC, after sourcing it ⚠ DECIDED AND TAKEN (R-N1), strikeable · ⚠ **RECONSIDER: the tax side is untested for D-13's defect** (re-read 2026-09-01)

**The question.** D-2 (b) ruled *"source the MPC"* on the grounds that it is the one foreign number in
C-N4's term. It has been sourced. **Does the model move to the European figure?**

⚠ **D-2's own named source is the wrong paper.** It cited *"Riksbank WP 365"*. WP 365 is **"The
Interaction Between Fiscal and Monetary Policies: Evidence from Sweden"** — not a consumption study, and
it carries no MPC. A citation recalled rather than opened is an invented figure in a technical costume,
and this one was recalled by this register.

**What the Swedish evidence actually says**, read not recalled: *Identifying the MPC-Liquidity Gradient in
High-Quality Data* (arXiv 2607.07055, July 2026), Swedish administrative tax registers — annual MPC falls
from **0.7 in the lowest cash-on-hand decile to 0.3 in the top**; **Households-sample average annual
total-expenditure MPC bounded at 0.54–0.66** (nondurable 0.36–0.44). Total expenditure is the right
comparison: national-accounts consumption includes durables. ⚠ It is a **preprint, v1, not peer-reviewed**.

**Measured at three values before choosing** (audit harness, Sweden, seed 777):

| MPC | tax multiplier L / L+1 / L+4 | spending multiplier |
|---|---|---|
| **0.67** (US, Johnson/Parker/Souleles, AER 2006) | 0.485 / 0.682 / 0.760 | 0.603 / 0.850 / 0.959–0.966 |
| 0.60 (Swedish bracket midpoint) | 0.428 / 0.602 / 0.671 | **identical, to the digit** |
| 0.54 (Swedish bracket floor) | 0.380 / 0.535 / 0.596 | **identical, to the digit** |

| option | cost | what it forecloses |
|---|---|---|
| **a. Hold 0.67, record the bracket** | none | nothing — the choice is measurably free and revisitable |
| **b. Move to 0.60** | one edit | ⚠ widens the tax-multiplier gap to Romer & Romer, which the model already undershoots (⚠ by four to six, corrected at R-D8 — not the threefold read off the enforced denominator) |
| **c. Move to 0.54** | one edit | idem, more so |

**Recommendation, TAKEN: (a).** Basis, all measured: **the spending multiplier is invariant to this
constant to the digit**, so the hard constraint is not in play and the two channels are separable — the
choice can be revisited at any time at no cost. The peer-reviewed source outweighs the preprint. And ⚠
**against the intuition that a European figure would improve the model, every lower value moves the tax
multiplier further from Romer & Romer** — the weak channel is the tax channel, and lowering the MPC
weakens it. **The Swedish paper itself cites JPS's two-thirds and calls its own estimates "on the lower
end compared to the literature on tax rebates"**, so the disagreement is methodological and known;
C-C11's standing ruling for that case is *report the range*, which is now what the constant does.

> **To strike it, write:** `D-8 STRUCK: (b)` *(or c)* — one constant, one line, and the measurement above
> already tells you what it will do.

---

### D-9 · Per-country tax base shares ⚠ BUILT, MEASURED, AND REJECTED BY THE HARD CONSTRAINT · ✅ **RULED (a) BY ELIAS 2026-09-01**

**The question.** D-2 (c) ruled *"per-country base share"* to fix C-N4's finding that **a +10-point tax
rise moves consumption by −2.68 % of GDP in all six countries identically**. It was built and sourced.
**The hard constraint rejects it. Does the constraint hold, or does it bend?**

**It was fully built, not sketched.** OECD Revenue Statistics
(`OECD.CTP.TPS,DSD_REV_COMP_OECD@DF_RSOECD`, general government, % of GDP, 2022) for income `T_1110`,
corporate `T_1210`, VAT `T_5111` and payroll `T_2000+T_3000`; ⚠ **Poland from Eurostat `gov_10a_taxag`
D51A/D51B**, because the OECD flow reports **no income rows for Poland in any of 2020–2023** — checked
across four years. Base = (revenue % of GDP) / (seeded rate %):

| | income | corporate | VAT | payroll |
|---|---|---|---|---|
| USA | 0.3077 | 0.0955 | — | 0.3929 |
| Germany | 0.2317 | 0.0772 | 0.3860 | 0.3673 |
| France | 0.2154 | 0.1139 | 0.3745 | 0.2469 |
| Italy | 0.2491 | 0.1106 | 0.3151 | 0.4257 |
| Poland | 0.1406 | 0.1474 | 0.3132 | 0.3775 |
| Sweden | 0.1998 | 0.1675 | 0.3798 | 0.4488 |

*(against uniform authored 0.4 / 0.15 / 0.5 / 0.4)*

⚠ **THE MEASUREMENT THAT REJECTED IT.** With the table wired in, the spending multiplier moved
**0.603 / 0.850 / 0.966 → 0.593 / 0.838 / 0.951**. **0.593 is below Ramey's 0.6.** The standing rule is
pre-committed and unambiguous — *"any proposed fix that moves it out of Ramey's 0.6–1.0 band is rejected
by that fact alone"* — so it was reverted, and the revert restored 0.603 / 0.850 / 0.959–0.966 exactly.

⚠ **The argument FOR bending, stated fairly rather than suppressed:** the model's **GDP response did not
change at all** (1.37 / 1.93 / 2.17 before and after). Only the measured *impulse* grew, 2.27 → 2.30,
because a different revenue path shifts the GDP that 2 % of G is taken from. **What moved is the
denominator, not the behaviour.** That is a real argument that the constraint is policing a measurement
artifact at the third digit — **and it is not mine to act on.** A pre-committed rule that yields to the
first change that trips it is not a rule.

| option | cost | what it forecloses |
|---|---|---|
| **a. Constraint holds; leave it rejected** | the −2.68 % identical-across-six finding stands | nothing — the table is recorded in full above and rebuildable in one item |
| **b. Constraint holds at L+1/L+4 only; accept 0.593 at impact** | one item to re-land | ⚠ narrows the rule to two of three horizons, permanently |
| **c. Re-state the constraint as "the GDP response must not move"** | one item, plus re-stating the rule | ⚠ a weaker rule than the one that has held this pass three times |
| **d. Take it and re-anchor the spending channel too** | a BASELINE item on top of a BASELINE item | ⚠ two families landing together cannot be explained apart |

**Recommendation was (a) for now, (c) as the right long-run shape** — and ⚠ **no option was taken by the
run.** This was the one fork deliberately not self-ruled: every other was reversible and mine to log, and
this one asked whether **Elias's own pre-committed constraint** bends.

### ✅ **D-9 RULED (a) — Elias, 2026-09-01. The constraint holds at ALL THREE horizons.**

> *"Ramey's 0.6–1.0 is a band on the multiplier as a quantity, not on the horizons where a given change
> happens to bite. Relaxing it precisely where a proposed fix violates it is moving a bar to pass."*

**D-2 (c) stays reverted.** Option (b) is refused explicitly: narrowing the rule to the two horizons where
the behaviour did not change is the same move as bending it, spelled differently. The per-country base
share table stays recorded in full in this sheet and rebuildable in one item.

**The two routes that remain legitimate for the argument D-2 (c) was making**, recorded with the ruling:

| route | what it would have to show | status |
|---|---|---|
| **(a) a mechanism that achieves the same end without costing spending transmission** | per-country differentiation of the tax→consumption channel that does not move the revenue path, e.g. the table applied only inside `HouseholdTaxBurdenShare` and not in `GetTotalTaxRevenue` | ⚠ **NOT TAKEN, and the objection is structural, not budgetary.** The table is *derived from revenue* — base = (OECD revenue % of GDP) / (seeded rate %) — so using it everywhere **except** revenue is using a revenue-derived number in the one place it was not derived for, and leaves the same tax with two different bases. It is also **downstream of route (b)**: if the basis question below resolves the other way, this route's premise disappears entirely |
| **(b) a sourced case that our landing-year figure measures something other than what Ramey measures** | that the harness's denominator is not Ramey's | ✅ **TESTED AND TRUE — and it does not rescue D-2 (c). It opens something larger.** See below |

#### ⚠ Route (b), MEASURED 2026-09-01 — and the finding is the opposite of a rescue

`ResponsivenessAuditHarness` forms `multiplier = ΔGDP / −Δ(budget balance)`. Ramey (JEP 33(2), 2019)
reviews **ΔY/ΔG** — output per unit of government *purchases*. These are not the same quantity, because
`−Δbalance = ΔG − ΔRevenue` and the extra output raises revenue. Measured, Sweden, seed 777:

| dial | impulse (balance) | impulse (G) | balance basis L / L+1 / L+4 | **purchases basis L / L+1 / L+4** |
|---|---|---|---|---|
| Spending +2 % | 2.267 | 2.695 | 0.603 / 0.850 / 0.959 | **0.507 / 0.715 / 0.807** |
| Spending +10 % | 11.331 | 13.474 | 0.603 / 0.852 / 0.966 | **0.507 / 0.716 / 0.812** |
| Spending −10 % | −11.338 | −13.471 | 0.603 / 0.850 / 0.959 | **0.507 / 0.715 / 0.807** |

⚠ **On the purchases denominator the model's impact multiplier is 0.507 — below the 0.6–1.0 band — and
that is true TODAY, with no per-country base table and nothing pending.** The impact horizon clears 0.6
only because the denominator is 16 % smaller than the spending change.

So route (b) is *established* and is **no help to D-2 (c) whatsoever**: it does not lift the model over the
bar, it shows the bar has been measured on a flattering quantity. ⚠ **The ruling above is strengthened, not
weakened, by its own escape route** — and the honest consequence is not a re-land of D-2 (c) but a new
question about the rule itself, which is D-13.

**What was done with the finding.** The other bases are now **printed by the audit harness beside its own,
reported and never enforced** (`ResponsivenessAuditHarness`, second table). Enforcement stays on the basis
the rule was pre-committed on. ⚠ **A denominator swapped after a gate rejected a change is moving the bar
to pass; a denominator measured and printed beside the one in force is evidence.** It lives in the
enforced instrument rather than in a side diagnostic, because §140's fifth sweep is exactly the lesson
that a proven thing nothing calls is a hole — a standalone `SpendingImpulseBasisDiagnostic` was written,
used to take the measurement, and **deleted** once the harness carried it.

---

### D-13 · Which denominator the spending constraint is enforced on ✅ **RULED (b) BY ELIAS 2026-09-01 — enforcement is on the CUMULATIVE column**

**The question.** The pre-committed rule rejects any change that moves the spending multiplier outside
Ramey's 0.6–1.0. **The band and the column it is enforced on are not the same quantity.** Which one is the
constraint enforced on?

#### ⚠ (d) was TAKEN FIRST, because it is the half that needs no ruling — and it changes the options

The register's own precedent is that a citation recalled is an invented figure in a technical costume
(D-2 (b), Riksbank WP 365). **So the paper was opened rather than recalled.** Ramey, *JEP* 33(2), Spring
2019, `econweb.ucsd.edu/~vramey/research/Ramey_Fiscal_JEP.pdf`, fetched 2026-09-01 (HTTP 200, 808 KB) and
text-extracted locally — ⚠ **no PDF reader on this machine**, so the streams were inflated directly and
the sentences below are verbatim from that extraction:

> *"For multipliers on general government **purchases** … The bulk of the estimates across the leading
> methods of estimation and samples lie in a surprisingly narrow range of 0.6 to 1."*

> *"…the present discounted value of the output response over time divided by the present discounted value
> of the government spending response over time to the shock. In most applications, different interest
> rates used for this present discounted value — including the use of a zero discount rate — give nearly
> identical multipliers."*

> *"…the quantities they calculated were not true dynamic multipliers; instead, Blanchard and Perotti
> calculated multipliers as the ratio of the output response at a particular horizon, or at its peak, to
> the impact effect of the shock on government spending."*

⚠ **That is two mismatches, not one.** The band is over (i) government **purchases** as the denominator
and (ii) a **cumulative** response, and the harness's enforced column is neither — it divides one horizon's
output response by the change in the **actual budget balance**. Worse, the obvious "fix" — ΔGDP(h)/ΔG(L) —
is the exact quantity Ramey attributes to Blanchard and Perotti and declines to call a true multiplier.

**All three, now measured and printed by the harness** (Sweden, seed 777):

| basis | L | L+1 | L+4 | comparable to Ramey's band? |
|---|---|---|---|---|
| **enforced** — ΔGDP(h) / −Δ(budget balance at L) | 0.603 | 0.850 | 0.966 | ⚠ **no** — no published family divides by the change in the actual balance |
| quasi — ΔGDP(h) / ΔG(L) | 0.507 | 0.715 | 0.807 | ⚠ no — Ramey names this Blanchard–Perotti's and excludes it |
| **cumulative** — ΣΔGDP / ΣΔG from L, undiscounted | **0.507** | **0.607** | **0.702** | ✅ **yes** |

⚠ **THE MODEL ON THE COMPARABLE QUANTITY: 0.507 / 0.607 / 0.702 — below the band at impact, inside it from
L+1 onward.** The zero discount rate is Ramey's own allowance, quoted above, not a convenience.

| option | cost | what it forecloses |
|---|---|---|
| **a. Nothing changes; all three printed, enforcement stays on the balance basis** | none — **this is what is in force** | ⚠ the model's Ramey-comparable impact multiplier stays **outside** the band it is nominally held to, visible in every run and enforced on nothing |
| **b. Enforce on the CUMULATIVE column** | ⚠ **the model fails its own constraint at impact on day one** (0.507), and every fiscal item inherits an open failure | nothing permanently — but it converts a passing gate into a standing red, which is a decision about what the suite means |
| **c. Enforce on the cumulative column at L+1 and L+4, report impact** | one item | ⚠ horizon-narrowing — the exact move D-9 (b) was refused for, one day earlier |
| ~~**d. Open the paper and find what the band is actually a band on**~~ | ~~a sourcing item~~ | ✅ **DONE, above. It is the reason (b) and (c) now read the way they do.** |

**Recommendation: (b), with (a) in force until Elias rules.** Basis: (d) has removed the ambiguity the
other options were hedging against — there is now exactly one column comparable to the quoted band, and
the model is outside it at one horizon of three. ⚠ **The honest consequence of a sourcing correction is
sometimes a red suite**, and this project has taken that consequence twice already (the 100+ band at
stage 2, the ratchet at stage 3). (c) is D-9's refused move wearing a new denominator. (a) is defensible
only as a temporary state, and it is the state in force precisely so that nothing is enforced on a number
Elias has not ruled on.

⚠ **No enforcement option is taken.** This asks whether Elias's pre-committed rule is enforced on a
different quantity than he pre-committed it on — the same class as D-9, and the same reason the run does
not self-rule it. **What WAS taken, as a strikeable R-N1 call: the sourcing, and printing all three
columns.** Neither moves a constant, neither changes an exit code, and both are reversible in one edit.

> ~~**To rule it, write:** `D-13 RULED: (b)` *(or a / c)*~~

### ✅ **D-13 RULED (b) — Elias, 2026-09-01. Enforcement moves to the CUMULATIVE column.**

> *"That is the quantity Ramey defines the band over; enforcing on a basis no published family recognises
> is a bar that cannot be checked against anything. Keep all three columns in the harness forever — the
> divergence between them is itself information — and record that the model reads below the band at impact
> and inside it from L+1 on the comparable basis, stated plainly rather than smoothed."*

**Executed the same day.** `ResponsivenessAuditHarness` now asserts on the cumulative column and on
nothing else: L+1 and L+4 must sit inside 0.6–1.0 (they do, 0.607 and 0.702), and **impact is carried as a
RATCHET at its measured 0.507** — the shape `PartyMarkCoverageCheck` and P-I2 stage 3 already use for a
real finding with no fix in hand. ⚠ **It is a FLOOR, not a ceiling**: the run fails if impact slips
further below the band, and the ratchet is *retired* if it ever reaches 0.6, never moved down.

⚠ **Proven in both directions before it was trusted.** With the band ceiling temporarily at 0.65 and the
ratchet at 0.600, the harness exits **1** and names six breaches — three `L+4 = 0.702` and three
`IMPACT = 0.507 … it got WORSE`. Restored, it exits 0 with `3 of 3` dials checked and `0 of 3` impact
horizons inside the band, which is the finding printed rather than hidden.

---

### D-2 (c) · REOPENED by D-13, re-tested, and NOT LANDED — for a defect the first rejection hid

**Elias, 2026-09-01:** *"It was rejected for moving a number off a band that turns out not to be the band.
Re-test it against the cumulative column: if it holds there, it lands; if it does not, it stays reverted
for a reason that survives inspection."*

#### ✅ On the stated test it HOLDS — and it holds by being exactly neutral

The per-country base table was rebuilt from this register's own recorded provenance (OECD Revenue
Statistics `DSD_REV_COMP_OECD`, general government, % of GDP, 2022; Poland from Eurostat `gov_10a_taxag`)
and wired through every call site, then measured:

| | balance impulse | QUASI L / L+1 / L+4 | **CUMULATIVE L / L+1 / L+4** |
|---|---|---|---|
| before | 2.267 | 0.507 / 0.715 / 0.807 | **0.507 / 0.607 / 0.702** |
| with D-2 (c) | **2.303** | 0.507 / 0.715 / 0.807 | **0.507 / 0.607 / 0.702** |

⚠ **Identical to the digit on both of Ramey's quantities.** Only the balance-basis denominator moved —
2.267 → 2.303, reproducing the original rejection's 2.27 → 2.30 exactly. **The constraint that rejected
D-2 (c) never had anything to say about it.** It was rejected by a measurement artifact, which is what
D-13 predicted and this run measured rather than argued.

#### ⚠ And the thing it was FOR works: the identical-across-six finding is gone

`TaxTransmissionDiagnostic`, a +10-point income-tax rise, dC as % of that country's own GDP:

| | USA | Sweden | Germany | France | Italy | Poland |
|---|---|---|---|---|---|---|
| before | −2.68 % | −2.68 % | −2.68 % | −2.68 % | −2.68 % | −2.68 % |
| with D-2 (c) | **−2.06 %** | **−1.34 %** | **−1.55 %** | **−1.44 %** | **−1.67 %** | **−0.94 %** |

Six economies answer a tax change with six numbers. That is the whole point of the item, delivered.

#### ⚠ IT STILL DOES NOT LAND, and the reason is one nobody had reached

`FiscalRecalDiagnostic`, with the table in: **every country's revenue-to-GDP falls off its calibrated
target** — USA 18.0 → **15.12**, Sweden 42.2 → **32.98**, Germany 40.9 → **29.77**, France 45.3 →
**28.66**, Italy 42.5 → **32.38**, Poland 37.6 → **25.54**. The recalibration's ANCHORED quantity is the
primary balance, and this moves it in all six by 3–17 points of GDP.

**Why, and it is structural rather than arithmetic.** `CollectionEfficiency` is *solved* as
`Target / Implied`, where `Implied = Σ(rate × base)`. The sourced base is `(realised revenue % of GDP) /
(seeded rate %)` — ⚠ **it already contains the collection loss.** So the model would mark realised revenue
down a second time. Re-solving CE to compensate needs **CE > 1 for five of six countries** (Sweden 1.006,
Germany 1.151, France 1.182, Italy 1.237, Poland 1.311) — and `Country.CollectionEfficiency`'s own doc says
*"how much of the theoretical tax base is actually collected (0.0-1.0), reflecting enforcement quality, the
size of the informal economy, and evasion"*. A value above 1 makes that sentence false.

⚠ **A second, independent defect in the same table: the USA row is on the wrong fiscal perimeter.** The
sourced bases are **general government** for all six, while `WorldFactory`'s stated organizing principle —
the perimeter rule — puts the USA's whole calibration on the **federal** perimeter, because the state and
local layer is not modelled. The USA's 0.3077 income base is therefore not the base of the thing the model
taxes.

**So it stays reverted, for a reason that survives inspection and is not the one it was rejected for.**
The measurement is kept; the code is not. ⚠ **Route (a) remains refused independently** on Elias's ruling:
a revenue-derived table applied everywhere except revenue is incoherent whatever the multiplier does — and
this run confirms it from the other side, since the incoherence is exactly what the CE double-count is.

---

### D-14 · How a sourced tax base is reconciled with `CollectionEfficiency` and the perimeter rule ✅ **RULED (a) BY ELIAS 2026-09-01 — the revert stands; F-A and F-B split out as their own measured items**

**The question.** The sourced per-country bases already contain the collection loss that
`CollectionEfficiency` exists to apply, and the USA's row is on a different fiscal perimeter from the
USA's calibration — so how does a sourced base land without breaking the primary-balance anchor?

| option | cost | what it forecloses |
|---|---|---|
| **a. Re-solve `CollectionEfficiency` per country, permit values above 1, and RE-DOCUMENT the constant** as the coverage bridge between four modelled instruments and a whole tax system | one item; the anchor is preserved exactly, so only ONE family moves — the policy response | ⚠ a constant named "efficiency" stops meaning efficiency, and it is read by a Cabinet decision channel and the preview clone |
| **b. Re-derive the bases at the THEORETICAL (pre-loss) level** so CE keeps its meaning | ⚠ unknown, and probably unavailable — OECD publishes realised revenue, not theoretical bases | nothing, but it may not exist |
| **c. Land it with CE re-solved AND re-anchor the revenue targets to the four modelled instruments' own share** | ⚠ two BASELINE families landing together — the exact thing D-9 (d) was refused for | nothing permanently; it is the largest of the three |
| **d. Leave D-2 (c) reverted; the identical-across-six finding stands with its cause now fully named** | nothing — today's state | ⚠ the model keeps answering a tax change with one number for six economies, and now we know precisely why the fix is not free |

**Recommendation: (a), and the USA row re-derived on the federal perimeter before anything lands.** Basis:
(a) is the only option that keeps the anchored quantity fixed, so exactly one family moves and can be
explained per country — the project's own rule. (c) violates that rule by construction. (b) is a sourcing
job that may have no source. (d) is honest but leaves a known-wrong term standing when its fix is one
re-documentation away.

⚠ **NOT self-taken, and the reason is a rule rather than a stall.** (a) changes what a documented,
serialized, cross-system constant *means* in all six countries — and the USA half needs a re-derivation on
a perimeter no figure on disk covers. Taking it inside a run whose job was to re-test D-2 (c) would land a
second BASELINE family beside the first, which is the one thing D-9's own option table refuses.
**Logged strikeable: if Elias says nothing, the next fiscal item takes (a).**

> ~~**To rule it, write:** `D-14 RULED: (a)` *(or b / c / d)*~~

### ✅ **D-14 RULED (a) — Elias, 2026-09-01. The revert stands; the findings underneath become their own items.**

> *"No second BASELINE family beside the first — that is the move D-9 (d) was refused for and it does not
> become acceptable because a different item wants it."*

**D-2 (c) stays reverted.** The two defects it surfaced are split out, unattached to the reverted change,
as **F-A** and **F-B** below. Both are measured by `CollectionEfficiencyBasisDiagnostic` — ⚠ **one tool for
two findings on purpose**: a second file would have reprinted the first's arithmetic to add one ratio, and
a tool that mostly restates another tool is a second thing to keep true.

---

### F-A · `CollectionEfficiency` double-counts the collection loss — MEASURED, nothing applied

`CollectionEfficiency` is solved as `Target / Implied`, `Implied = Σ(rate × base)`. Under the **uniform**
stand-ins that figure is deliberately larger than reality and CE marks it down — which is what the word
*efficiency* means, and why every CE today is below 1. ⚠ **The sourced base is `(realised revenue % of
GDP) / (seeded rate %)`, so `rate × base` IS the realised revenue.** Marking it down again applies one
correction twice.

| country | implied (UNIFORM) | implied (SOURCED) | target | CE today | CE needed (SOURCED) |
|---|---|---|---|---|---|
| USA | 29.37 | 24.70 | 18.0 | 0.6119 | 0.7287 |
| Sweden | 53.45 | 41.93 | 42.2 | 0.7865 | **1.0065** |
| Germany | 48.73 | 35.54 | 40.9 | 0.8375 | **1.1508** |
| France | 60.45 | 38.32 | 45.3 | 0.7480 | **1.1822** |
| Italy | 45.10 | 34.37 | 42.5 | 0.9422 | **1.2366** |
| Poland | 42.10 | 28.67 | 37.6 | 0.8910 | **1.3117** |

⚠ **Five of six would need CE above 1** against a field whose own doc says *"how much of the theoretical
tax base is actually collected (0.0-1.0)"*. Where it exceeds 1 the four modelled instruments **under-cover**
that country's tax system, and the constant would be measuring **coverage**, not efficiency.

⚠ **AND THE ONE COUNTRY THAT LOOKS FINE IS FINE FOR THE WRONG REASON.** The USA needs only 0.7287 —
because its target is FEDERAL while its sourced base is GENERAL GOVERNMENT. **Its apparent fit is F-B.**

**Proposed, not applied.** A sourced base and a solved efficiency cannot both be right about the same
quantity. Three exits, and only the first keeps the anchored primary balance fixed: **(1)** re-solve CE and
re-document it as the coverage bridge it would then be; **(2)** re-derive the bases at the *theoretical*
level so CE keeps its meaning — ⚠ OECD publishes realised revenue, not theoretical bases, so this may have
no source; **(3)** leave the uniform bases and keep the identical-across-six response with its cause named.
**Today D-14 (a) holds and nothing is applied.**

---

### F-B · The USA's perimeter mismatch — MEASURED, and it is not a scaling error

| | |
|---|---|
| USA implied revenue on the SOURCED (general-government) bases | **24.70 % of GDP** |
| USA calibration target, FEDERAL receipts (CBO FY2025, on disk) | **18.00 % of GDP** |
| ⚠ **the mismatch** | **×1.372** |

⚠ **Both figures are sourced and neither is ours** — the first is OECD general-government revenue divided
by the game's own seeded rates, the second CBO federal receipts. They are about **different governments**.
`WorldFactory`'s stated organizing principle is the perimeter rule, and the USA sits on the federal side of
it because the state and local layer is not modelled. **So the USA's row of the D-2 (c) table is not the
base of the thing this model taxes, and no amount of re-solving CE fixes that.**

**The bill, precise:** FEDERAL-ONLY revenue by tax type as a share of GDP — individual income, corporate
income, payroll — for one stated year. OECD Revenue Statistics publishes a sub-sector split; ⚠ **two API
shapes were tried from here and returned 422 and 404, so the series is NAMED rather than quoted.** Until it
is on disk the USA has **no** sourced base and must keep the uniform stand-in whatever the other five do —

⚠ **THE COST, MEASURED 2026-09-01 from the repo's own figures rather than from the missing series**
(`COMPLETED.md` §159). `FiscalRecalDiagnostic`: the USA's **spending** seed is federal too — `spendRate 17.0 %`,
discretionary 6.04 %, mandatory 13.83 %, real federal dollars. **The perimeter is consistent today, on both
sides**, so a sourced US tax base breaks it one of exactly two ways:

| | what it costs |
|---|---|
| **A — keep the USA federal** | the sourced table lands for **five countries and not the sixth**; C-N4's *"identical across six"* becomes *"identical across one"*, and the USA's tax response stays uncalibrated while five neighbours are not |
| **B — move the USA to general government** | its **spending** seed must be re-based to the state-and-local layer the model does not have. ⚠ **B breaks the perimeter on the side the model has more of** — 20 % of GDP in spending lines against one tax table. **A seed programme, not a calibration** |

⚠ **A third fetch attempt (CBO's budget data page) returned 403.** Three attempts, three failures; per **R-T2**
nothing about the series is inferred from a probe, and it stays NAMED rather than quoted.
which is itself an argument against landing the table for five countries and not the sixth.

---

### D-10 · C-N1, the media system's route to the ballot ⚠ DECIDED AND TAKEN (R-N1), strikeable · ✅ **HOLDS** (re-read 2026-09-01)

**The question.** C-N1 asked whether perception-only media is **the design** or **an omission**. Measured
2026-09-01, the answer is **neither of its two exits**, and the third one is better than both.

**What the measurement found.** The persuasion chain is coherent and deliberate:

- **Campaign ACTIONS persuade.** `pressure.Add` → `ToCompatibilityBonus()` → compatibility →
  `PreferenceModel.Preference` → `truePreference` → the ballot. Media actions are in that set.
- **Coverage and momentum do NOT persuade**, exactly as `MomentumTracker`'s own doc says: they shift
  *where a race appears to be*.

⚠ **That split only closes if perceived viability can reach the ballot — and the mechanism that would
carry it is BUILT, PROVEN AND UNWIRED.** `TacticalVoting.Apply` and `ApplyToRegions` take a preference
vector and **polled shares** and return a tactically-adjusted vector. They appear in exactly one file
outside their own: `TacticalVotingHarness`. **`ElectionDay` never mentions a poll.**

So the design is right and incomplete in one specific place: **momentum → poll → *nothing*.**

| option | cost | what it forecloses |
|---|---|---|
| **a. Wire `TacticalVoting` into the vote model** | one BASELINE item on the election path | nothing — it is the bridge the design already assumes, already written and already proven |
| b. Declare perception-only media intended and close | nothing | ⚠ it would record as INTENDED a chain that terminates one step short of the mechanism built to receive it |
| c. Give coverage its own persuasion term | a §42 chain change with a new explained family | ⚠ invents a second persuasion route while the first one's bridge sits unused |
| d. Delete `TacticalVoting` as dead | small | ⚠ deletes a proven, sourced-behaviour model to make a gap tidy |

**Recommendation, TAKEN as an R-N1 decide-and-log: (a), sized as its own item and NOT built inside this
one.** Basis: (c) is the expensive answer to a question (a) answers with code that already exists and has
a harness; (b) would write down as deliberate something that is one wire short of deliberate. ⚠ **The
wiring changes election results, so it is a baseline item with its own before/after per country** — and
C-A1's recorded FdI figures are among the numbers it would move, which is precisely why it does not ride
this item.

> **To strike it, write:** `D-10 STRUCK: (b)` *(or c / d)*

---

### D-11 · C-N2, what the door-to-door action is FOR ⚠ DECIDED AND TAKEN (R-N1), strikeable · ✅ **HOLDS, strengthened by §134** (re-read 2026-09-01)

**The question.** C-A2 measured that optimising personalities knock **zero** doors and hold **zero**
rallies, while the grassroots profile carries the roster's strongest pro-local thumb. The defect is
mechanical, not a weighting: the three local actions hold the largest hour costs (4/3/5 h) against the
smallest reaches (0.06/0.02/0.01) while §33 scores **per hour**. ⚠ **And door-to-door is largely
redundant with a mechanism that already runs for free** — W-B4's offices knock doors through their daily
operation, outside the eight actions and outside the AI's choice.

| option | cost | what it forecloses |
|---|---|---|
| **a. Retire the door-to-door ACTION; the office operation is the ground game** | one item; §12's verb set shrinks by one | ⚠ a player loses an explicit verb — but it is a verb whose effect they already get by opening an office |
| b. Re-price the local actions until §33 chooses them | small | ⚠ tuning a magnitude to force a choice, against the standing rule; and it leaves the redundancy |
| c. Make door-to-door do something the office cannot (target a region's swing voters) | one design item plus a build | nothing; it is the answer that keeps the verb by giving it a job |
| d. Leave it; record that local action is a bad bet by construction | nothing | ⚠ §34's "no single dominant approach" bar stays unmet, on the record |

**Recommendation, TAKEN: (c) — give the verb a job, and do NOT re-price.** Basis: (b) is tuning to force
an outcome, which this project forbids by rule; (a) is defensible and cheaper but throws away a verb that
real campaigns spend most of their volunteer hours on; (d) leaves a measured bar unmet. **(c) is the only
option that answers the question C-N2 actually asked — *what is the action FOR* — rather than adjusting
what it costs.** ⚠ Sized, not built: it is a §12 verb-set change and needs its own item.

> **To strike it, write:** `D-11 STRUCK: (a)` *(or b / d)*

---

### D-12 · P-I2 stage 3's time base and the two inseparable rates ⚠ DECIDED AND TAKEN (R-N1), strikeable · ⚠ **RECONSIDER: both halves govern reverted code** (re-read 2026-09-01)

**Two questions the retirement cannot avoid**, both measured before deciding.

**(1) When does the cohort step run?** The substrate ages by a YEAR; the simulation advances by a DAY,
and `AggregationEquivalenceCheck` requires one turn to equal 365 days.

⚠ **A fractional daily step cannot be made exact.** Survival composes exactly under a power
(`s^(1/365)`), and so does a retained fraction — but the band transition is a *matrix*, and its 365th
root does not decompose into per-band constants. Any daily form would agree with the turn form only on
tolerance, and that is a decision about the model's TIME BASE rather than about demography.

| option | cost | what it forecloses |
|---|---|---|
| **a. Step once per turn, on the boundary day** | none — the spec-let already says *"at the YEAR boundary, not the day tick"* | ⚠ `Population` and `DependencyRatio` become annual step functions instead of moving daily |
| b. Fractional daily step accepted on tolerance | one item | ⚠ turn/daily equivalence stops being exact BY CONSTRUCTION and becomes a tolerance the suite must carry forever |
| c. Keep the scalars daily, run cohorts in parallel | none | ⚠ **two populations** — the exact thing the spec-let's §5 exists to forbid |

**Recommendation, TAKEN: (a).** It is the spec-let's own instruction — *"a day-tick aging step would need
fractional cohort movement, which is arithmetic nobody can check on screen"* — and it makes turn/daily
equivalence **exact by construction** rather than by tolerance, which is the property this project has
protected through every continuous-time phase. The cost is real and visible: **a population that steps
once a year instead of creeping daily**, which is also the more honest picture of an annual stock.

**(2) `DeathRate` and `NetMigrationRate` cannot both be derived.** D-6 made the survival ratio deaths and
net migration *together*, so the step measures their SUM and nothing in it separates them.

| option | cost | what it forecloses |
|---|---|---|
| **a. Both become DERIVED REPORTS of the step, split by each country's seeded ratio** | none | ⚠ the split is authored; only the total is measured, and the doc must say exactly that |
| b. Keep both stepped by their own rules | none | ⚠ they would drift independently of the population they no longer drive — **two populations by the back door** |
| c. Retire `NetMigrationRate` and report only a combined rate | one item | ⚠ deletes a stat the UI shows and the immigration lever names |

**Recommendation, TAKEN: (a).** ⚠ **The total is measured and the split is authored, and that sentence
goes at the declaration.** (b) is the failure the collision map calls double-stepping wearing a different
hat; (c) throws away a stat to avoid writing one honest sentence.

> **To strike either half, write:** `D-12 STRUCK: (1b)` or `D-12 STRUCK: (2c)` *(etc.)*

---


---

---

### D-17 · A closed row must cite the commit that closed it — and that hash cannot exist inside the commit it names ⚠ DECIDED AND TAKEN (R-N1), strikeable

`InstructionResidueCheck` requires every row under the CLOSED heading to produce a commit hash, so that
section cannot become the place a row goes to stop being counted. ⚠ **But a citation is a claim about a
commit, and a commit cannot contain its own hash** — so an item's work and its closure record cannot be
the same commit without the citation being false or absent.

| option | cost | what it forecloses |
|---|---|---|
| **a. The closure record is its own commit, citing the work commit** | two commits per item instead of one | nothing; the residue lags the work by one commit, which is visible and true |
| b. Cite the *previous* commit, or a branch name | none | ⚠ the citation stops pointing at the work, which is the only thing it is for |
| c. Amend the work commit to add the row after the fact | none | ⚠ the amended commit's hash changes, so the citation is stale the moment it is written — the defect in its purest form |
| d. Drop the citation requirement | none | ⚠ refused: it is the clause that makes CLOSED mean something, proved both directions this session |

**Recommendation, TAKEN as an R-N1 decide-and-log: (a).** Basis: it is the only shape in which the
citation is *true*. ⚠ **The "one commit per item" rule is about not batching items together, not about
forbidding a record** — and the record commit is cheap, small, and independently reviewable. The visible
consequence is stated rather than hidden: **between an item's two commits the residue is one higher than
the work warrants**, which is the honest direction for it to be wrong in.

> **To strike it, write:** `D-17 STRUCK: (b)` *(or c / d)* — the mechanism is three lines of the check.

### D-16 · How F-B's five-country landing survives F-A's double count ✅ **RULED (a) BY ELIAS 2026-09-01** — execution still STOPPED for budget, order written

> ⚠ **SEQUENCING FOR THE NEXT BLOCK (Elias, 2026-09-01).** D-16 and D-15 stage 3 both need a session with
> **the trajectory suite in budget**. **Take them together only if BOTH explanations fit; otherwise D-16
> first, because its order is fully written.** ⚠ Neither may land half-explained — that rule has now bound
> three times this week against work somebody wanted to land, which is what makes it a rule rather than a
> preference: *anchors do not yield to tables.*
>
> **Carry into the prompt for stage 3**: the Eurostat `age` dimension holds **110 categories and mixes
> single years with aggregates**, so a band build must **filter and never sum** — summed, Sweden reads
> **44.8 million** instead of 12.1.

**F-B RULED by Elias, 2026-09-01: keep the federal perimeter.** *"Consistency inside a country outranks
uniformity across the set, and breaking the perimeter on the side the model has more of is the worse
trade."* **Option B is not to be built.** The sourced tax-base table lands for **five** countries; the USA
keeps the uniform stand-in with its reason stated. The CBO series stays named-not-quoted with its 403.

⚠ **But F-A is unresolved, and landing the table for five re-introduces exactly what §147 measured**:
those five need `CollectionEfficiency` **above 1** to hit their targets (SE 1.0065, DE 1.1508, FR 1.1822,
IT 1.2366, PL 1.3117), because the sourced base already contains the collection loss CE exists to apply.
**Landing F-B on its own would knock the primary-balance anchor off in five countries** — the regression
D-14 (a) refused to absorb. So F-B's ruling needs one more decision, and this is it.

| option | cost | what it forecloses |
|---|---|---|
| **a. Re-solve CE for the five and RE-DOCUMENT it** as the coverage bridge between four modelled instruments and a whole tax system | one item; ⚠ the anchored quantity is preserved **exactly**, so only the RESPONSE family moves and the level family does not | a constant named "efficiency" stops meaning efficiency; it is read by a Cabinet decision channel and the preview clone |
| b. Land the table and accept the revenue drop | none up front | ⚠ the primary balance moves 3–17 points of GDP in five countries — the thing the recalibration anchored |
| c. Land it in the transmission term only | one item | ⚠ **refused independently by Elias** at D-9 route (a): a revenue-derived table applied everywhere except revenue is incoherent |
| d. Do not land until F-A is ruled | none | ⚠ F-B is ruled and would sit unexecuted behind a second ruling nobody has been asked for |

**Recommendation, TAKEN as an R-N1 decide-and-log: (a).** Basis: it is the only option that honours F-B's
ruling **and** keeps the anchored quantity fixed, so exactly **one** family moves and can be explained per
country — the project's own rule. ⚠ **And the explanation is cheap in this shape**: with CE re-solved, the
no-policy path is algebraically unchanged, so the expected diff is **float-path divergence only**, which
C-C6 already established as an explainable class. (b) breaks the anchor, (c) is refused, (d) stalls a ruling.

#### ⚠ THE EXECUTION IS LOGGED **STOPPED**, WITH ITS REASON AND ITS ORDER

The landing is a **BASELINE change** and this project's rule is that the new family lands **with** its
per-country explanation or it does not land. The explanation needs the **trajectory suite** — a six-country
dump that has historically run over an hour — and the budget that remains cannot carry both it and the
records of the work already done. **Nothing is landed. The order, for the item that takes it:**

1. per-country base table for **the five only**, USA excluded with its reason at the call site;
2. `CollectionEfficiency` **re-solved** for the five from `FiscalRecalDiagnostic`'s own implied figures,
   the constant re-documented, and ⚠ **the >1 values named as coverage rather than efficiency**;
3. `FiscalRecalDiagnostic` re-run — **each of the five must land back on its recalibrated target**, which
   is the proof that the level family did not move;
4. `TaxTransmissionDiagnostic` — the per-country response must differ, which is the point of the item;
5. the trajectory suite, with the diff **explained as float-path divergence per country** or not landed.

> **To strike it, write:** `D-16 STRUCK: (b)` *(or c / d)* — nothing is built on it, so a strike costs one
> decision and no code.
### D-15 · P-I2 stage 3's anchor: which shape converges toward the sourced projection ⚠ DECIDED AND TAKEN (R-N1), strikeable

> ⚠ **STEP 1 ADVANCED 2026-09-01, and then the probe was DELETED rather than left in the tree.** Two things
> the next session does not have to rediscover:
>
> - **The Eurostat query shape that works.** The statistics API refuses the TSV form (400) and answers the
>   JSON one: `.../statistics/1.0/data/proj_23np?format=JSON&geo=SE&projection=BSL&sex=T&time=2050`.
>   **Sweden 2050 reads 12 130 240 — exactly the figure §142 recorded**, so the query returns the intended
>   series and not a neighbouring one. Downloadable straight to disk with `Invoke-WebRequest -OutFile`.
> - ⚠ **A trap that would have produced a wrong catalog silently.** The `age` dimension holds **110**
>   categories and MIXES single years with aggregates — `TOTAL` and the broad bands sit beside `Y1`, `Y2`,
>   and so on. A naive sum over the dimension gives **44.8 million** for a country of 12.1 million. **A band
>   build must FILTER to the single-year categories, never sum the dimension.**
>
> Nothing was seeded and nothing kept: a dataset confirmed reachable is not a dataset used, and an
> unconsumed download in the tree is the thing §158 refused.

**The question.** §141 reverted stage 3 because the cohort step applies one observed year's rates forever
and has no reversion of any kind — Germany and the USA reach `MaxPopulation`, Italy, Poland and Sweden
reach `MinPopulation`. §142 then confirmed a **sourced target trajectory for all six** (Eurostat
`proj_23np` baseline; US Census `np2023_d1_mid.csv` by single year of age). **What converges toward it?**

| option | cost | what it forecloses |
|---|---|---|
| **a. Converge the SURVIVAL array** toward the projection's implied survivorship | one item; the step keeps its shape and only its rates move | ⚠ survival is deaths **and net migration together** (D-6 (a)), so this converges a composite and cannot be read as mortality improving |
| **b. Converge the FERTILITY rate** toward the projection's implied births | one item | ⚠ fertility alone cannot fix a pyramid that is wrong at the top; Germany's overshoot is not a birth problem |
| **c. Scale the WHOLE PYRAMID toward the projected one**, band by band, at a rate the projection itself implies | one item, and the arithmetic is a per-band ratio rather than a new model | ⚠ the step stops being purely generative — the pyramid is pulled toward a published shape, and that must be said at the call site |
| **d. Leave stage 3 reverted; keep the ratchet at two** | nothing — today's state | ⚠ the substrate stays a table rather than a model, and F-7 stays blocked behind it indefinitely |

**Recommendation, TAKEN as an R-N1 decide-and-log: (c).** Basis, and it is the same argument §141's revert
made: **the defect is that nothing anchors the LEVEL**, and (a) and (b) both anchor a rate while leaving
the level free — they would slow the divergence without stopping it, which is the failure that is hardest
to see because it looks like an improvement. ⚠ **(c) is also the only option whose honesty is checkable**:
a per-band ratio toward a published pyramid can be hindcast against the publisher's own intermediate
years, the way stage 2's step was, and that assertion is what caught stage 2's 50 % double count.

⚠ **Its cost is real and goes at the call site**: the population is no longer purely generated — it is
generated and then **pulled toward a published projection**, which is a different claim about what the
model knows. **That sentence is the price, and it must be written where the code is, not here.**

> **To strike it, write:** `D-15 STRUCK: (a)` *(or b / c / d)* — nothing is built on it yet, so a strike
> costs one decision and no code.

## 1. The clearance pass — live work (owner CODE unless stated)

Execution order: Phase 0 → A → B → C → D → R → E → F → G, then the Track N fix rows.

⚠ **ONE DEPENDENCY CHAIN GOVERNS THE ELECTIONS SIDE AND IS STATED HERE ONCE, so the FdI ceiling reads as
a plan rather than a surprise** (forced by C-D1, 2026-08-31):

> **C-C13's ruling → P-I2 (the cohort substrate) → C-D1 (per-valkrets voter groups) → C-A1's per-group
> loyalty → the Italy FdI surge.**

Every link is a real blocker, not a preference. Voter groups must be a **view** over the cohort substrate
with computed shares (`POLISIM_COHORT_SPECLET.md` §5) — sourcing per-valkrets marginals onto a separate
group layer would build the second population that spec-let exists to forbid. So **C-A1's named ceiling is
four items away and the first of them is Elias's to rule**, not a fetch anyone can do tonight.

### Phase 0 — the reconciliation

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-0.1 | This file: the single ordered register | every open item appears exactly once; every closed row cites its commit; a grep proves no open item sits in a source document without a row here | CODE | SAFE | — |
| C-0.2 | The post-wiring re-derivation — correct every document that still asserts a pre-wiring premise | no live document asserts a pre-wiring premise (grep: `PartyArchetype`, `TotalSeats = 200`, "not wired", "unreachable from any gameplay path", "VERIFIED NOTHING", "no party seeds exist on main", "UNINSPECTED") | CODE | SAFE | C-0.1 |
| C-0.3 | ✅ **CLOSED 2026-09-01 (`COMPLETED.md` §169) — and ⚠ "four unsuperseded pieces" was never derived.** The number appeared only in this row and the master-list row that copied it. Measured: **thirteen of fifteen artifacts are superseded**, each shown against what replaced it — and in three cases by something stronger (sourced thresholds, statutory citations, 6 584 captures at the resolution a Python port existed to reach). ⚠ **TWO are unsuperseded and neither is code**: `Chamber.ChamberRenewal` (chamber staggering — `StaggeredThirds`, `FollowsAnotherBody` appear NOWHERE on main) and `ElectorateCohort`'s per-election-type turnout (`TurnoutModel` has no election-type term). Both recorded as **preserved ideas, not work** — features to plan, not defects to fix. The branch stays a recorded ref, which is its citation | CODE | `COMPLETED.md` §169 |
| C-0.4 | ✅ **CLOSED at `9489d97`** — the check suite's batchmode entry (`RunAllBatch`) exists and every bar run since has used it; the per-check invocations still work unchanged. ⚠ **The register read OPEN until 2026-09-01, when the master-list sweep put it against the code and the code won** — a row can be done for weeks and the document not know it, which is why the sweep is by repo and never by list | closed; cited above | CODE | SAFE | — |

### Track A — the verifications whose blockers have landed

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-A1 | The Italy FdI standing test re-run (4.35 → 29.27 %) | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §87): UNREACHABLE.** Blended 10.31 % (dev −18.96 pp), spatial alone 17.82 % (dev −11.44 pp), gate MAD 7.14 pp REGRESSED at 53 % coverage. Momentum and media cannot move a vote at all (two call sites, both polls); the persuaded share required is 58.58 % against 17.82 % produced, ×3.29. **Ceiling: per-group loyalty → C-D1.** Original done-when: the measurement is on record either way, **the path it ran on is named**, and the standing test is closed. No tuning; the loyalty constant is not re-fitted | CODE | SAFE | — |
| C-A2 | The local-campaigning question (the worklist's standing design question, until now unowned) | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §88): the MECHANISM, not the weighting.** Rally 0 for all eight parties; door 0 for seven of eight. The grassroots personality carries door affinity 2.2 and enthusiasm value 1.6 — the strongest pro-local thumb in the roster — and still knocks zero doors, so §33 is not blind to local value. The three local actions hold the largest hour costs (4/3/5 h) and smallest reaches (0.06/0.02/0.01) while §33 scores per hour. Original done-when: the measurement names the cause — the model underpowers local action, or §33's EV function undervalues local reach. **No adjustment in this item** | CODE | SAFE | — |
| C-A3 | 2a-iv re-measured after W-B12 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §89): stays PEND at est/grass 0.269** (prof/grass 0.573), 0.031 below the line — **not 0.291**, which has been superseded since W-F1. W-B12 moved it by nothing, so the convergence is in what the two parties CHOOSE, not what they can afford. The dependency is re-pointed from the stopped W-F5 to the pool question, C-D2. Original done-when: the line carries a current number. The 0.30 threshold does not move | CODE | SAFE | — |
| C-A4 | The claim sweep — re-word every claim whose evidence has since been superseded | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §90).** The broadest claim in the repo — "five of six real chambers reproduce EXACTLY" — counted a STAGE as a chamber: it is four chambers (Sweden 2022, Germany 2025, Poland 2023 real-system, USA 2024, each deviation 0/exact) plus **Italy 2022's national proportional stage only, 245 of the Camera's 400**, with France uncovered. Every pre-W-F1 "seat-for-seat" is scoped at its two origin sections. Original done-when: no record overstates its own evidence | CODE | SAFE | C-0.3 |

### Track B — item 10 and the D0 reconciliation

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-B1 | §E2's mark accounting, recorded (the code half shipped at `a289e1e`) | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §91) with no new work, honestly reported**: W-G1 did the code half, C-0.2 re-derived §E2, C-0.4 measured 53/1/52/0, and D8-1 already quoted the check verbatim. R3 DISCHARGED. Original done-when: §E2 states the real accounting; the 52 feed D8-1's count | CODE | SAFE | — |
| C-B2 | The R5 hex exchange — Sweden's eight inks, 45 named uncoloured | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §91).** Sweden's eight seated hexes delivered into D8-2, 45 named uncoloured. ⚠ **Built `PartyInkHarness`, which `PoliSimTheme` already claimed was checking this and did not exist** — and it found the constraint BROKEN: **S and V collapse onto one ink `#753838`** (a 106-seat and a 24-seat party drawn identically), and six of eight sit inside the derived 8.7° floor. Reported as PEND, not fixed; both are D8-2's ruling. Original done-when: the exchange is a line in the Design ask, not a gate in the prerequisites file. Nothing picked by eye | CODE | SAFE | C-B1 |
| C-B3 | The trade axis for the Trade bill's vote, on R-CL2 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §92).** `EuPosition` on the party record (31 sourced values, joined by each party's existing lrecon/galtan pair, AVS and the other unscored units left NaN), `TradeStance` over §29's own `RescaleEu`, a `BillAxis` the vote and both screens share. ⚠ **Poland is the one chamber that changes its mind** (+0.150 → −0.227 on a rise); Sweden and Germany triple in magnitude without flipping; **the USA falls back to fiscal, asserted exactly, because GPS 2019 has no EU item**. Trajectories 6/6 identical — containment only, the per-country table is the evidence. Original done-when: the vote reads a trade position or documents why it cannot, **per country** (the USA has no `eu_position` and keeps the fiscal axis with the reason stated) | CODE | SAFE ⚠ vote-side evidence, not trajectory | R-CL2 |
| C-B4 | Riksbank-B merged into P-D1 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §93): MERGED into C-C7, not inherited** — P-D1 specifies the same subject, and felt verdict 2 ("still not independent") travels with it. Three documents that said it was waiting now say where it went, including the shelf line explaining its absence from the shelf. Original done-when: one item, not two, with felt verdict 2 attached | CODE | SAFE | — |
| C-B5 | Step 6 (story mode) re-gated — scope the gate, not the work | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §93).** Both old gates fired (item 10 shipped; the player-party question ruled R-CL1). ⚠ What remains is **not a ruling but two builds Step 6 does not own** — C-R2 (the party choice persisted) and C-D4 (§38 carry-over); until both land an arc can remember what the government did but not what the party is. The work is deliberately NOT scoped. Original done-when: the entry says whether it opens now or what remains | CODE | SAFE | R-CL1 |

### Track C — the Playtest-1 remainder

| ID | what | finding | class | depends on |
|---|---|---|---|---|
| C-C1 | P-B1 — yearly budget impact on drafts | 3 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §94).** `EstimateBudgetBill` — two clones, one standing and one with the draft applied through the same delegate a passed bill uses, each run through the model own boundary; a full turn IS a year so the figures are annual with no scaling. ⚠ **The honest range is a POINT** (the preview never rolls an event and is deterministic), with the scope stated instead — and the 1280 film caught the first cut printing `FormatMoneyEstimate`s **randomly rolled** ± above a caption saying "no margin". Diagnostic: no clone escape, untouched draft exactly zero, legs reconcile to 0.0000, direction right, 6 of 6. Films 4 widths, guards silent | — |
| C-C2 | P-B2 — the first-year budget window | 4 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §95). SAFE — the identity HOLDS, and was established where the change lives.** ⚠ A trajectory diff could not have shown it: `TryOpenBudgetProcess` is reached only via `AdvanceCountryDayTick`, whose callers are `GameController.Update` and the capture driver, never `AdvanceDay` — so the trajectory harness never calls the changed method and a clean diff would have passed regardless. Asserted instead by two worlds of day ticks, window forced open against never opened, no bill introduced, every `EconomyState` float: IDENTICAL. Trajectories 6/6 identical, reported as containment only. ⚠ **The measurement also found an off-by-one that cost five countries a YEAR** — the epoch is 1 Jan, five countries budget on the calendar year, and the day tick runs after the date has moved to the 2nd, so the window did not open until 2027-01-01. Now 1 tick for all six; filmed open on 31 January | — |
| C-C3 | P-F1 — the Policy Web's focus mode | first sitting's 3 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §96).** Node dimming (dimmed, never removed — the ring's shape is the structure), direction arrowheads, and the restore gesture (second click / empty space). Thickness scaling and DERIVED/DECLARED already existed. R-W2 kept: no invented edge, no new hue (dimming reuses `PoliSimTheme.Tint`), no legend; provenance survives the arrow; the head is sized FROM the line thickness so it cannot contradict the coupling weight. `PolicyWebCensus` now ASSERTS every weight is real and in [0,1]. ⚠ The 1280 film caught the heads being painted over by the nodes they pointed at. Filmed rest/focused/restored at 1280 and 2560; **rest and restored byte-identical** | — |
| C-C4 | P-G4 — enactment markers on the graphs | 9 (cheap half) | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §97).** Ticks at the TOP of every live graph from `Country.Divisions`, passed entries only; the release-tick idiom reused and distinguished by POSITION rather than a new hue. ⚠ The mapping is anchored on the series' own `LastQuarterlyDate`, **not** on `CurrentDate` — which would be right one day in 91 and drift for the other ninety — and an out-of-window enactment is DROPPED, never clamped. `EnactmentMarkerDiagnostic` ALL PASS: 3 of 5 records draw, at exactly 0.0000 / 0.5000 / 1.0000. Filmed at four widths; the films correctly show NO ticks, a new game having enacted nothing | — |
| C-C5 | P-C1 — national currency display | 5 | ⏸ **STOPPED AND BILLED 2026-08-31 (`COMPLETED.md` §99), not abandoned.** ⚠ Re-ordered after C-C6 (R-N1, §98), and C-C6 then established that the blocker is **data, not sequencing**: every seed is USD, so the ONLY two ways to show kr — re-base the seeds, or convert at display time — **both need a sourced vintage-dated FX rate, and none is on disk.** Printing kr on a dollar figure would be false by ~10.5×. **THE BILL:** USD/SEK, USD/PLN and USD/EUR at one stated vintage, from a citable authority (ECB euro reference rates publish SEK, PLN and USD daily, so the three cross-rates are one fetch and one stated derivation). **THE BAR, once sourced:** re-basing is a seed change under the full sim-math bar with per-country diffs explained — and §98 already establishes those diffs are **float-path divergence, not a modelling change**. The display layer itself (per-country symbol, placement, format; `MoneyUnit` extended not replaced; a unit test per country) is a session's work **once the number means what the symbol says** | ELIAS / CODE — the FX bill first | ⚠ **PROBED 2026-09-01 (D-block item 6): the rates ARE reachable — ECB `eurofxref-daily.xml`, HTTP 200, vintage 2026-08-31, EUR/USD 1.1596 · EUR/SEK 11.1100 · EUR/PLN 4.3280, and the three pairs cross through the euro in one stated line. ⚠ NOT TAKEN, and the reason is the gate itself: a 2026-08-31 rate against 2024/2025 seed vintages is the BASIS-MIXING the cross-check gate forbids. The bill is not discharged, it is SHARPENED — it needs the rate at each seed's own vintage, which the ECB also publishes (`eurofxref-hist.xml`). No rate authored, none seeded. See `POLISIM_SEED_DATA_MACRO_OVERHAUL.md`.**
| C-C6 | P-C2 — the seed basis (RULED: national units, cross-country views at a sourced vintage-dated rate) | 6 | ✅ **MEASURED AND DOCUMENTED 2026-08-31 (`COMPLETED.md` §98); the done-when — "the basis is documented in the seed doc, whichever branch ran" — is met.** The seeds are **USD billions for all six countries**. ⚠ **Two findings pointing opposite ways:** at ×2 (exact in binary) every ratio and level is invariant **to 0.000E+000**, so no constant carries an absolute money scale and the unit is a convention — **but** SEK/USD is ~10.5 and at ×10 the float path diverges to order-unity by 12 turns, so a re-based seed set *would* produce different trajectories. **The cheap branch is not available**; re-basing is a seed change under the full sim-math bar whose diffs are explained as float-path divergence. ⚠ **The re-basing itself is BILLED, not done: it needs a sourced vintage-dated FX rate per country and none is on disk.** | ELIAS (the FX bill) / CODE |
| C-C7 | P-D1 — central bank independence **+ Riksbank-B** | 7 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §100). The first BASELINE item, and its family is explained per country.** Four of six already had a rule-driven rate; only Sweden and Poland were player-set. Both get a governor (original fictional people, bias 0), so `CurrentFedChair != null` routes them to the existing chair path — **the rate slider is gone by construction, not by deletion**, and the reaction function is `TaylorRule`, already declared, so **no `[AUTHORED-DRAFT]` value is introduced**. ⚠ **New family `traj_cc7_*`: Germany/France/Italy BYTE-IDENTICAL** (shared euro — `ApplyCurrencyStrength` skips them), **Sweden/Poland changed as intended**, and **the USA changed through a NAMED channel** — its partner-rate average includes Sweden and Poland, so `TradeBalance`/`CurrencyStrength` move at turn 1 and everything else at turn 2. Pressure mechanics recorded as future, not built. ⚠ The film caught four USA-flavoured strings on a Swedish sheet; three fixed, the fourth is S-15 | C-B4 |
| C-C8 | P-E1 — the international browser | 8 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §102).** Browsable pages over the other five, prev/next, every line derived — headline readings side by side, the pair's trade from the map's own links, the tariff each charges through the same `GetTariffRate` the simulation charges, bloc and currency, both compass axes as the compass computes them. ⚠ **The absence block is the page's most important content:** this model holds **no bilateral relations state at all**, so the page says so plainly rather than inventing a warm/cool reading, and a pair with no link reads "not the same as trade of zero". ⚠ The 1280 film caught the first cut leaking a backticked type name onto a player surface — P-A1's own class, which `MetaTextCheck` does not catch (→ S-16) | — |
| C-C9 | P-G1 — the shadow baseline | 9 (deep half) | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §§103–105), in three parts: premise measured, gate passed, then wired.** ⚠ **A shadow turn consumes 41 real draws**, so `ShadowBaseline` wraps every one in a save/swap/restore of the generator state, restoring in a `finally` — and **no shadow computation was written until the gate proved it**. Five assertions: counters unchanged · ⚠ **two real games, one with a shadow beside it and one without, BYTE-IDENTICAL over 8 turns on every `EconomyState` field** (the one that binds — a counter check cannot substitute) · the shadow equals a plain no-policy world · **under a live policy the real game moves and the shadow stays exactly on the baseline** · a shadow turn that THREW left the real state untouched. The six economic graphs now carry a dashed "without your policies" line, drawn on the real series' own scale. Cost shipped as measured (~148 ms the pair); **lazy computation named as the fallback and not built**. ⚠ **Assertion 4 first reported itself UNTESTED rather than passing** — its rate lever is dead because C-C7 gave every country a central-bank head; re-armed on the tax lever. **RIDE-1 discharged here.** Trajectories 6 of 6 byte-identical to `traj_cc7_*` | — |
| C-C10 | P-G2 — the impact ledger | 9 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §106).** Premise measured first: six whole games, leave-one-out per dial. ⚠ **THE DIALS ARE NOT ADDITIVE** — the residual reaches **17.4 % of the divergence on government debt**, 12.6 % on the budget — so the ledger carries the **interaction as its own named line**, on Elias's ruling that an honest residual beats a false identity. Built: `ShadowBaseline` gained a fork-from-a-running-game constructor (a save/load round trip, not a second deep clone); `PolicyImpactLedger` runs one except-world per family the player has touched, forked on FIRST TOUCH and **proven byte-exact** against the same world run from the seed; the Statistics sheet gained the "your policies" block. Gate ALL PASS (partition · byte-identity · the identity, worst break 0 · the exact fork · cost 47 ms real + 205 ms explanation). ⚠ **A film caught a bug no assertion would have**: `Start` binds the ledger before the player picks a country, so the id now travels with the call. New flag `-shotledger` films the populated state the no-policy sweep cannot. Second finding, larger than the item: **tax and welfare move GDP/inflation/unemployment by exactly zero** → S-19, C-C11's headline | C-C9 |
| C-C11 | P-G3 — the responsiveness audit (**propose, apply nothing**) | 9 (honesty half) | ✅ **MEASURED AND PROPOSED 2026-08-31 (`COMPLETED.md` §107). NOTHING APPLIED — the harness has no code path that writes a constant.** Three findings, each against a named source with its vintage: ⚠ **the tax multiplier is EXACTLY 0.000** at three tax types, both directions, three horizons, while producing a real revenue impulse — against Romer & Romer 2010's −2 to −3, a missing transmission mechanism rather than a mis-calibration; ⚠ **the implied Okun coefficient is −0.007** against Ball/Leigh/Loungani 2013's −0.23…−0.54, **33–77× too small**; ✅ **the spending multiplier is RIGHT** — 0.603 / 0.850 / 0.966, inside Ramey 2019's 0.6–1.0 at every horizon, linear and symmetric, **and must not be touched while the other two are fixed.** Five strikeable recommendations R-C11a…e; the tax magnitude is **BILLED** (no Swedish source readable from here) rather than proposed. ⚠ Two experimental errors caught and recorded before publishing: reading at a fixed year 1 measures the model before the lever lands, and holding a percentage spending dial COMPOUNDS (it showed 0.6→1.5→5.3, which was the harness, not the model) | **ELIAS, per line** |
| C-C12 | P-H1 — the tax spec-let (document only) | 10 | ✅ **WRITTEN 2026-08-31 — `POLISIM_TAX_SPECLET.md` at root (`COMPLETED.md` §108). Awaits Elias's ruling, line by line; no tax code until then.** Sweden's real instruments **SOURCED** from Skatteverket *Belopp och procent 2026* and SCB *Kommunalskatterna 2026* (kommunal average 32.38 %, statlig 20 % above a brytpunkt of 660 400 kr, arbetsgivaravgifter 31.42 %, moms 25/12/6); ⚠ **two rows left deliberately unfilled** (bolagsskatt, kapitalskatt) rather than filled from memory. ⚠ **The headline finding: three of the six countries do not fit a bracket table at all** — Germany's tariff is a formula, France has the quotient familial, Italy has three layers — so "brackets as data" would fit three countries and misrepresent three. Seven strikeable design lines; ⚠ **the spec-let is DOWNSTREAM of C-C13** (a bracket schedule over a single average income is arithmetically a flat rate) and downstream of C-C11's R-C11a (detailed instruments with a zero output channel are detail without consequence). Sized ~8 sessions, ~11 if the pluggable-schedule branch is ruled | **ELIAS, per line** |
| C-C13 | P-I1 — the cohort spec-let (document only) | 11 | ✅ **WRITTEN 2026-08-31 — `POLISIM_COHORT_SPECLET.md` at root (`COMPLETED.md` §108). Ruled before built; P-I2 is sized there and not in this pass.** **5-year cohorts recommended**, and the sourcing agrees rather than merely permitting (21 cohorts × 6 countries, and none of the four consumers can tell 42 from 43). ⚠ **The collision map is the document**: eight `EconomyState` scalars become derived, `NaturalBirthRate`/`NaturalNetMigrationRate` are ANCHORS whose loss makes every demographic policy effect compound instead of offset, the player's two demographic levers reach the rates through **two hops** and both must be re-pointed or they join the tax dials and the rate lever as dead levers — **three would be a pattern**. The voter groups become a **VIEW** over the same cohorts with computed shares and an eligible-population cut, asserted the way the approval ledger asserts its identity. ⚠ **One dataset id asserted (`demo_pjan`), six BILLED** — a code recalled rather than checked is an invented figure in a technical costume. Sized ~11–13 sessions | **ELIAS, per line** |
| **C-C14** | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §109).** `MinPreviewMarginPercent`/`MaxPreviewMarginPercent` and **`_previewRandom` itself** are deleted; the scope replaces the margin in three places (the Estimated Effects caption, the Desk's margin line — same slot, same height, so nothing below moves — and the Desk's no-draft caption). ⚠ **Two doc comments in other files named `_previewRandom` and were re-pointed in the same commit** rather than left naming a deleted field (S-11's phantom-guard class, avoided by hand because C-E3's check is not built yet). Films 81/0/0 at 1280 and 2560, trajectories 6 of 6 byte-identical. ~~**Remove the Estimated Effects panel's authored ±5–10 % margin** — **RULED 2026-08-31 (Elias, on S-12): REMOVE it, do not re-roll it and do not stabilise it.** `FormatMoneyEstimate` draws a margin from `_previewRandom` and appends it to the model's own figure, and the panel's caption states the range in prose; `PreviewTurn` is deterministic, so **the honest form is the point with its scope stated**, exactly as C-C1 resolved it on the Budget surface. Four macro rows (GDP growth, unemployment, inflation, approval) plus the money rows the same helper formats. *Done when:* no rolled margin reaches a screen, the scope replaces it in the caption, and the films show the four rows at four widths | S-12 | SAFE | ⚠ **AFTER C-C5 and C-C6** — both touch the same money/display paths, and the ruling is explicit that this is not to be done inside a currency item |

*P-I2 builds only after C-C13 is ruled; it is a DEFERRED row below, not a Track C item.*

### Track D — the elections remainder

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-D1 | W-F4's real path — the voter groups | ✅ **BUILT 2026-09-01 (`COMPLETED.md` §136), on the substrate D-4 unblocked.** Closed as billed 2026-08-31 with the finding that the blocker was the ORDER, not the data; P-I2's substrate landed, so the groups are now a VIEW over it exactly as the spec-let's §5 requires — shares COMPUTED, never seeded, of the **eligible** population rather than the country's. Σ shares = 1.000000000 in all six. ⚠ **Turnout SOURCED for Sweden only** (SCB *Valdeltagandeundersökningen* `ME0105C/ME0105T01`, fetched by POST; **vintage 2014, the end of the series**), and the check FAILS if another country carries a number. **The cross-check: Sweden's 2024 cohort shares weighted by SCB's 2014 band rates give 85.67 % against SCB's separately-published 85.8 %** — two independently sourced things agreeing to 0.13 points across a decade. ⚠ **No per-group LOYALTY** — see the row below | CODE | SAFE | ✅ P-I2 stages 1–2 |
| **C-D1b** | **Per-group loyalty — the FdI ceiling's last link** | ⏹ **MEASURED AND BLOCKED 2026-09-01 (`COMPLETED.md` §137), and blocked by NON-CIRCULARITY rather than by data.** ⚠ **The ceiling is now CONFIRMED, not merely named**: ITANES 2022 (`doi:10.13130/RD_UNIMI/JV77WR`, unrestricted) puts FdI's weighted vote at **18.22 % among 18–24 and 32.04 % among 45–54**, a 13.8-point spread, while PD runs 25.91 % among the over-65s against 14.25 % at 55–64. The extraction validates itself — weighted national shares reproduce the real 2022 Camera result to within a tenth of a point — and the gradient survives unweighted. ⚠ **`LoyaltyModel` holds non-circularity as an invariant: a 2022 backtest needs 2013 and 2018, and only the 2022 wave is openly available.** The one wave available is the one wave forbidden. **The bill is exact: ITANES 2013 and 2018 post-election waves, weighted cross-tabs of vote by the same six age bands** | CODE | SAFE ⚠ blocked on two data waves | C-D1 | ⚠ **THE BILL IS NOW EXACT (2026-09-01, `COMPLETED.md` §149).** Verified on the archive's own API that `dataverse.unimi.it` holds **exactly one** ITANES dataset (2022, `doi:10.13130/RD_UNIMI/JV77WR`) — the negative is measured, not inherited. The **2013 and 2018 waves ARE published**, on `itanes.it`'s Data Portal (fourteen waves, 1968–2022), each on its own page with *Questionario pre-elettorale*, *Questionario post-elettorale* and **Dataset completo** — behind **"Log in / Register to access"**, with no anonymous download link (read at the 2018 page itself). ⚠ **NOT TAKEN: the gate is a REGISTRATION, and creating an account on an external service in Elias's name is an outward-facing act this session does not take** — the E2 convention that keeps sending his. **The errand: register, download both complete datasets, and produce one weighted cross-tab per wave — vote choice by the same six age bands §137 used, with that wave's own weight variable. Nothing else is missing.**
| C-D2 | W-F5's pool question — size a playable pool, propose, apply nothing | ✅ **MEASURED AND PROPOSED 2026-08-31 (`COMPLETED.md` §113). NOTHING APPLIED — the chests stay equal and `WarChestPool` is untouched.** The floor is **DERIVED** on the route the bill named (the organisation's own bill, W-B12), two independent ways: **ANALYTIC 38 983 300 kr** (`max(bill ÷ seat-share)`, set by MP) and **MEASURED 88 182 607 kr** (bisected on "all eight finish with ZERO unpaid staff-days") — ⚠ **the arithmetic understates the need by ×2.26**, because a party given money spends it and the payroll competes. Against today's 19.2 M the measured floor is **×4.59**: W-F5's refusal to raise the pool was right by a wider margin than it knew. ⚠ **The tension, quantified: the pool a party needs on its mandate share spans 4.63 M (C) to 38.98 M (MP) — a factor of 8.4.** Funding is allocated by SEATS; the bill is driven by the OFFICE NETWORK, a personality choice, and the two are uncorrelated — so any mandate split is set by whichever small party builds the most. Four strikeable proposals; **P-D2c recommended** (scale the office plan to what a party can afford — dissolves the tension at its source and invents no money). ⚠ Any pool raise re-opens 2a-iv and the two PEND lines | **ELIAS, per line** | RULING-BLOCKED (the resolution is Elias's) | — |
| C-D3 | MP's two språkrör — answer §15/§29, record the ruling, implement it | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §114), on Elias's pre-ruling.** ⚠ **MP's statutes were READ, not assumed**: its *stadgar* elect *två jämställda språkrör* (§ 11.1) of different genders (§ 11.2) whose task is to represent the party (§ 11.4), with **no clause designating one for a debate or any other setting** (`mp.se/om/stadgar/`). So the fallback applies **because of what the statute says**: neither is seated, both are named, and the reason quotes the statute. Built: `PartyLeader` (name + the office **in the party's own word** — Sweden uses three), `PoliticalParty.Leaders` as an **ARRAY** for one party's sake (the alternative drops Per Bolund), and `ResolveDebateSeat` returning ⚠ **two DISTINCT absences** — `AbsentByDesign` (two known equals) vs `NotSourced` (the model does not know, which is not a claim that nobody leads it). Sweden's eight seeded at vintage 2022-09-11, **name and office only** — sourcing a name does not license inventing a character. Gate: nobody dropped **checked by NAME** · MP absent-by-design with both named · 7 resolved · all 9 German parties `NotSourced`, none absent-by-design. Trajectories 6 of 6 identical | CODE | SAFE | — |
| C-D4 | §38 long-term political capital, **BUILT** (R-CL3) | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §115).** `PartyCampaignCapital` on `Country` beside `ElectionHistory` (the layer the round-trip harness can PROVE), seeded for all 53 parties at their own mandate from `PartyProfile`'s **own** defaults — **no new number authored**. `SaveVersion` **2 → 3**: additive and technically loadable, but an empty list would silently mean *"no party holds any capital"*, a different state from *"every party opens at 50"*. ⚠ **The carry-over rule holds NO invented constant** — organisation moves by `newSeats / seatsAtLastUpdate`, the election's own number, on the sourced *mandatbidrag* shape; zero seats **holds still** rather than deleting the machine; **reputation does not move and the asymmetry is ASSERTED** (an election observes no reputation). Donor and grassroots networks **specified ABSENT**. ⚠ **What it is worth in play, measured not mentioned: S 50.00 → 49.53 on the first election, then held through two more** — the chamber changes **once, at the seam** between the seeded 2022 mandate and the model's predicted one, and never again. §38 is built and persisted; **it is not yet a mechanic a player can feel.** RT extended to snapshot **by party NAME** with one record moved off its seed first (an inert carry-over would round-trip 50.0 vs 50.0 and prove nothing) | CODE | SAFE ⚠ save-layer | R-CL3 |
| C-D5 | V-N3 — the swing column, against the last real result | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §116).** Built on the **completed** count against a named, sourced prior (Sweden 2018, the same `Votes2018` the results screen uses, so the two cannot disagree): S +2.10 · SD +3.05 · M −0.74 · V −1.28 · C −1.92 · KD −1.00 · MP +0.68 · L −0.89 pp. ⚠ **WITHHELD while the count is partial, with the reason on screen** — at 4 of 29 S reads 33.12 % against a final 30.80 %, so a swing computed there says +4.9 pp for a party that moved +2.10. V-N3's deviation **restated, not just struck**: the per-constituency blocker is real for a RUNNING swing and was never a blocker for the complete-count one. ⚠ **The item found that board 1h had NEVER been on film** — every `-shotelectionnight` capture showed the Desk, W-E6's own included, because an overlay Canvas draws under IMGUI; fixed via the controller's `_canvasLive` takeover. Filmed at four geometries, 8/0/0/0 each | CODE | SAFE | C-D4 |
| C-D6 | The deferral register — one home per deferral | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §117).** §6 rebuilt: ⚠ **every row now carries a TRIGGER rather than a date** — a date says when somebody stopped, a trigger says what would make it start, and only the second is re-readable a month later. Seven rows (F-7 added: the tax build, downstream of F-6 **and** of C-N4). ⚠ The rule made explicit: **a source document may DESCRIBE a deferred thing; it may not also QUEUE it** — so each row names where it is legitimately described, and the three files that still carry duplicate QUEUE rows are **deliberately not edited**, because all three are *migrate → delete* at C-G1 and editing a row out of a file about to be deleted is work done twice. **C-G1's grep is what proves the rule holds** | CODE | SAFE | ⚠ its proof is C-G1's grep |

### Track R — the ruling executed (R-CL1)

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-R1 | The ruling recorded and its reach stated | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §119).** R-CL1 on the record with its asymmetry stated: **only Sweden and Germany have a modelled election**; the other four return `NotImplemented` with a reason, so **a party there is an IDENTITY, not yet a contest**, and no screen may imply otherwise | CODE | SAFE | R-CL1 |
| C-R2 | Party selection at country selection, persisted as world state | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §119) — the model and the persistence.** `Country.PlayerPartyAbbrev` rides `SaveGame.World` beside `ElectionHistory`; `SaveVersion` **3 → 4** (an absent party is a different game state, not a harmless default). ⚠ **The PICKER is BILLED, not built** — a Canvas build on `CountrySelectorScreen` with its own films and §V row. Until it exists selection seats **the largest party in the country's own seeded chamber**, a DERIVED interim rule marked as one at the call site, not an invented default; a loaded save keeps its own party. Round-trips **by index into the roster**, with the staged save deliberately seating the SECOND-largest so a re-derivation on load could not masquerade as persistence | CODE | SAFE | C-R1 |
| C-R3 | The approval split | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §119) — and it is SAFE BY MEASUREMENT, not by intention.** `Country.PartyApprovalRating` is a NEW additive stock; `EconomyState.ApprovalRating` keeps its name and every consumer. The row read *"SAFE if additive, BASELINE if not"*: **trajectories 6 of 6 byte-identical to `traj_cc7_*`**. ⚠ Nothing moves it yet — a coupling rule needs a coefficient nothing sources — so it opens at the personal rating and PERSISTS, which is itself the change | CODE | SAFE (proven) | C-R2 |
| C-R4 | The rail cell and the win/lose rule | 🟡 **HALF BUILT 2026-09-01 (`COMPLETED.md` §135) — the RULING half is done.** D-5 (a) ruled it and `GovernmentFormation` answers the question W-G1 said could not be asked: **office is CABINET MEMBERSHIP**, and the game ends only if the player's party is not in it. ⚠ Three states, one ending: in cabinet → play continues; out → office lost; **no government formable at all → play continues and says so**, because ending a game on a modelling gap is the worst invented verdict. The approval threshold survives NARROWED to the four countries with no modelled vote. **Sweden 2022 forms M+KD+L with SD supporting from outside and S out — the government Sweden actually formed**, from sourced positions and sourced declarations, nothing fitted; proven in both directions by removing the declarations. ⚠ **Germany forms CDU+AfD+CSU on derived lines alone** — the *Brandmauer* is a declared fact not on disk, and the column says `derived only` rather than hiding it. **What remains: the rail cell and a live `CampaignSnapshot`** — see the row below | CODE | SAFE (proven) | ✅ R-CL1, C-R2, C-R3, D-5 |
| **C-R4b** | **The campaign wired to the game loop** | ⏹ **MEASURED AND SIZED 2026-09-01. Not a rail cell — a missing layer.** `GameController.cs` still says *"No player path sets `_campaignScreen`"*, and the measurement behind that is now exact: ⚠ **NO gameplay type holds campaign state at all.** `ResourcePool`, `CampaignStaff` and `OfficeNetwork` appear nowhere under `Assets/Scripts/Data`, `Simulation` or `Persistence`; `CampaignRun` is referenced from gameplay only by `ResultsScreenSnapshot`; and every field the driver fills — money, hours, volunteers, poll, staff, offices, the day's queue — is staged. A live snapshot therefore needs a persisted player campaign advancing with the day loop, plus a `SaveVersion` bump, before a cell has anything to open. **The war chest is no longer the blocker** (D-1 (c) left the pool equal and `[AUTHORED-DRAFT]` at 2 400 000 kr) and **neither is the ruling** (D-5 (a) landed). This is a build, and it is sized as one rather than half-started | CODE | BASELINE ⚠ save-layer | C-R4 |

### Tracks E, F, G

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| C-E1 | The trigger shelf re-read | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §120), on the pre-ruling's literal test.** ✅ **FIRED:** the trade axis (entry 5) — R-CL2 ruled it, **C-B3 built it**, struck from the shelf. ⚠ **NOT FIRED, and this OVERTURNS a verdict this pass was carrying: the Compass Y implemented-average.** Its trigger has two clauses and **neither holds** — §F confirmation is not on record, and **Playtest 1 produced eleven findings, none about the compass**; its §V row is still open. Restated on the shelf, **not promoted**. Four other entries NOT FIRED with their triggers restated; Riksbank-B was never on the shelf and C-B4 disposed of it | CODE | SAFE | — |
| C-E2 | The two watch items made standing guards | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §120).** **G-1** (P4 label-clipping) stays a watch under rule 3 — ⚠ this pass produced an instance, **S-17**, and the watch absorbs it rather than becoming a task. **G-2** (`MetaTextCheck`) armed, green, **widened at C-E3**, and its enumeration stated as it really is (`Assets/Scripts/UI`, `LawCatalog.cs`, `Assets/Scripts/Data` top level only — a future screen outside those roots would not be covered) | CODE | WATCH | — |
| **C-CAP** | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §101): the two capture traps armed as guards, each proven both ways.** (a) The capture REFUSES to run under `-batchmode` — exit 2 in seconds, naming the flag and the correct invocation — instead of hanging for ten minutes. (b) Every capture asserts the width it got against the width asked for: negative control 81 captured / 0 failed at 1280, **positive control 0 captured / 81 failed at an unobtainable 6000** where before it would have written 81 files at 4000 px and called it a 6000-wide pass | CODE | WATCH | — |
| **C-E3** | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §120). `PhantomGuardCheck` is the TENTH check.** Enumeration: **254 `.cs` files under `Assets/`, comment text only, every identifier ending in Check / Harness / Diagnostic — 143 names**, each required to resolve to a real type. ⚠ **It found a phantom on its first run**: `CountingRandom.cs` cited `SaveLoadDiagnostic` twice; the type is `SaveLoadRoundTripDiagnostic`. Corrected → **143 resolved, 0 missing.** It deliberately does NOT judge whether the named guard covers what the comment claims (no regex can), and a name marked as PAST is reported as history rather than failed. ⚠ **S-16 done in the same pass, as the pre-ruling directed** — `MetaTextCheck` gained a twentieth pattern, the **backtick**: C-C8 shipped a backticked type name to a player surface and the check passed it. 79 files, 2 517 literals, 0 hits. ~~**The phantom-guard sweep, armed as a check of its own.** ⚠ Opened by C-B2: `PoliSimTheme`'s doc comment stated *"the desk-seated hues below are checked against the area accents by `PartyInkHarness`"* and **no such file existed** — a constraint the code asserted was enforced, enforced by nothing. C-0.3 found the same shape on the stranded branch (`ThresholdRule.CoalitionShare`, named in `ApplyThreshold`'s contract, read nowhere). Twice in one pass is a class, not a coincidence. **Sweep every doc comment and code comment that names a `*Check` / `*Harness` / `*Diagnostic` and assert each resolves to a type that exists**, then arm it so the class cannot return — a comment that names a guard is a promise the build should keep | every named guard resolves to a real type, the sweep's enumeration is stated in its own header (rule 6), and it runs in the suite. A comment naming a guard that does not exist FAILS | CODE | WATCH | — |
| C-F1 | The Design ask consolidated to ONE paste | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §121). ONE LIVE ASK — D9, eleven rows numbered *n of N*** — folding D7/board 2b and D8's six with everything the pass added: C-B1's measured **52 of 53** marks, C-B2's hex exchange (⚠ **Sweden's eight are DELIVERED INK, not a request**; the other 45 have none and none is invented), **S-7** and **S-8**, §E4's two icons, the §A.14 chip finding, ⚠ **C-D3's språkrör question that no code ruling can answer**, and C-C8's absence finding for information. **§E4 BUILT FIRST (R-CL4)**: both `StatNodeId` members appended and named, `StatIconCoverageCheck` reports a missing icon as a **GAP not a failure** (19 of 21 resolve, 2 gaps, suite green) — **R-N1 fork logged: a check's severity changed.** `SEND_PACKAGE.md` regenerated with fresh digests, **the stale dated file DELETED and every reference repointed with no dangler**. ⚠ **P-F2 ANSWERED: no receipt exists** — `85690abf…` appears only in sending-side records, so the honest reading is the paste was never made. **Sending stays Elias's** | **ELIAS (the paste)** | SAFE | — |
| C-G1 | The document retirement | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §122).** **Five files migrated then deleted** — the elections work list (46 rows all closed; ⚠ **W-F4 and W-F5 re-homed as C-D1/C-D2 BEFORE deletion**, and both are now closed on the record), the Playtest-1 list (17 rows each pointing at its section; the two unclosed are register rows, not lost text), the Day-1 and Day-2 reports, the overnight morning report — plus `SEND_PACKAGE_2026-08-28.md` at C-F1. ⚠ **A report is not a second home for a finding.** The document-set table **re-derived**: it listed **eleven** files against a root holding **twenty-four** and charted **none** of the elections-era documents; it now charts all nineteen with a disposition each, the two new spec-lets and the register included. ✅ **`ls *.md` matches the table EXACTLY — proven by diff, not by eye — and grep shows no dangling reference anywhere, `Assets/**/*.cs` comments included** | CODE | SAFE | — |

### Track N — sized from this pass's own findings

Measurements in Tracks A and C turned up model-level questions too large to answer inside the item that
found them. Each is sized here as its own item rather than left as a paragraph in a closed record.

| ID | what | done-when | owner | class | depends on |
|---|---|---|---|---|---|
| **C-N1** | **The media system cannot move a vote.** ⚠ Established at C-A1 by enumerating call sites, not inferred: **`MomentumTracker.Apply` has exactly two call sites — `CampaignRun.cs:341` and `:451` — and both are the argument to `PollingSystem.Conduct`.** Election day counts `truePreference`, which `CurrentPreference` → `PreferenceModel.Preference` produces and which never sees momentum. So the whole chain **media (W-B9) → coverage → momentum (§22) → poll terminates before the ballot**, and momentum's own doc comment says so: *"shifts where a race APPEARS to be without changing the underlying preference."* The question this item exists to answer is **whether that is the design or an omission** — §13/§14 build a media system whose only route to a result is through a player's or an AI's *reaction* to a poll, and §39's chain gives coverage no persuasive term at all. Two exits, and they are not equivalent: **(a) it is intended** — perception-only media is a defensible model, and then the record must say plainly that no amount of coverage changes a vote, and W-B9's own claims are re-read against that; **(b) it is an omission** — coverage should reach persuasion, which is a §42 chain change with its own explained baseline family | the answer is on record either way, with the call sites cited; if (b), the chain change is sized but NOT built here | ELIAS (the ruling) then CODE | RULING-BLOCKED | — |
| **C-N2** | **The local-action mechanism defect: optimising personalities refuse the action they most prefer.** ⚠ Established at C-A2: the grassroots profile carries door-to-door affinity **2.2** and `EnthusiasmValue` **1.6** — a ~3.5× thumb, the strongest pro-local weighting in the roster — and knocks **zero** doors; both grassroots parties run zero; not one rally is held by any of eight parties; the only personality that acts locally is the chaotic one, which is not optimising. §33 is not blind to local value, so the defect is in the mechanism: the three local actions hold the set's **largest hour costs** (rally 4 h, door 5 h, town 3 h) against its **smallest channel reaches** (0.06 / 0.02 / 0.01) while §33 scores **per hour**, against a free interview (0 kr, 2 h, 0.20) and a 5 000 kr social post (1 h, 0.12). ⚠ **And the door-to-door ACTION is largely redundant with a mechanism that already runs for free** — W-B4's offices knock doors through their own daily operations, outside the eight actions and outside the AI's choice. **The fix is sized separately and is a §12 verb-set question — what is the door-to-door action FOR, given the offices already knock** — not a magnitude to nudge. §34's "no single dominant approach" bar is not met today | the question is answered as a design ruling and the verb set re-derived from it. ⚠ **NO AFFINITY, `EnthusiasmValue`, HOUR COST, REACH OR PRICE IS ADJUSTED** — tuning any of them would bury the finding rather than answer it | ELIAS (the ruling) then CODE | RULING-BLOCKED | interacts with calibration entries 3 and 10 |
| **C-N3** | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §111).** Built and armed as the fourth simulation check, with a **batchmode entry for that group** (it had a menu item and nothing else, so a check in it could never fire in CI — the same failure C-0.4 fixed for the nine). First run: **28 LIVE · 8 RETIRED · 1 GAP · 0 dead-and-unlisted · 0 not-exercised · 0 stale**, and it earned its keep immediately — it **corrected S-18** (the rate lever is live in the eurozone trio) and **found C-N6**. ~~⚠ **NOTHING IN THIS BUILD ASSERTS THAT A PLAYER-FACING LEVER MOVES THE MODEL, and three levers have gone dead without anything failing.** The interest rate (S-18 — `ApplyInterestRateChanges` routes every country to its central bank and never reads the field, whose long doc comment still describes a live lever); the tax dials (§107 — a real revenue impulse and an output multiplier of **exactly 0.000** at every horizon); and the two demographic levers, which go dead the day the cohort substrate lands if only the first of their two hops is re-pointed (§108). **Three is a pattern, not an accident.** The item is a guard of the same shape as C-E3's phantom-guard check: enumerate the fields of `PolicyDecision`, step each one from a country's own seeded value, and **assert that something in `EconomyState` moves** — reporting a dead lever as a NAMED GAP rather than a failure, on `PartyMarkCoverageCheck`'s precedent, since some are dead by ruling. ⚠ **The leave-one-out machinery C-C10 built already does most of this**; the guard is largely a re-use, which is why it is worth sizing now | every `PolicyDecision` field is either shown to move the model or listed as a dead lever with the ruling that killed it; armed as a check with its enumeration in its own header | CODE | SAFE | belongs with C-E3 |
| **C-N4** | ✅ **MEASURED AND PROPOSED 2026-08-31 (`COMPLETED.md` §123). NOTHING APPLIED.** ⚠ **THE LOSS POINT IS CONSUMPTION.** A +10-point tax rise moves **four** `EconomyState` fields (`ApprovalRating`, `Budget`, `GovernmentDebt`, `Gini`) and leaves **thirty-two** unmoved, including `GDP`, `Consumption` and `ConsumerConfidence`. `MacroSystem.ApplyNationalAccounts` computes consumption as a fixed share of PRIOR GDP times interest and confidence — ⚠ **there is no disposable-income term**, so a tax rise takes money from households and the C term never learns of it, while spending enters the same identity directly as G. The diagnostic **FAILS if `Consumption` ever moves**, so the finding retires itself. Proposal: give C a disposable-income input; ⚠ **magnitude stays BILLED** (Romer & Romer is a US narrative-shock estimate, not transplanted); the spending band is a hard constraint and `ResponsivenessAuditHarness` is the acceptance test; BASELINE, not in the same pass as C-N5 | **ELIAS** then CODE | RULING-BLOCKED | after Track D; before C-N5 |
| **C-N5** | ✅ **MEASURED AND PROPOSED 2026-08-31 (`COMPLETED.md` §123). NOTHING APPLIED — and it CORRECTS C-C11's own headline.** ⚠ **The implied Okun at the LANDING year is −0.498, INSIDE Ball/Leigh/Loungani's −0.23…−0.54; `OkunCoefficient` is 0.5 and is NOT wrong.** C-C11's −0.007 was the same quantity five years later, after it had decayed. **The defect is a SPECIFICATION mismatch**: the model applies the coefficient to a GROWTH gap and mean-reverts to NAIRU, the literature applies it to an OUTPUT gap — so the level gain persists (11.13 at year 8) while the unemployment gain decays to nothing. ⚠ **The fix is blocked by a shelf entry whose trigger this item FIRES**: a gap-form Okun needs a level output gap, and the identity has no government-consumption block. Proposal: **do NOT scale the constant** (tuning a right constant for a wrong specification is tuning to pass); re-specify after the G block; report the literature's RANGE; after C-N4, never together | **ELIAS** then CODE | RULING-BLOCKED | after C-N4 |
| **C-N6** | ✅ **DECIDED AND LOGGED 2026-08-31 (`COMPLETED.md` §123): THE FIELD STAYS, ITS CONSUMER IS BILLED.** History answers the fork — CLAUDE.md's Round-3 record calls the dial *"tracked/displayed but this pass does NOT model differing domestic-vs-international returns — a deliberate scope simplification, honestly disclosed, not a gap"* and **names the intended consumer**. Deferred on purpose, not forgotten, so the gap note now lives at the field. ⚠ **The bill cannot be guessed**: it needs a sourced domestic-vs-international return spread per country, and **Norway's GPFG — this model's own anchor — invests almost entirely ABROAD by mandate**, so it cannot supply a domestic leg. Until then the dial is honest scenery and **no player-facing surface may imply otherwise** | **ELIAS** then CODE | RULING-BLOCKED | — |

---

## 2. Owner ELIAS — nothing in this pass can close these

| ID | what | why no measurement replaces it | source |
|---|---|---|---|
| E-1 | **One paste** — C-F1's regenerated package to Design, then mark §S SENT with the date | sending is Elias's by the §E2 convention | `MISSING_PREREQUISITES.md` §S |
| E-2 | **The sitting** — §V, 52 rows, `../PoliSim-captures/sv_index.html` | a capture is a harness film, not Elias's eyes (rule 3's third layer) | §V |
| E-3 | **Felt verdict 1** — decision density | a staged save, loaded and played | §P |
| E-4 | **Felt verdict 3** — the Trade bill's costs | a staged save, loaded and played | §P |
| E-5..E-24 | **The 20 play-calibration entries** | every one is a number awaiting a loop to judge it against; Track R is what makes that loop exist, the judging is still his. **Nothing here is tuned to make a gate pass** | `ELECTIONS_PLAY_CALIBRATION.md` |
| E-25 | C-C6's basis ruling — **executes as written unless struck** | — | the Playtest-1 work list (retired at C-G1, 2026-08-31; every row points at its `COMPLETED.md` section) |
| E-26 | C-C11's recalibration recommendations — strike or bless, per line | — | idem |
| E-27 | C-C12 and C-C13's spec-lets — ruled before any code | — | idem |
| E-28 | C-D2's pool resolution | a design question, not a measurement | `COMPLETED.md` §83 |
| E-29 | C-D3's språkrör answer, if he wants the call rather than mine | — | the W-F6 finding |

*Verdict 2 ("still not independent") is NOT a row here — it became P-D1 and is discharged by C-C7.*

## 3. Owner DESIGN — waiting, once E-1 is done

| ID | what | note |
|---|---|---|
| D-7 | Board 2b, the Policy Web drawn to be read (the tenth request, Annex G) | written 2026-08-28, **never pasted** |
| D-8.1 | Party identity marks — 52 of 53 undrawn; the seven remaining Swedish ones are what 13 September needs | rule 9a: original art by silhouette, never the registered logo |
| D-8.2 | Party colours for five countries — **a ruling, not art** | Sweden's eight are sourced; we will not pick 30 colours by eye |
| D-8.3 | A drawn valkrets map | the single asset that would most change the campaign map |
| D-8.4 | Election night's paper (V-N1) — a 9-sliced sprite, or a ruling that flat is correct | |
| D-8.5 | The verdict stamp (W-E5) | |
| D-8.6 | Modal or stage for the debate (V-N2) — **a question, not an asset** | |
| D-E4 | The two Society-row icons (youth unemployment, life expectancy) | the two `StatNodeId` members are ours and land under R-CL4; the icons are Design's |

## 4. Owner CALENDAR

| ID | what | date |
|---|---|---|
| K-1 | The seed refresh from Sweden's real result | **13 September 2026** — scheduled, not blocked |

---

## 5. TRIGGER — real work whose trigger has not fired

Nothing here is startable and no named party owes anything. A trigger firing moves the row into §1.

| ID | what | trigger |
|---|---|---|
| T-1 | Per-scenario term accumulation | the first scenario whose epilogue reads wrong without it |
| T-2 | Investment deepening (R-Q5e) | a capital stock ships, or I/GDP measures cyclical |
| T-3 | The identity's government-consumption block | the first mechanic that needs the level output gap to mean something. Measured gaps: USA −14.5 %, Poland −7, Italy −4.5, Germany −2.7, Sweden −0.8, France −0.5 |
| T-4 | Trade volumes indexed to GDP | pass 6's deferred set |
| T-5 | Retaliation against a base-dial hike | idem |
| T-6 | Retaliation memory / lag | idem |
| T-7 | The coupling queue Q6–Q10 | each at its own named trigger; nothing is startable until one fires and Elias rules |

⚠ **Two shelf entries have FIRED and are no longer triggers:** the trade axis for the vote (→ C-B3) and the
Compass Y implemented-average (its trigger — the first play reading the compass against six seeded
portfolios — has happened). C-E1 executes the move. **Riksbank-B was never on the shelf**; it waited on §D
and C-B4 disposes of it.

## 6. DEFERRED — recorded, deliberately not built

### ⚠ TWO STANDING RULES, ADDED 2026-09-01

#### R-T1 · Every trigger carries its own DEFENCE CLAUSE

**The evidence.** Of the five deferral triggers re-read on 2026-09-01, **three were written to survive the
misreading that would fire them early** — *"PLAYABLE, not merely simulated"* (F-3), *"before playable, not
before trusted"* (F-4), *"a campaign the player actually RUNS"* (F-1). ⚠ **F-6's was the one sentence with
no defence in it**, a single condition with nothing saying what would *not* satisfy it — and it fired
unnoticed on the day it was written down, leaving its row two days out of date.

**The rule, from now on.** A trigger is written in two parts:

1. **the literal condition** — the sentence that must be true, as written;
2. **the defence clause** — *what does not count as satisfying it*, naming the nearest plausible
   misreading.

⚠ **A trigger with no defence clause is not finished**, and the reviewer's job at every re-read is to test
the sentence literally rather than the intent behind it. **Retrofitted below where it was missing.**

#### R-T2 · An ad-hoc shell probe is the weakest evidence in this project

**The evidence, one day's worth.** Three shell probes were wrong on 2026-09-01 and **every one of them
looked right in its output**:

- a `head -1` that matched a doc comment and reported the constant `PersuasionPerCompatibilityPoint` as
  **40** when it is **40 000**;
- a type-versus-FILE-name match that reported `CampaignCalendar` as missing and **"corrected" a correct
  document reference into a wrong one** — the type is real and simply shares a file with another;
- a PowerShell one-element array unrolling on return, so `$tokens[0]` was a **character**, filing 17 sitting
  rows under the wrong era while the page rendered perfectly.

**The checks written the same day caught two of the three.** The third was caught by a fourth check within
the hour of being armed.

**The rule.** ⚠ **A shell or PowerShell probe may not carry a finding on its own.** It is fine for
orientation and for deciding where to look. **Anything load-bearing — a number that enters a document, a
claim that closes an item, a count that sets a ratchet — becomes an armed check, or is re-derived by one
before it is written down.** The reason is not that pipelines are more error-prone than C#; it is that
**a pipeline is evidence nobody re-runs**, so its errors have no second chance to be found.


#### R-T3 · After an instrument fix, ENUMERATE every other consumer before the item closes

**The evidence.** §158 fixed one instrument — the name scanner that a comment could silence — and closed
the item there. ⚠ **The question *"what else shares this instrument?"* went unasked**, and when §160
finally asked it the answer was **four checks**, not one. The enrolment built the same day then found a
**fifth** (`DeadStateCheck`), and that fifth was hiding **two genuinely dead methods**.

**The rule.** An instrument fix is not finished when the instrument works. It is finished when **every
other consumer of that instrument has been enumerated and each is either fixed or exempted with a stated
reason** — and **the enumeration is named in the fix's own record**, so the next reader can see what was
covered rather than assuming it was all of it.

⚠ **The enumeration is the deliverable, not the fix.** A fix with no enumeration beside it is a claim
about one call site dressed as a claim about a class.

**RETROFITTED to this week's instrument fixes:**

| instrument fixed | consumers enumerated | what the retrofit found |
|---|---|---|
| **the comment-blind name scanner** (§158) | 11 source-reading checks | ⚠ **five** must strip (one, `DeadStateCheck`, found only by the enrolment — and it was masking **two dead methods**); four exempt with reasons; two more (`CommentImmunityCheck`, `RatchetSlackCheck`) found unenrolled **by their own census** |
| **`RatchetLedger`** (§157) | 9 check files declaring a ceiling or ratchet | ⚠ **three unreported** — `CohortAgingStepDiagnostic`, `EvidenceDiscriminationCheck`, `PublicationCadenceCheck`. Three ratchets sat outside the audit that exists to compare a bound with its measurement |
| **the capture-identity token** (S-20) | 3 Canvas takeovers, all stamping; driver default `imgui` | ✅ **complete — no finding.** The set that stamps is exactly the set `PlayerReachabilityCheck` enumerates |
| **the type-is-its-file assumption** (S-35) | `DocumentClaimCheck` (fixed at S-35), `EvidenceDiscriminationCheck` | ⚠ **one finding**: a registered check whose file is not named after its type was reported and then **silently skipped**, so clause A verified nothing about it while the summary read clean. It fails now |
| **the width assertion** (trap 2, `UiScreenshotDriver`) | the geometry argument PAIR — `-shotwidth=` and `-shotheight=`, set on one line of `UiScreenshotCapture` from one ruling | ✅ **ENUMERATED 2026-09-01, and it found the other half unguarded for a month.** ⚠ The phrase resolves uniquely after all: trap 2 is the only assertion in the repo comparing a requested size with a captured one, and its own comment reads *"THE WIDTH ASKED FOR MUST BE THE WIDTH CAPTURED."* R-T3's question — *what else shares this instrument?* — has exactly one answer, **HEIGHT**, and nothing had ever checked it. ⚠ **And the reason it was missed is the finding**: `view.position` is a WINDOW rect, the Game View carries a 21 px toolbar, so a run asking for 950 has always captured **929** — this project's own records say `1600×929` while every command in them says 950. Width has no side chrome and matched exactly, so the naive assertion was true on one axis and would have been false on the other **for a stable reason**, which is why symmetry was never suspected. Armed with `GameViewChromeHeight` named, dated and carrying its defence clause; proved both directions |

⚠ **The last row was carried NOT-DONE for a week and is now closed** — the rule working on itself in both
directions: an enumeration that cannot be completed is reported as incomplete, and one that later CAN be is
completed rather than left standing as a permanent apology. ⚠ **It was worth completing**: the owed
enumeration is where the unguarded half was hiding.
|---|---|---|
**C-D6 (2026-08-31) gave every row a TRIGGER rather than a date.** A deferral with a date says when
somebody stopped; a deferral with a trigger says what would make it start, and only the second is
re-readable a month later. ⚠ **The distinction this section rests on: a source document may DESCRIBE a
deferred thing; it may not also QUEUE it.** Description is what `ELECTIONS_GAP_TABLE.md` and
`ELECTIONS_CAMPAIGN_SPEC.md` are for and none of it is touched here; what may not exist twice is a row
that reads as open work.

| ID | what | ⚠ the TRIGGER that would start it | described (not queued) in |
|---|---|---|---|
| F-1 | §37 staff progression — staff who get better at the job | a campaign the player actually runs, so a staffer's improvement is something they can feel. ⚠ **DEFENCE (R-T1):** a campaign SIMULATED by a harness does not count, nor does `CampaignRun.Simulate` gaining a caller in editor code. The literal condition is a player, in a running game, spending a campaign. Today the campaign layer is harness-only, and progression over a run nobody plays is invisible by construction | `ELECTIONS_CAMPAIGN_SPEC.md` §37; the gap table |
| F-2 | §2's other election types — referendum, leadership contests | a ruling that the game is about more than a parliamentary term. ⚠ **DEFENCE (R-T1):** a BIGGER parliamentary term does not count. R-CL1 gave the player a party and D-5 (a) made losing office survivable; both enlarge the term and neither rules that the game is about anything else. Both are real mechanics with real Swedish precedent, and neither is reachable from the loop the game has | spec §2; the gap table |
| F-3 | France's constituency model (R-EL10) | a decision that France must be PLAYABLE, not merely simulated. ⚠ **UNSIZED, UNSTARTED, and no placeholder or approximation is to be built** — two-round SMD needs a 577-constituency model with runoff behaviour, a large sourced build serving one country. The data (the Ministry's data.gouv family) is where it always was | `POLISIM_MASTER_ROADMAP.md`; `ElectionsData/DATA_BILL.md` carries the *data* bill, which is a different thing from this *build* deferral |
| F-4 | Italy's sub-national stages | **Italy becoming playable** — explicitly *before playable, not before trusted*: the proportional stage already reproduces exactly, so the model is trustworthy without them. Needs the per-*circoscrizione* and per-*collegio cifre elettorali* plus the art. 84 cascade | `ElectionsData/DATA_BILL.md` (the data), `italy/rosatellum_allocation.md` (the statute) |
| F-5 | The gap table's nine N/A sections | nothing — ⚠ **these are principle and illustration sections, not work.** They are listed here so that a future reader counting "unbuilt sections" does not mistake them for a backlog | `ELECTIONS_GAP_TABLE.md` |
| ~~F-6~~ | ✅ **PROMOTED OUT OF DEFERRED 2026-09-01.** Its trigger — *"`POLISIM_COHORT_SPECLET.md` being RULED by Elias"* — **FIRED at D-4 (a), `COMPLETED.md` §130, and this row outlived it by two days.** It is now **P-I2 stage 3**, live work whose remaining question is a SHAPE (D-15), not a deferral. Stages 1 and 2 are built; stage 3 was built and reverted on its own measurement (§141) and its anchor is sourced for all six (§142). ⚠ Its second clause was stale too: *"it now also gates C-D1"* — **C-D1 was built at §136** | ~~the spec-let being ruled~~ **fired** | `COMPLETED.md` §§130, 141, 142, 156 |
| F-7 | The tax instruments build (C-C12's §S1–S7) | **`POLISIM_TAX_SPECLET.md` being RULED**, ⚠ **and it is downstream of F-6** — a bracket schedule applied to a single average income is arithmetically a flat rate — **and of C-N4**, since detailed instruments behind a lever with a zero output channel are detail without consequence | `POLISIM_TAX_SPECLET.md` |

*§38 has LEFT this list — R-CL3 ruled it built, and it was, at C-D4 (`COMPLETED.md` §115).*

⚠ **Three documents still carry duplicate QUEUE rows for F-1, F-2 and F-4** —
the elections work list (retired at C-G1, 2026-08-31; its 46 rows are closed and its record is `ELECTIONS_PROTOTYPE_LOG.md`) and the two day reports. They are **not edited here on purpose**: all
three are scheduled *migrate → delete* at C-G1, and editing a row out of a file that is about to be
deleted is work done twice. **C-G1's grep is what proves the "exactly one home" rule holds**, and this
row is the note that it must.

### ⚠ THE DEFERRAL REGISTER, RE-READ AGAINST THE REPO — 2026-09-01

**The §9.3 shelf item, and it found what the Compass Y entry's literal test found once before: a trigger
that had FIRED while its row still read as deferred.** Every row's trigger checked against the repo as it
now stands, not against the day it was written.

| row | verdict | against the repo of 2026-09-01 |
|---|---|---|
| **F-1** staff progression | **NOT FIRED** | The trigger is *"a campaign the player actually runs"*. `CampaignRun` is still UNWIRED — `UnwiredSubsystemCheck` names it as a wired type with an uncalled entry point, and C-R4b holds it. **The trigger is unchanged and correct.** |
| **F-2** other election types | **NOT FIRED — but its distance has changed** | The trigger is *"a ruling that the game is about more than a parliamentary term"*. Not given. ⚠ **What HAS changed: R-CL1 gave the player a party and D-5 (a) made losing office survivable** — a leadership contest now has an actor and a stake it did not have. Restated, not started |
| **F-3** France's constituency model | **NOT FIRED** | *"a decision that France must be PLAYABLE"*. Nothing since. **UNSIZED, UNSTARTED, and no approximation is to be built** — unchanged |
| **F-4** Italy's sub-national stages | **NOT FIRED** | *"Italy becoming playable"*. Nothing since. ⚠ **What changed is visibility**: `Rosatellum` is now counted by `UnwiredSubsystemCheck`'s UNREACHABLE class under a ceiling, so the deferral is guarded by a number rather than by memory |
| **F-5** the gap table's nine N/A sections | **NOT FIRED, by construction** | Its trigger is *"nothing"* — it is a note against miscounting, not work. ⚠ **Its own literal test passes: it is still not work**, and the row still earns its place by stopping a future reader counting it as backlog |
| **F-6** P-I2, the cohort substrate | ⚠ **FIRED — AND THE ROW WAS STILL SAYING DEFERRED** | The trigger was *"`POLISIM_COHORT_SPECLET.md` being RULED by Elias"*. **Elias ruled it — D-4 (a), `COMPLETED.md` §130 — and stages 1 and 2 were BUILT the same day** (§§130–131), stage 3 built and reverted on its own measurement (§141). ⚠ **And its second clause is stale too**: *"it now also gates C-D1"* — **C-D1 was built at §136.** This row has been describing a world two days old |
| **F-7** the tax instruments build | ⚠ **TWO OF ITS THREE CONDITIONS HAVE FIRED** | Its trigger is a conjunction: `POLISIM_TAX_SPECLET.md` ruled, downstream of F-6, downstream of C-N4. **D-3 was RULED (b) at §133**; **C-N4 was BUILT at §126**; **F-6's spec-let is ruled and its substrate part-built.** ⚠ What actually remains is narrower than the row says and should be stated as such: *the cohort substrate is not WIRED* (stage 3 reverted, §141), so a bracket schedule still has one average income to apply itself to |

⚠ **The pattern, and it is the reason this re-read is a standing shelf item rather than a one-off:** a
deferral's trigger is written when the work is furthest from happening, and nothing re-reads it when the
world moves. **F-6's trigger fired on the day it was written down and the row outlived it by two days**;
F-7's fired twice without anyone noticing the conjunction had shrunk. **Neither is re-planned here** — the
verdicts are the deliverable, and F-6 leaves the deferred list at the next item that touches the cohort
chain rather than being re-homed inside a re-read.


### ⚠ F-6 PROMOTED OUT OF DEFERRED, F-7 SIZED, AND EVERY TRIGGER RE-READ **LITERALLY** — 2026-09-01

**The literal test, and why it is stated before the table.** The Compass Y precedent: *"a play happened"*
is not *"the first play that reads the compass"*. A trigger is a sentence, and it fires only when the
sentence is true **as written** — not when something adjacent to it has happened.

#### F-6 — PROMOTED. It is no longer deferred and has not been for two days.

**Its trigger, verbatim:** *"`POLISIM_COHORT_SPECLET.md` being RULED by Elias."* ⚠ **Elias ruled it —
D-4 (a), `COMPLETED.md` §130 — and stages 1 and 2 were built the same day.** The sentence is true as
written. **F-6 leaves §6.**

**What it is now, precisely:** P-I2 **stage 3**, the retirement of the scalar demographics onto the cohort
substrate. Stages 1 and 2 stand; stage 3 was built and **reverted on its own measurement** (§141) because
the step had no steady-state anchor — Germany and the USA reach `MaxPopulation`, Italy, Poland and Sweden
reach `MinPopulation`, and the finding is carried as a ratchet at two.

⚠ **Its blocker is no longer "no anchor exists".** §142 probed and confirmed one for all six: Eurostat
`proj_23np` (`projection=BSL`) for the EU five, and the US Census 2023 National Population Projections
`np2023_d1_mid.csv` **by single year of age**. **What remains is a SHAPE, not a figure** — and that is a
decision, so it is written as one: **D-15**.

#### F-7 — the last third, stated precisely

**Its trigger is a conjunction of three**, and two have fired: `POLISIM_TAX_SPECLET.md` ruled (D-3 (b),
§133) and C-N4 built (§126). ⚠ **The third, read literally, is not "F-6 exists" but "a bracket schedule
has more than one income to apply itself to"** — the spec-let's own words are that *a bracket schedule
applied to a single average income is arithmetically a flat rate*. **That needs the cohort substrate
WIRED**, which is stage 3, which is F-6's remaining work. **NOT FIRED, and it cannot fire before F-6 does.**

⚠ **This is sharper than "downstream of F-6"**: it is downstream of stage 3 specifically, and stages 1 and
2 landing does nothing for it. The row said F-6; the truth is narrower.

#### Every remaining trigger, re-read as a sentence

| row | its trigger, verbatim | literally true today? |
|---|---|---|
| **F-1** | *"a campaign the player actually runs"* | ⚠ **NO.** `CampaignRun.Simulate` is never invoked (C-R4b); the campaign layer is harness-only. Not *"a campaign was simulated"* — **a campaign the PLAYER RUNS** |
| **F-2** | *"a ruling that the game is about more than a parliamentary term"* | ⚠ **NO — and the near-miss is worth naming.** R-CL1 gave the player a party and D-5 (a) made losing office survivable. That is a *bigger parliamentary term*, not a ruling that the game is about more than one |
| **F-3** | *"a decision that France must be PLAYABLE, not merely simulated"* | **NO.** No such decision. ⚠ The row's own words already carry the literal test — *playable, not merely simulated* |
| **F-4** | *"Italy becoming playable — explicitly before playable, not before trusted"* | **NO.** `Rosatellum` returns `NotImplemented` and is counted UNREACHABLE. ⚠ The trigger anticipates its own misreading and refuses it |
| **F-5** | *"nothing"* | **N/A by construction.** It is a note against miscounting. ⚠ Its literal test still passes: it is still not work |

⚠ **The pattern the literal re-read exposes: three of the five triggers are written to survive exactly the
misreading that would fire them early** — *playable not simulated*, *before playable not before trusted*,
*a campaign the player runs*. **Whoever wrote them had already been burned once**, and F-6's fired without
anyone noticing because its trigger was the one sentence with no defence in it: a single condition that
came true on a day nobody was re-reading the register.
## 7. WATCH — standing guards, never tasks

| ID | what | state |
|---|---|---|
| G-1 | The label-clipping class (P4) | open as a watch under rule 3; instance #14 fixed at `a331e82`; nothing startable until a capture shows another |
| G-2 | `MetaTextCheck` | armed as the ninth check at `1df2917`; scans `Assets/Scripts/UI`, `Assets/Scripts/Simulation/LawCatalog.cs` and `Assets/Scripts/Data` (top level only) |

## 8. Standing gaps and findings that own no item yet

Named so they are not rediscovered as surprises.

| ID | what | where it bites |
|---|---|---|
| S-1 | **The electorate does not move with the simulation.** §8 couples it to the economy; nothing does that yet, so two elections in one game return the same chamber | C-D4's carry-over rides on top of it; C-D5's swing column will show no swing from the model's own play |
| S-2 | Germany 2025 sits on a threshold cliff — BSW missed 5 % by 0.02 pp — so a model with ~1.5 pp of error lands on the wrong side and ninety seats move | reported, never tuned. The weakest point in the seat model |
| S-3 | ✅ **CLOSED 2026-09-01 (`COMPLETED.md` §165) — and the row was stale in both halves.** It is not *"SD keeps 6 of 38"*: measured today it is **SD 6, V 12, MP 12 — 30 member-days across three parties**. ⚠ **And the two parties that hired FEWEST have the WORST record** (V and MP hired one each and went unpaid 12 of 56 days, 21 %; SD hired two and missed 6 of 112, 5 %) — **unpaid days track INCOME, not headcount**, which is the opposite of what "keeps N unpaid days" reads as. The arithmetic closes to the krona from two constants read out of source: an 8-week (56-day) campaign at `CampaignStaff.SalaryPerDay = 1 800`. Assertion **1i** already conserved the payroll; ⚠ **what nothing checked was whether an unpaid day is POVERTY or a BUG**, since the books balance either way. New assertion **1j**: a party that went unpaid must finish below one day's salary. Measured SD 0 kr, V and MP 1 500 kr — genuinely one day short. Proved to discriminate at the real margin | CODE | `COMPLETED.md` §165 |
| S-4 | Five of §4's eight axes are UNDEFINED and are NOT centred; `FlatIssueMatch = 0.5` stands in for per-issue positions that exist for no party anywhere | W-F2's bill |
| S-5 | Sweden's TOP issue (EB105: "threats to democracy", 26 %) is not representable in §6; the harness's four issues are Sweden's second through fifth | W-F3's bill |
| S-6 | Sweden 2014 does NOT reproduce through the same allocator (6 seats absolute error) — the reason every "reproduces" claim is scoped to 2022 | C-A4's rule; migrated from the stranded branch at C-0.3 |

---

## 9. Corrections — rows a source document states as open that the repo says are closed

The repo outranks the document. These are recorded, not re-worked.

| what a document says | what the repo says | commit |
|---|---|---|
| `PartyMarkCoverageCheck` reports "PARTY SYSTEM NOT PRESENT — VERIFIED NOTHING" (`MISSING_PREREQUISITES.md` §E2) | real accounting: **53 seeded, 1 resolving, 52 gaps, 0 errors**; R3's verification obligation DISCHARGED | `a289e1e` |
| "no party seeds exist on main"; "count unknown until the seeds land" (§E2) | 53 parties on main; the count is 53, of which 52 are undrawn | `a289e1e` |
| item 10 is the unbuilt spine; the stranded branch is "preserved UNINSPECTED" (§D0) | item 10's core shipped; the branch is inspected and disposed at C-0.3 | `a289e1e`, C-0.3 |
| "nothing was wired and the election system remains unreachable from any gameplay path" (the roadmap's 13 September minimum) | wired; what is true on 13 September is W-H5's status line | `a289e1e` |
| 2a-iv PEND "at 0.291" (the clearance list) | the harness prints est/grass **0.269**, prof/est 0.306 post-W-F1; 0.291 was the pre-W-F1 reading | `806fb17` |
| "measure what pool a playable field needs" (C-D2 as listed) | already measured: the *mandatbidrag* split clears both PEND lines (0.430 / 1.405) **and bankrupts five of eight**; the pool is 2 400 000 kr, one party's budget | `cc95e03` |
| "produce the hex set for every seated party" (C-B2 as listed) | party colour is not a field on `PoliticalParty`; ink lives in `PoliSimTheme` and only Sweden has it | `a289e1e` |
| §E5 is an open ask (the 1i–1n note) | CLOSED end-to-end 2026-08-28, both sides | `COMPLETED.md` §46 |
| §E6's boards are pending | LANDED 2026-08-28 | `COMPLETED.md` §41 |
| the roadmap's document-set table lists eleven root files | the root holds **21** | C-G1 corrects it |
| the gap table's class column: §3, §12, §17, §21, §22 read unbuilt | built at W-B1, W-B3, W-B8, W-B10, W-B10. The honest count is **3 truly unbuilt of 44** — §5, §37, §38 (§38 now built at C-D4) | C-0.2 re-derives |
| `SEND_PACKAGE.md` states the request doc at 65 004 bytes, digest `85690abf…` | the file on disk is 69 753 bytes — D8 was appended after; **the readback glance it prescribes would fail** | C-F1 regenerates |
| W-F4 and W-F5 are closed items | closed **by STOPPING**, and their real work is live here as C-D1 and C-D2 | `cc95e03` |

---

## 10. Findings opened by this pass

| ID | what | owner | where |
|---|---|---|---|
| S-7 | **`#753838` is drawn for two parties — Sweden's S and V.** Their published `fargkod` values differ only in darkness and the desk seating keeps hue while replacing saturation and value, so a 106-seat party and a 24-seat party render identically in a hemicycle arc, a legend swatch and an election-night row | DESIGN (D8-2) | `COMPLETED.md` §91 |
| S-8 | **Six of Sweden's eight party inks sit inside the derived legibility floor** — closer to an area accent (8.7°, Political/SovereignWealth) than two area accents sit to each other. KD 0.9° from Fiscal, SD 2.8° and L 4.6° from Global. Only C and MP clear it. The constraint the four replaced archetype inks were designed around does not hold for the sourced hues | DESIGN (D8-2) | `COMPLETED.md` §91 |
| S-9 | ⚠ **SIZED AS ITS OWN ITEM 2026-08-31 — see register row C-N2.** **Local campaigning is a bad bet by construction** — the three local actions hold the largest hour costs and smallest channel reaches in the set while §33 scores per hour, so only the non-optimising personality does any. The door-to-door ACTION is also largely redundant with the offices' own daily operations. A fix must answer what the action is FOR, which is a §12 verb-set question | ELIAS / CODE | `COMPLETED.md` §88 |
| S-10 | ⚠ **SIZED AS ITS OWN ITEM 2026-08-31 — see register row C-N1.** **The media system cannot move a vote.** `MomentumTracker.Apply`'s only two call sites (`CampaignRun.cs:341`, `:451`) are both the argument to `PollingSystem.Conduct`; election day counts `truePreference`, which never sees momentum. Media → coverage → momentum → poll terminates before the ballot | ELIAS then CODE | `COMPLETED.md` §87 |
| S-11 | ⚠ **SIZED AS ITS OWN ITEM 2026-08-31 — see register row C-E3.** **A doc comment can name a guard that does not exist.** Twice in one pass: `PoliSimTheme` cited `PartyInkHarness` (no such file, C-B2) and the stranded branch's `ApplyThreshold` named a `CoalitionShare` rule it never read (C-0.3). A comment naming a check is a promise the build should keep, and nothing was keeping it | CODE | `COMPLETED.md` §§86, 91 |
| S-12 | ⚠ **The Estimated Effects panel's "±5-10% margin of error" is AUTHORED and re-rolled at random.** `FormatMoneyEstimate` draws a margin from `_previewRandom` between `MinPreviewMarginPercent` and `MaxPreviewMarginPercent`, multiplies the model's figure by it, and prints the result as `(±$X)` — a random number generated for display, presented beside a measurement. Found at C-C1, which removed it from its own three rows and filed this rather than silently re-cutting a long-standing convention across four macro rows it does not own.  ✅ **RULED 2026-08-31 (Elias): REMOVE it — do not re-roll it, do not stabilise it.** `PreviewTurn` is deterministic, so the honest form is the point with its scope stated, matching C-C1's resolution on the Budget surface. Sized as its own item, **C-C14**, to be taken when C-C5 and C-C6 are clear of that file — not inside a currency item | CODE (needs a display ruling) | `COMPLETED.md` §94 |
| S-13 | **On a focused STAT node the Policy Web's ~40 incoming edges converge on one point and their arrowheads stack at that node's rim.** Direction is honest there; suppressing it would be deciding the diagram reads better without the model's own direction. A comprehension judgement, so it belongs to board 2b | DESIGN (D7) | `COMPLETED.md` §96 |
| S-17 | ✅ **CLOSED 2026-09-01 (`COMPLETED.md` §167). Both silent defaults are now guarded, and the finding was REPRODUCED LIVE in the closing.** `-shotheight`'s default was unchecked until M-S2 armed trap 2's height half; the **geometry itself** was unchecked until now. `UiScreenshotCapture` refuses any pair outside S-17's four — **1280×720 · 1600×950 · 1920×1080 · 2560×1440** — naming them, with `-shotoffstandard` as a loud opt-out that announces the film as a different test rather than silencing anything. ⚠ **Reproduced today**: identical code, identical width, 80 px of height — `1280×800` films **8 text overflows**, `1280×720` films **0**. (The record says 13; today's tree gives 8. **The count moved and the phenomenon did not**, which is the honest way to carry it.) ⚠ **And the four geometries independently confirm `GameViewChromeHeight = 21`** — their filmed view heights 699/929/1059/1419 sit exactly 21 below the requests, written down weeks earlier by somebody not looking for it. The pattern half is closed by M-S16 | CODE | `COMPLETED.md` §167 |
| S-18 | ⚠ **CORRECTED 2026-08-31 at C-N3, and the correction is the more interesting fact.** ~~The player's interest-rate lever is DEAD in every country~~ — **it is LIVE in Germany, France and Italy.** `ApplyInterestRateChanges` routes a country with `CurrentFedChair != null` to the bank's own rule and never reads `PolicyDecision.InterestRateChange`; that is USA, Sweden and Poland. **The eurozone trio have no chair**, so they fall through to `EurozoneRateSystem.ApplyEurozoneRate`, which reads the decision and gives each member a bounded push on the shared rate. The original row generalised from ONE country (C-C9's assertion set it on the USA) to all six — the exact error C-N3's per-country retry exists to prevent, and it was C-N3's first run that caught it. **What remains true and still needs doing:** the lever is dead for the three chair countries while `PolicyDecision.InterestRateChange`'s long doc comment still describes a live player lever everywhere, and `GameController`'s independent-currency rate slider branch — with its *"this game deliberately hands you the central bank"* paragraph — is now **unreachable** (it needs a country with no chair and an independent currency; there is none). Retire the branch and re-word the field's doc | CODE | `COMPLETED.md` §§105, 111 |
| S-21 | ✅ **CLOSED 2026-08-31 (`COMPLETED.md` §125) - and the claim that opened it is RETRACTED.** §124 said two frames beyond the named three were unstable; ⚠ **three controlled back-to-back runs on one unchanged tree show both byte-identical every time**, and exactly the three already-named frames vary. The earlier variation was **code-driven** (the `StatNodeId` enum gained two members mid-window) and was misread because **the comparison spanned a change** - the same mistake, in the opposite direction, as C-D5's first film and C-C11's decayed Okun reading. ⚠ **The real defect was that the rule-15 diff had NO TOOL**: re-typed as a shell loop each pass end with no named exclusion list, so noise and change looked identical. **`FilmDiffCheck` built** - matches frame to frame by SHA-256, excludes three frames BY NAME WITH REASONS, and **fails on a roster mismatch** (a frame in one set and not the other is a capture that silently did not happen). Proven three ways: clean pair 78/0, real change 78 differ, roster mismatch exit 1 | CODE | `COMPLETED.md` §125 |
| S-20 | ⚠ **A CAPTURE THAT WRITES IS NOT A CAPTURE OF WHAT YOU MEANT — and nothing in the film bar checks the difference.** Found at C-D5: **every `-shotelectionnight` film ever taken photographed the DESK**, board 1h's own W-E6 films included, because the board is a `ScreenSpaceOverlay` Canvas and `GameController.OnGUI` draws after overlay canvases in the built-in pipeline. Through all of it: **8 captured, 0 failed, 0 text overflows, 0 containment escapes, exit 0.** The guards check containment and text fitting **within whatever was drawn**; **nothing checks that the thing under test is the thing on screen.** ⚠ This is a class, not an instance — every Canvas board filmed beside a live IMGUI controller has the same exposure, and a screen that silently stops rendering would film clean forever. Sized as its own item: a cheap identity assertion per staged capture (a token the board draws and the capture must contain, or a pixel-difference against the same frame with the board suppressed). Belongs with S-17 and C-CAP's two traps | CODE | `COMPLETED.md` §116 |
| S-19 | ✅ **ANSWERED at C-N4, 2026-08-31 (`COMPLETED.md` §123): the loss point is CONSUMPTION** - the C term has no disposable-income input, so a tax rise never reaches output while spending enters the identity directly as G. ~~⚠ **INCOME TAX AND WELFARE GENEROSITY MOVE GDP, INFLATION AND UNEMPLOYMENT BY EXACTLY ZERO.** Not "by a little" — **0.0000** on all three over 12 turns, while the same dials move the Budget by 3 094 and 1 225 and approval by 6.6 and 2.1 points. **The revenue side of fiscal policy has no output channel at all**; only spending and the crime dial reach real output. Measured by leave-one-out at C-C10, unlooked-for. ⚠ **This is C-C11's headline before C-C11 starts**: a model whose tax multiplier is identically zero sits outside every sourced estimate in the literature by construction, and C-C11's pre-ruling says exactly what to do with that — say so plainly, propose with the basis attached, **move no constant** | CODE → C-C11 | `COMPLETED.md` §106 |

| **S-22** | ⚠ **`DrawTimeRangeRow`'s doc asserts a behaviour that does not exist — and the coherence audit could not see it.** Its summary said *"bounded ranges filter on real elapsed time, so a monthly stat and a quarterly one both show the same calendar span"*; **nothing filtered**, `_timeRange` was set and read only inside a selector that was never drawn. Deleted at the (b) ratchet 2026-09-01. **The finding is not the dead code, it is the blind spot**: `CommentClaimCheck` verifies backticked `Type.Member` references and cannot read a PROSE claim about behaviour, which is the larger half of what comments assert | CODE ⚠ a gap in the audit itself | `COMPLETED.md` §138 |
| **S-23** | ✅ **CLOSED 2026-09-01 (`COMPLETED.md` §168) — the claim is TRUE now, and it was false for months through one correction.** ⚠ It did not need *"more than a regex"*; it needed a regex **and a classifier**. Per-occurrence read/write separation, reported as `DeadStateCheck.WRITE_ONLY`. ⚠ **Its first run found SIX — five being the `_cached*Raw` family this row's own worked example named** (their readers were the accessors the check DID catch, so deleting those left the fields looking alive) plus `_primaryButtonStyle`. **All deleted; the ceiling untouched.** A seventh, `_attachAttempts`, was a **false positive** — `if (++x > 600)` consumes the value — and ⚠ a false positive here would have had somebody delete a live loop bound, so the rule now asks what follows the operator. The declaration counts as neither, which the first version got wrong and a **planted probe** caught | CODE | `COMPLETED.md` §168 |
| **S-24** | ⚠ **`TaylorRule.InflationTarget = 2` is not Poland's target.** The NBP targets **2.5 % with a symmetric ±1 pp band** (verified 2026-09-01 at the NBP's own guidelines), so 2 sits inside its tolerance and is not its target. One target for six countries, and the one country it misstates is now named at the constant. ⚠ Related: **`NeutralRealRate = 2` is Taylor's 1993 assumption for a quantity that is unobservable and actively contested** — published r* estimates for these economies have run from below zero to above two within a decade | CODE | `COMPLETED.md` §138 |
| **S-25** | ⚠ **`BaseConsumptionRate` and `BaseInvestmentRate` are in the real range and UNIFORM ACROSS SIX COUNTRIES.** Household consumption really is around 50–70 % of GDP and investment near a fifth — but the six differ from each other and one value does not distinguish them. The same shape as D-9's rejected per-country tax base shares, on the other side of the identity | CODE | `COMPLETED.md` §138 |
| **S-26** | ✅ **CLOSED 2026-09-01, and it was FIVE, not four.** The dial midpoint is stated ONCE — `CrimeJusticeCouplings.NeutralDialLevel`. `MacroSystem.NeutralPolicyDialLevel` is now a local ALIAS (the name stays where its readers expect it, the value does not); the `PolicyWebRenderer` and `SimulationManager` locals reference it. ⚠ **Unifying them was NOT the fix** — each of the four already carried a comment saying the others existed, and one said outright that unifying was *"a refactor this pass deliberately does not do"*: **the fact was known, written down four times, and grew anyway.** So the closure is `SharedMidpointCheck`, and ⚠ **its first run found a FIFTH the sweep had missed** plus two false positives, both answered with an argument rather than by narrowing the test: `Sector.BaselineRegulationLevel` (a serialised field default whose own ruling says it must NOT be the uniform 50) and `MacroSystem.NeutralApprovalRating` (approval's midpoint, a different quantity). The exception list is printed in full and **policed** — an entry matching nothing, or naming the owner, fails. Proved all directions | CODE | `COMPLETED.md` §163 |

| **S-27** | ⚠ **`TacticalVoting` is built, harness-proven, and has NO caller in the model.** It appears in exactly one file outside its own — `TacticalVotingHarness` — and `ElectionDay` never mentions a poll, so the chain **media → coverage → momentum → poll → ballot** terminates one step before the mechanism written to receive it. Found 2026-09-01 while answering C-N1; ruled at **D-10 (a)**: wire it, as its own baseline item. ⚠ **The coherence audit could not see this either** — `DeadStateCheck` scans PRIVATE declarations, and `TacticalVoting.Apply` is public. **Public API with no production caller is a third blind spot**, alongside S-22's prose claims and S-23's write-only fields | CODE | `COMPLETED.md` §139 |

| **S-28** | ⚠ **P-I2 stage 3's anchor is SOURCEABLE for all six — probed 2026-09-01.** §141 left the retirement blocked on *"a convergence speed nothing sources"*; two requests corrected that. **Eurostat `proj_23np`** (`projection=BSL`) answers for all five EU countries — Sweden 2050 reads **12 130 240** against 10 551 707 in 2024 — and the **US Census 2023 National Population Projections** file `np2023_d1_mid.csv` (2.87 MB) gives the projected pyramid **by single year of age**, which is better than the EU series for this purpose. **The remaining choice is a SHAPE, not a figure**: converge the survival array, the fertility rate, or scale the whole pyramid toward the projection. ⚠ **Nothing is built on it** — both were probed for reachability only, and a dataset confirmed reachable is not a dataset used | CODE | `COMPLETED.md` §142 |
| **S-29** | ✅ **CLOSED 2026-09-01 (`COMPLETED.md` §166) by `PartyInkDrawSiteCheck`.** ⚠ **The surface turned out to be ONE file**: exactly one runtime file draws `PoliSimTheme.Party(` — `HemicycleRenderer`, at three sites — so the ruling's *"where"* constraint is fully enumerable rather than approximate. Clause 1 is an **allow-list of files with the argument for each** (a deny-list is silent about the file nobody thought of, and the finding is precisely that ink turns up unconsidered). Clause 2 takes the containing FILE as the unit of adjacency and **says so**: *adjacent* is not decidable from source, and a file that never draws both cannot draw them adjacent — coarser than the ruling and strictly stronger. ⚠ **Clause 3 needs no clause**: chrome lives in `GameController`, off the list, so a party ink in a status dot fails clause 1 by construction — proved by putting one there. Measured today the rail dot draws `PoliSimTheme.Good`. Four failure paths proved: off-site, mixed, a permission naming nothing, and the accessor renamed | CODE | `COMPLETED.md` §166 |
| **S-30** | ⚠ **A one-way ask has no receipt unless somebody reads the far side.** P-F2 concluded from the absence of a readback that the D7 paste was never made — correct for D7, and quietly generalised to D9, which **had** been pasted and answered in full three days earlier. The Design project's own `uploads/` listing carries each pasted artifact **with its digest in the filename**, so a receipt was always one `list_files` away. **Standing habit, not a task:** before recording an ask as unsent, list the far side | CODE (a habit) | `COMPLETED.md` §146 |
| **S-31** | ⚠ **R-N2 WAS RETIRED AT W-G1 AND THE BACKLOG ITS LICENCE CREATED WAS NEVER RE-HOMED.** R-N2 authorised building the elections model as pure functions wired to nothing; `a289e1e` retired it. **Five files still say *"PURE FUNCTIONS, WIRED TO NOTHING (R-N2)"* in their own headers and cite a rule that no longer exists.** `UnwiredSubsystemCheck`'s new UNREACHABLE class counts them (ceiling 5) so the backlog cannot drift, and each has a disposition at `COMPLETED.md` §148 — but the standing point is the pattern: **when a licence is withdrawn, the things it licensed are a list, and that list was never made.** ⚠ Check for others whenever a standing rule is retired | CODE (a habit) | `COMPLETED.md` §148 |
| **S-32** | ⚠ **ELECTION NIGHT HAS NO DOOR.** `ElectionNightScreen` — board 1h, built, filmed at four widths, recorded as delivered — is named by **nothing except `UiScreenshotDriver`**, appears in no scene and no prefab, and **the running game cannot open it.** ⚠ **This is S-20's class in a new form**: S-20 found a capture can photograph the wrong thing; this finds a capture can photograph a thing *the game has no route to at all*, with every guard green throughout — containment and text-fitting check what was DRAWN, never whether a player could get there. Sized as its own SAFE row (a screen opening, no trajectory) | CODE | `COMPLETED.md` §148 |
| **S-33** | ⚠ **THE THREE UNWIRED ELECTIONS SYSTEMS ARE ONE MISSING INPUT, AND TWO OF THEM ARE EACH OTHER'S ANSWER.** Measured 2026-09-01. `ElectionNightScreen` needs a **per-constituency count**; `NationalElection.Run` takes national shares and allocates a chamber, so the live election produces no regional count at all. **`RegionalVoteModel` is the thing that would produce it** — and is itself unreachable. `TacticalVoting` needs **polled shares**, and the campaign layer that produces polls is harness-only (C-R4b). ⚠ **Underneath all three: the sourced elections data lives in `ElectionsData/` at the repo ROOT, outside `Assets/`** — editor-side code only, and a built player would not have it. The project's own pattern is to TRANSCRIBE sourced data into C# (`PartySystem`, `DeclaredRedLines`), so each of these needs its seed data transcribed and reconciled first. **The order, and it is a programme rather than three loose ends:** regional seed data → `RegionalVoteModel` → a regional count at the live election → board 1h has something true to draw | CODE | `COMPLETED.md` §154 |
| **S-34** | ⚠ **`PlayerReachabilityCheck` armed at a ratchet of 1, and the ratchet is a DEBT that must not be paid cheaply.** The one gap is `ElectionNightScreen`. **Naming the type in `GameController` without a path would satisfy the scan and fail the rule** — and giving board 1h a one-constituency "night", or the sourced 2022 count under an election simulated with different shares, would make the check green and the screen a lie. That is the S-20 class this sweep descends from. **Lower the ceiling to 0 only when the screen has a real route with a real count** | CODE | `COMPLETED.md` §154 |
| **S-35** | ⚠ **A TYPE IS NOT ITS FILE, AND A PROBE THAT ASSUMES SO WILL "CORRECT" A CORRECT REFERENCE.** The §9.4 pass reported `CampaignCalendar.DefaultPreCampaignWeeks` as naming a missing type and rewrote the entry; **the entry was right** — `CampaignCalendar` is a `public readonly struct` living in the FILE `CampaignClock.cs`. The probe matched TYPE names against FILE names, so any type sharing a file with another read as absent. ⚠ **The sweep built the same session caught it within the hour** (`DocumentClaimCheck`, §155), which is why the check indexes DECLARATIONS rather than filenames. **Standing habit:** when a scan says a name is missing, confirm against the declaration before editing the document — a wrong correction is worse than the drift it was aimed at, because it carries the authority of having been checked | CODE (a habit) | `COMPLETED.md` §155 |

| **S-36** | ⚠ **DOCUMENTATION OF AN ABSENCE FUNCTIONS AS EVIDENCE OF A PRESENCE — the sharpest form of this project's signature defect, and it happened TWICE.** The generic spending seeder in `WorldFactory` (deleted 2026-09-01, and deliberately NOT named here — see the habit below) carried the words *"has no caller left"* in prose, **and that prose is what kept `DeadStateCheck` from seeing the corpse**; `POLISIM_V2_SCREEN_SPEC.md`'s fold-override field was validated by the `SaveGame` comment **recording its removal**. In both cases the only occurrence of the name in the codebase was the note saying the thing was gone, and in both cases the note read to a name scanner as a use. ⚠ **Neither was visible until the stripper existed** (§§160–161), which is the second half of the lesson: the class was undetectable by construction, not overlooked. **Standing habit: when a comment says something is dead, that sentence is the LAST place its name should appear — delete the thing in the same commit, or the note becomes its alibi.** ⚠ **This row proved it on itself**: written naming the deleted method, it made `DocumentClaimCheck` go from 2 to 3 within the minute — the sentence recording a deletion committing the very class it describes. Re-worded to name no dead member. | CODE (a habit) | `COMPLETED.md` §§160, 161 |
| **S-37** | ⚠ **A BOUND WHOSE DIRECTION IS NOT CARRIED CAN SILENTLY INVERT.** `PublicationCadenceCheck`'s reachable-preliminary bound is a **floor** — the capture driver's warm-up breaks when the count goes DOWN — and it was nearly reported to `RatchetLedger` as a ceiling, where "tight" would have meant the opposite of what it says and slack would have been measured in the wrong direction. The ledger carries `IsFloor` now. **Standing point: every bound in this repo is a claim about a direction as well as a number, and a ledger that stores only the number stores half the claim.** | CODE | `COMPLETED.md` §161 |
---

## 11. Riders — work that rides the next item touching its file

Not items. Each is a small, ruled change with no commit of its own: it lands inside whatever item next
edits that file, and is recorded here so it cannot be forgotten and so nobody mistakes the current state
for the intended one.

| ID | what | rides | ruled |
|---|---|---|---|
| **R-D8** | ⚠ **Apply D-13's test to the TAX side.** D-13 found the enforced spending denominator is not the quantity Ramey's band covers. **Romer & Romer's −2 to −3 is per EXOGENOUS tax change of 1 % of GDP; the harness divides by the REALISED change in the budget balance**, which nets off the endogenous response — the same defect, unexamined, on the channel the model already undershoots threefold. Cheap: the same runs hold both quantities, and the statutory change is `baseGDP × Δrate × BaseShareOfGdp`. ⚠ **A measurement, not a re-decision of D-8, and it must not move a constant** | ✅ **DISCHARGED the same day, inside the §D re-read's own commit** — the file was already open | 2026-09-01, opened and closed by the §D re-read (R-N1) |
| **RIDE-1** | ✅ **DISCHARGED at C-C9, 2026-08-31 (`COMPLETED.md` §105) — rode the wiring commit, no separate commit, as ruled.** ⚠ **the published-series renderer, named in full in `COMPLETED.md` §105,** and its eight exclusive helpers deleted — ⚠ **the name is deliberately NOT spelled here (S-36)**: this is a LIVE document, and a deleted member whose only surviving occurrence is the note recording its death is exactly how this project lost five things to a comment. The record carries the name; (`GraphRenderer.cs` 1188 → 837 lines); `ReleaseMarkerColor` kept because C-C4's enactment markers are live on it; the published-series *model* untouched. ~~**Delete the published-series renderer** (named in `COMPLETED.md` §105).** ⚠ **It has NO CALLERS** — P-A2's cut of the "as published" graph block (2026-08-29) removed the last one and left the method behind, so **the published-graph path is NOT live and must not be treated as such.** Anything reasoning about how the game draws a published series should read `Draw` and `DrawPublished`'s own overlay helpers instead. Found at C-C4 | the next item that touches `GraphRenderer.cs` | 2026-08-31 (Elias): rides the next item, **no separate commit** |
| S-14 | ✅ **RULED 2026-08-31: campaign money is a SEPARATE PURSE in national units, and the two never transact unless Elias rules otherwise. DO NOT BUILD THE JOIN.** ⚠ **The game already holds TWO currencies with no conversion.** The campaign layer prices in **kronor** (war chest 2 400 000 kr, a television buy 500 000, a social post 5 000); the macro layer is in **USD billions**. They never meet today because a campaign is staged rather than funded from the state's budget — **the day a campaign is paid for out of anything the macro model holds, one of the two is wrong by a factor of ~10 500 000 000**. Invisible until it is expensive | CODE | `COMPLETED.md` §98 |
| S-15 | ⚠ **The central-bank candidate pool's prose is USA-specific and is now shown to every country.** `FederalReserveSystem.CandidatePool`'s authored descriptions name the institution — *"believes **the Fed** waited too long to act last cycle"* — so since C-C7 a Swedish player is offered Riksbank governors who talk about the Fed. Fixing it means authoring per-country fictional descriptions for the whole pool: a CONTENT item, not a display fix, and not to be improvised. The three structural strings (banner, lean row, appointment sentence) were fixed at C-C7 | CODE (content) | `COMPLETED.md` §100 |
| S-16 | ✅ **DONE at C-E3, 2026-08-31** (`COMPLETED.md` §120) - the backtick is `MetaTextCheck`'s twentieth pattern. ~~**`MetaTextCheck` could catch a backtick in a player-facing string.** C-C8's first cut shipped *"`Country` carries no bilateral relations field"* to a player surface — a leaked identifier that rendered its backticks literally — and the check passed, because backticks are not in its 19 banned patterns. A backtick in a UI string is a reliable tell for exactly the class P-A1 cut 131 strings of. Widening the enumeration is a change to a standing guard and belongs in its own item, not inside the one that found it | CODE | `COMPLETED.md` §102 |
