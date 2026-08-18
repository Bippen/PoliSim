# Step 2 — Causality Legibility: the scoping package (2026-08-18)

**Design work only; nothing is built.** The package answers the four scoping questions with
options and costs and ends in the ruling items. Per the sandbox ruling: the job is making the
causal web READABLE — the actual terms, surfaced — not a tutorial and not a second model.

## 0. The inheritance list, verified at HEAD

- **Approval's writer census (R-Q1c's motivating case), enumerated**: FIVE writer classes
  across FOURTEEN sites — (1) `ApplyApprovalRating`, the boundary delta formula, whose NINE
  effect terms plus reversion **already exist as named locals** (reversion, growth, misery —
  itself four sub-gaps: U/inflation/crime/corruption — tax-hike, weighted-spending ×
  deficit-awareness, welfare, paid-leave, drug-policy, Gini gap); (2) eight
  `BillFailedApprovalCost` sites + the tax-hike penalty in ParliamentSystem — **which land on
  ANY day, not at boundaries** (the system's own comment says so); (3) Cabinet option effects;
  (4) EventSystem shocks; (5) ForeignPolicy options and the reshuffle cost. The ±20-point
  drug-policy equilibrium shift is `sens × (dial − 50)` through the 0.05 reversion — the
  composition R-Q1c wants visible.
- **Q2's founding**: `EffectiveConsumerConfidence` = base × wage-sentiment factor, and the
  factor is a PERIOD-OPEN ANCHOR (the fifth fixed reference). Single-book rider inherited as a
  constraint: the explanation presents EFFECTIVE values; the base may be named as a component,
  never shown as a second truth.
- **The coupling graph as built**: `PolicyWebRenderer` holds 55 policy nodes, 18 stat nodes,
  **50 policy→stat edges — and ZERO stat→stat edges.** The web the feature must explain
  (misery→approval, confidence→consumption→GDP→Okun, the productivity pipe, the interest
  chain, FRF/erosion/maturity) exists only in formulas, nowhere as data.
- **The decomposition machinery**: the erosion-standard decompositions live OFFLINE (editor
  diff tools + analysis scripts over dumps) — but the TERMS they decompose are in-code named
  locals, and `FiscalTurnReport` already demonstrates the needed principle in production, in
  its own doc comment: *"recorded rather than recomputed… any caller adding the components up
  would produce a plausible number that is not the one the simulation used."* The fiscal
  chain's attribution surface half-exists (report + three display sites).
- **The preview hand-list question**: carried to §5, answered yes — with a mechanism.

## 1. The central derivation — what can be attributed HONESTLY (this drives everything)

Four attribution classes exist at HEAD, and they have DIFFERENT honest grains:

| class | quantities | honest grain | exact? |
|---|---|---|---|
| **A — boundary delta formulas** | ApprovalRating's formula (politics lives at boundaries) | per period, term-level | EXACT — the terms are the computation |
| **B — event-grain shocks** | bill-failed costs, cabinet/foreign/event shocks, reshuffle | per EVENT, with its date | EXACT — discrete, face-valued |
| **C — period stances** | planned G, FRF multiplier, the wage-sentiment factor, the anchors | per period | EXACT BY CONSTRUCTION — a stance is one number per period (the fifth fixed reference makes Q2's factor attributable for free) |
| **D — daily compounding feedback** | GDP, C, I, U, π, debt path | per period: drivers' period values + **a named residual** | NO exact additive split exists — the feedback loop is real |

**The honesty rules that fall out, stated as design law:**
1. **A surface showing daily numbers for boundary-resident terms lies** (there is no daily
   Gini-approval contribution). Class A/C render per period; Class B renders per event with
   its date — an event smeared into a period average also lies.
2. **Any Class-D display carries a residual line** ("feedback/interaction: +0.1") — the
   erosion standard's remainder practice made player-facing. A Class-D breakdown without a
   remainder is the StatTile bug as pedagogy.
3. **Delta-model terms get equilibrium framing**: a sustained per-period term of +0.5 through
   the 0.05 reversion is a +10 EQUILIBRIUM shift — the honest unit Q1's magnitude was ruled
   in, shown beside the per-period number ("drug policy: +0.5/turn → sustains ≈ +10").
4. **Single book**: confidence explains as effective = base (the policy accumulation,
   labeled) × wage-sentiment factor (the gap, signed). One truth, two named components.

## 2. SURFACE — the four candidates, costed

- **(d) Boundary-report annotations — the MINIMUM-VIABLE SLICE, recommended phase 1.**
  Term lines on the period report: *"Approval 46.2 → 44.4: reversion +0.2 · growth +0.3 ·
  misery −0.8 (U −0.5, π −0.2, crime −0.1) · Gini gap −0.3 · parliament −1.2 (2 bills,
  d113/d201) · shocks −0.3."* Paper/ledger idiom exactly; period-true by construction; pure
  IMGUI text in existing screens (the `GetLastFiscalReport` display pattern, already proven at
  three sites). Smallest build; term-level floor met on day one.
- **(a) "Why did X move" trace panel — recommended phase 2, the standing surface.** Click a
  stat chip → panel lists last period's writers, signed, with equilibrium framing and event
  dates; Class-D stats get drivers + residual. Reuses the ledger (d) already built; IMGUI
  panel under stable-control-layout (fixed maximum row count, `GUI.enabled` for absent terms —
  the DrawTaxPolicy pattern). Medium build, mostly UI.
- **(b) Tooltip chains — rejected as a primary surface.** IMGUI `GUI.tooltip` is a single
  string per frame: signed term lists don't fit legibly, chains (following causality upstream)
  don't fit at all, and hover-only discoverability hides the feature. Acceptable LATER as
  one-line pointers into the panel ("↘ 3 terms — click").
- **(c) Standing causal-graph screen — deferred, with a stated path back.** The web renderer
  shows STRUCTURE, not this period's numbers; stat→stat edges don't exist as data and hand-
  authoring them creates a second truth that drifts. The honest path: once the ledger exists,
  its term IDs ARE the stat→stat edge list (source stat → target stat per term) — the graph
  becomes DERIVED, not authored. Largest build, so it waits for that free data.

No Canvas dependency anywhere; everything lands in the v2.0 IMGUI chrome.

## 3. DEPTH — term-level floor, narrative as a generated layer only

**Term-level is the recommendation, alone, for v1** — the sandbox ruling's floor, and §1's
rules make it honest. A narrative layer ("rising inequality is hurting approval"), if ever
wanted, must be GENERATED from the ledger (largest |terms| → sentences) — never authored
beside it, which would be the single-book violation applied to prose. Deferred entirely; it
adds warmth, not information, and Step 3's scenarios need information.

## 4. COST — per piece

- **New state: the attribution ledger.** Per country, per period: per-stat term records
  (term id, signed float) + the period's event list (event id, date, value). Approval alone:
  ~11 records/period. Class C reuses `FiscalPeriod`'s anchors; the fiscal chain reuses
  `FiscalTurnReport` outright. **Save shape: persist ONE period's ledger** (small, bounded) —
  the alternative (empty explanations after every load) is exactly the class of silent
  post-load gap that item 8's history warns about. History beyond one period: NOT persisted
  in v1; `StatHistory` already gives series context.
- **Perf**: Class A/B/C recording is appending existing locals at sites that already compute
  them — negligible. **No per-day recording exists in v1 at all** (Class D's drivers and
  residual are computed once at the boundary from the anchors).
- **Reuse inventory**: `FiscalTurnReport` (recorded-not-recomputed precedent + surface),
  `FiscalPeriod` anchors (stances), named locals in `ApplyApprovalRating`, `StatHistory`,
  the `GetLastFiscalReport` display idiom. Needs building: the ledger type + recording lines,
  the annotation rendering, the two checks (§5), later the panel.
- **Build sizes**: (d) small · self-audit assert small · preview-parity diagnostic small ·
  (a) medium · (b)-as-pointers trivial, later · (c) large, deferred.

## 5. The two checks the ledger buys (the inheritance questions, answered)

- **The self-audit assert**: at every boundary, Σ(recorded terms) must equal the stat's
  actual Δ (clamp events recorded as their own term when they bind). The explanation layer
  SHARES the model's arithmetic and proves it every period — an explanation that cannot
  drift from the simulation, which was the derivation's requirement. Editor diagnostic +
  cheap always-on guard (one float compare per stat per boundary).
- **The preview-parity diagnostic — the hand-list class killer, YES.** Run `AdvanceTurn` and
  `PreviewTurn` from identical state; compare ledgers TERM BY TERM. A clone-escape (the
  BaselineGini class, three appearances) surfaces as a mismatched term THAT NAMES ITSELF —
  stronger than asserting field copies, because it tests the consequence, and it self-extends:
  every future term is covered the day it is added, with no list to maintain. (The assert the
  hand-list never had.)

## 6. SCOPE OF TRUTH — explain the LIVE model (recommended)

The precedent already splits surfaces by question: policy screens deliberately read LIVE
("what the player's own levers are doing right now" — `ReadLiveValue`'s doc), the Statistics
tab reads PUBLISHED. Explanation is a levers question: causality lives in the model, and the
publication layer ADDS no causal content — it lags and revises what happened (it can admit,
not explain; it deliberately excludes ConsumerConfidence entirely, so the published side
cannot even host Q2's chain). **The claim each choice makes:** LIVE — *"you are the
government: the statistical office stands between you and the PUBLIC, not between you and
your own instruments."* PUBLISHED — an information-asymmetry game where the operator is
blind too: a different, harder game that would demand published-first UI everywhere,
contradicting the recorded display stance. BOTH — double UI for a question no current
mechanic asks. Recommendation: LIVE, with the asymmetry hook noted for the queue (approval
reacting to PUBLISHED figures would be a real coupling, ruled at its own trigger, not here).

## 7. RULINGS NEEDED

- **R-S2a — SURFACE**: phase 1 = boundary-report annotations (d); phase 2 = the trace panel
  (a); tooltips only as pointers later; graph screen deferred until it can be derived from
  the ledger. (Recommended as stated.)
- **R-S2b — DEPTH**: term-level only in v1, under §1's honesty rules (event dates, residual
  lines, equilibrium framing); narrative deferred and generated-only if ever. (Recommended.)
- **R-S2c — TRUTH**: the explanation layer explains the LIVE model. (Recommended; the claim
  stated in §6.)
- **R-S2d — THE MINIMUM-VIABLE SLICE**: approval end-to-end first — ledger for the nine
  formula terms + event shocks, report annotations, the self-audit assert, the
  preview-parity diagnostic; then effective confidence (single-book presentation); the
  fiscal chain last (half-exists). **Blocks Step 3's scenario authoring; nothing else.**
- **R-S2e — persistence (yes/no)**: one period's ledger joins the save shape. (Recommended
  YES — small, bounded, and the no-case produces silent post-load blanks.)

**Stop at the package.** Nothing here is built; the build pass starts from the rulings.
