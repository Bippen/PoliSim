# Round 4 batch R4-4 — PRE-REPORT: the three cabinet portfolios (2026-08-17)

**Defense, Foreign Affairs, Education. A CONTENT batch, not a stat batch. Nothing has been
built — this document is the ruling 6 deliverable, and R4-4 builds only after the rulings at the
end land.** Everything below is derived from the code at HEAD (`9f12c96`) and Part A's shipped
pattern; where a statement is a design call rather than a derivation, it is named as one and
appears again in §5.

One step-0-class correction up front, because the R4-3 verdict left a question open that the
code answers: the verdict said R4-4's step 0 must determine "whether the new portfolios'
decisions consume the shared RNG stream." **There is no shared stream.** Cabinet draws come from
`SimulationRandom.Stream.Cabinet` — its own independently-seeded sequence, one of seven, built
"specifically so [one consumer] cannot perturb any simulation consumer's draws" per the class's
own doc. And the trajectory dump never draws from it anyway — see §1. The "judged, not asserted
away" fallback the verdict braced for never fires.

---

## 1. The bar-inheritance statement, applied item by item

- **Aggregation equivalence — N/A as an extension, runs unchanged as regression.** No new daily
  model, no new turn form. The check must still finish 104/104; any change is a defect, not a
  budget.
- **Bucket asserts — N/A.** No new `StatHistory` series.
- **Trajectory matrix — APPLIES, and byte-identical REMAINS the criterion.** This is stronger
  than the "bounded-and-explained movement" the batch brief anticipated, and it is derived, not
  hoped: (a) `TrajectoryBaselineDump` drives all-None decisions with no player country —
  `Country.CabinetMinisters` is empty by default for every country, and the only writers in the
  repo are GameController and the test harness's appointment scenarios, so the dump's worlds
  hold zero appointments; (b) `CabinetSystem.TryRollDecisions` iterates the appointment
  dictionary, so with zero appointments it consumes **zero Cabinet-stream draws**; (c) the
  Cabinet stream is isolated by construction even where draws do occur; (d) candidate/decision
  pools are static data — defining them draws nothing. So the concrete acceptance form proposed
  for ruling R6: **6/6 configurations vs a fresh `pre_r4_4` baseline, 35 of 35 shared
  EconomyState fields (+2 extras) byte-identical, ZERO fields named NEW — any movement is a
  FAIL requiring diagnosis, not an explanation.** The bounded-and-explained standard is real but
  lives elsewhere: in the anomaly matrix's `cabinetstress` scenario, which appoints by enum
  iteration and so absorbs the three new portfolios automatically — its concrete form is also
  under R6 below.
- **ctor/Clone — dead class.** `EconomyState.Clone()` is `MemberwiseClone` since R4-3, and a
  content batch adds no EconomyState fields regardless.
- **Save/load — APPLIES, and the coverage is confirmed rather than assumed.** Both layers
  already exist and already round-trip: appointments ride Layer 1 (`World` serialized close to
  as-is — `Country.CabinetMinisters` is a public field), pending decisions ride
  `SaveGame.PendingCabinetDecisions` via `PendingCabinetDecisionRecord` (by value, the named-pair
  capture layer). `SaveLoadRoundTripDiagnostic` already appoints a FinanceTreasury minister,
  lets decisions roll and sit PENDING across the save, and resolves them post-load. Enum growth
  is append-only: a pre-R4-4 save contains no new-portfolio keys and loads unchanged. **The
  R4-4 form of this bar item: switch (or add) the diagnostic's appointment to one NEW portfolio,
  so a new-portfolio appointment and a new-portfolio pending decision actually cross a save in
  the bar run — then 12/12 as always.**
- **Publication cadence — N/A.** No published stat; the `PublishedStat` enum does not change,
  and the check iterates it.
- **Captures — APPLY, and are the batch's real surface.** The Politics/Cabinet screen grows
  from three portfolio panels to six; `UiScreenshotDriver` iterates the portfolio enum with a
  guard whose own comment anticipates exactly this expansion ("only 3 of the 6 portfolios have
  authored candidate pools"), so the search-state and appointed-roster captures absorb the new
  portfolios with zero driver edits. Both sizes, 0 failed, 0 overflows, as standing.
  **D1 status, stated:** current portrait coverage is complete (9 ministers + 7 Fed chairs, all
  name-matched). `IconLibrary.GetCabinetPortrait` returns null for an unknown name and the UI
  degrades to the existing procedural placeholder — the documented safe failure ("never draw
  someone else's face"). So before D1 art exists, **the nine new ministers render with the
  procedural placeholder**, and captures proceed on it. The D1 request document can be written
  the moment the name list is signed — filenames derive from the names
  (`portrait_cabinet_<portfolio>_<slug>`), which is precisely why D1 has been blocked on this
  batch. The nine derived slugs are listed in §4.
- **Pre-report `[GAP]`/blocker figures:** none. A content batch seeds no data. The count for
  R4-4 is zero.

## 2. What the three portfolios actually do

The Part A pattern, restated as the template each portfolio below instantiates: a minister is
competence (a small, always-beneficial passive bias landing on an existing audited channel at
point-of-use) plus philosophy (Reformist/Pragmatic/Traditionalist, selecting which decision pool
their scenarios draw from) plus decision content (2 scenarios per (portfolio, philosophy), fired
at 12%/appointed-minister/turn on the Cabinet stream, player-resolved, landing as one-time
bounded shocks that fade via the target stat's own existing mean reversion — the EventSystem
idiom). `BudgetImpact` lands on the cumulative `EconomyState.Budget` in $B; decisions are
EXECUTIVE actions and do not route through Part B's legislation gates — bills are the player's
channel, and no authored scenario may promise a legislative act as its outcome (that would build
a competing lawmaking path, which the roadmap explicitly forbids). "Content batch" therefore
means: simulation contact exists, is enumerated here, and is entirely decision-outcome-shaped —
the roadmap's own "decision budget-impacts at event scale only" column, plus the shock fields
listed per portfolio below.

### Defense

- **Decision space:** procurement (a program over budget vs. a capability gap), readiness (an
  exercise or deployment posture request), basing/restructuring (close-or-consolidate with local
  fallout), personnel/veterans issues, and the odd procurement scandal. Costs and public
  reaction, not war — there is no military/conflict system to touch, and this batch does not
  invent one.
- **Reads:** nothing beyond its own decision presentation.
- **Writes (decision outcomes):** `BudgetImpact` and `ApprovalEffect` only — the existing
  option fields suffice; Defense needs no new shock field.
- **Spending mapping, stated per the name-class discipline:** the portfolio does NOT touch
  `SpendingCategory.Defense` (USA's $850B line, or the generic five's Defense line). Spending
  lines are the player's lever through the budget process; decisions land as one-time Budget
  deltas at event scale. Same rule for Education vs `SpendingCategory.Education` below.
- **Passive competence channel: NONE EXISTS to land on.** Part A chose its three portfolios
  because each had a well-understood audited channel; Defense has no state (no readiness index,
  no military stat), and the one adjacent quantity — the G-term via defense spending — is
  fiscal-engine contact, which the F1 sequencing keeps closed until the stock-vs-flow mechanism
  report. **Recommendation: decisions-only this pass, recorded openly as the Defense limitation
  (the same "not forgotten, but not done" honesty Part A's 3-of-6 used).** Alternative under R3:
  competence scales its own decisions' magnitudes — contained, but a departure from Part A's
  always-on semantics.

### Foreign Affairs

- **The adjacency that must be ruled, not discovered later:** `ForeignPolicySystem` already
  exists — Phase 0 scaffolding, deliberately "one small proof-of-pattern interrupt slice": a
  single flat pool of 4 meetings (no portfolio/philosophy branching), rolled per DAY (0.01) on
  its own stream, single pending slot, player-resolved options carrying
  `tradeBalanceShock`/`budgetImpact`/`approvalEffect`. A Foreign Affairs MINISTER sits directly
  on top of this. Options for R4:
  - **(a) COEXIST — recommended.** The FA portfolio gets the standard (portfolio, philosophy)
    decision pools like every other portfolio; meetings remain the separate "inbound interrupt"
    channel (the world calls you), cabinet decisions the "minister's own agenda" channel (your
    minister brings proposals). Authored content is differentiated from the 4 existing meetings
    so no scenario reads as a re-skin. An adjacency note lands in both files.
  - **(b) ABSORB — not recommended.** Philosophy-differentiated meeting pools and
    competence-scaled meeting outcomes would upgrade the Phase 0 slice into the full system —
    real design work smuggled into a content batch, exactly the double-touching the Master
    Sequence exists to prevent. If wanted, it is its own later item.
- **Writes (decision outcomes):** `TradeBalanceShock` — a NEW `CabinetDecisionOption` field,
  but not a new channel: `ForeignPolicyMeetingOption` already established the one-time
  trade-balance shock (TradeSystem recomputes, so it fades exactly like the Crime/Poverty
  shocks) — plus `BudgetImpact` and `ApprovalEffect`.
- **Passive competence channel: none recommended.** The candidates (TradeSystem dampening,
  CurrencyStrength) are real trajectory contact on existing tracked variables — rule 11 ceiling
  audits for a channel the batch doesn't need. Decisions-only, same recording as Defense.
- **A FINDING, stated rather than absorbed (the ForeignPolicySystem file, not this batch's
  code):** `MeetingChancePerDay = 0.01` carries a stale 121-era calibration comment ("roughly
  one meeting per 121-day turn, expected ~1.2"). At `DaysPerTurn = 365` the same 0.01/day is
  ~3.65 expected meetings per turn (97% chance of at least one) — the comment's claim is now
  false while the DAY-DENOMINATED experience is unchanged (about one meeting per 100 days). The
  capture warm-ups' habit of finding a meeting within the first days of play is this number
  working as coded. Under R4: whether to keep 0.01/day (recommended — the per-day cadence was
  the designed experience) and fix the comment, or re-rule the cadence itself.

### Education

- **Decision space:** curriculum reform (with its public fights), a teacher shortage or strike,
  vocational-training and apprenticeship pushes, university funding disputes, school
  infrastructure backlogs.
- **Writes (decision outcomes):** `YouthUnemploymentShock` — a NEW option field landing on
  `EconomyState.YouthUnemployment`, which qualifies by the same rule that admitted CrimeIndex
  and PovertyRate: the stat mean-reverts every turn by construction (R4-1's reversion model),
  so a one-time nudge fades without its own ceiling. Youth-U is a Round 4 inputs-only stat with
  NO downstream readers, so the write's ceiling audit is empty — and the direction is
  compatible with the standing posture, which bars new STATS from writing existing variables,
  not events from nudging new stats. Plus `BudgetImpact` and `ApprovalEffect`.
- **Passive competence channel — the one genuinely attractive landing, under R3:** competence
  subtracts up to its bias from the youth-U reversion TARGET while appointed (point-of-use, the
  Part A idiom; appointment-gated, so the no-policy matrix is untouched; zero downstream
  contact for the same no-readers reason). This is the "youth-retraining term" the R4-1 record
  itself named as a follow-on candidate. **Recommended.** Explicitly NOT `PotentialGrowthRate`:
  rule 11 names it heavily stacked, and the Round 4 posture bars new entrants into that
  ceiling.
- **The name-class hazard, addressed at scoping rather than in a post-mortem:**
  `CabinetPortfolio.Education` becomes the THIRD "Education"-named identifier
  (`SpendingCategory.Education`, `PolicyNodeId.Education`) — the reference-class trap's exact
  shape, three instances of which were caught by eye in one week (the "Energy (Spending)"
  mislabel among them). The guard is already the right shape and R4-4 stays inside it:
  portfolio display names come only from `GameController.GetPortfolioName` (a portfolio-scoped
  switch), colors only from `UiPalette.GetPortfolioArea` (same), portraits only from the
  portfolio-TYPED `GetCabinetPortrait`. No name-keyed cross-class lookup exists on the
  portfolio path, and the batch adds switch cases, never a `DisplayName.Of`/`Spaced` call on a
  portfolio. The build-time check, named: grep `DisplayName.` call sites for portfolio
  arguments — the expected count is zero.
- **UI area mapping (minor, folded into R7):** proposed `ForeignAffairs → Trade`,
  `Education → Labor`, `Defense → Political` (the switch's existing default; a distinct new hue
  is the alternative if three-of-six sharing Political reads too flat on the roster screen).

## 3. Content volume and rule 9

Part A parity, which is the derivation (anything more is a design call under R5):

| unit | count | note |
|---|---|---|
| Ministers | **9** (3 portfolios × 3 philosophies) | one global pool per portfolio, Part A's structure |
| Decision scenarios | **18** (9 pools × 2) | each 2–3 authored options → ~40–50 option lines |
| Cadence | 12%/appointed-minister/turn, unchanged | all six appointed: expected 0.72 firings/turn |
| Repetition | pool of 2 per (portfolio, philosophy) | the same repetition class Part A knowingly shipped |

Magnitude classes for every authored option, inherited: `ApprovalEffect` within ±2 (EventSystem's
class tops at 5 for events; ministers stay smaller, per Part A's −1.5…+1 range),
`BudgetImpact` within ±$250B (Part A tops at 220), `YouthUnemploymentShock` within ±1.5 pts
(the poverty-shock class), `TradeBalanceShock` within ±0.5 (the meeting-option class).
Competence biases in Part A's per-channel classes; Defense/FA biases are authored but inert
until a channel lands (documented at the field) if R3 resolves decisions-only.

**Rule 9's unreversed half governs all nine persons** (roadmap: "Cabinet ministers, party
leaders, legislators, Fed Chairs and heads of state remain original and fictional" — "this half
is not negotiable"). Everything in §4 is fiction.

## 4. The name list — ruling 6's deliverable

**Pool structure: Part A's, stated plainly — one GLOBAL candidate pool per portfolio, not
per-country.** The same nine characters serve whichever country the player governs.
"Plausible per country" is met the way the existing nine met it: deliberately cross-cultural
given/surname pairings that read as cabinet members of a generic developed democracy rather
than as any one nation's roster (Voskresenskaya, Osei-Bonsu, Tanaka precedent). All names are
ASCII (the portrait-slug convention has only ever handled ASCII).

| portfolio | philosophy | name | character in one line | portrait slug |
|---|---|---|---|---|
| Defense | Reformist | **Katarzyna Ekelund** | wants procurement audited in the open — believes opaque defense contracting is where readiness actually dies | `portrait_cabinet_defense_katarzyna_ekelund` |
| Defense | Pragmatic | **Rafael Iwasaki** | capability-planning technocrat; buys what the threat assessment says, not what the parade needs | `portrait_cabinet_defense_rafael_iwasaki` |
| Defense | Traditionalist | **Gunnar Petrakis** | deterrence through visible strength; distrusts any reform that reads as weakness abroad | `portrait_cabinet_defense_gunnar_petrakis` |
| Foreign Affairs | Reformist | **Camille Adeyemi** | institution-builder; thinks the multilateral table is where middle powers actually win | `portrait_cabinet_foreignaffairs_camille_adeyemi` |
| Foreign Affairs | Pragmatic | **Zofia Nakamura** | interests-first dealmaker; every communiqué is judged by what it moves, not what it says | `portrait_cabinet_foreignaffairs_zofia_nakamura` |
| Foreign Affairs | Traditionalist | **Aleksander Whitfield** | alliances and protocol; believes predictability is a foreign policy, and a good one | `portrait_cabinet_foreignaffairs_aleksander_whitfield` |
| Education | Reformist | **Yuki Dahlberg** | curriculum modernizer; argues the system trains students for an economy that no longer exists | `portrait_cabinet_education_yuki_dahlberg` |
| Education | Pragmatic | **Nadia Fitzgerald** | evidence-based incrementalist; pilots before mandates, data before both | `portrait_cabinet_education_nadia_fitzgerald` |
| Education | Traditionalist | **Tobias Marchetti** | standards and fundamentals; wary of every reform that trades rigor for relevance | `portrait_cabinet_education_tobias_marchetti` |

**How "no collision" was checked — the absence claim's named search, four parts, run
2026-08-17:**

1. **By construction:** each name is a cross-cultural pairing (Polish+Swedish, Iberian+Japanese,
   Nordic+Greek, French+Yoruba, Polish+Japanese, Polish+English, Japanese+Swedish,
   Arabic+Irish, German+Italian) — structurally unattested in any single country's political
   class, which is the same guard the existing nine rely on.
2. **Model-knowledge sweep:** each full name, and each surname within its portfolio, checked
   against cabinet-level officeholders past and present of the six simulated countries (USA,
   Sweden, Germany, France, Italy, Poland). No full-name match. One prominent-surname overlap
   was caught by this sweep and REMOVED before it reached this list: the Education Reformist
   was drafted "Yuki Andersson" and renamed — Andersson is the surname of a recent Swedish
   prime minister, and the sign-off should not have to carry that footnote.
3. **Live web search, each exact full name, quoted:** no officeholder or public figure matches
   any of the nine. Nearest hits, recorded for honesty: "Katarina Ekelund" (different name — a
   private-sector R&D manager); "Camille Adeyemi" appears as a fan-wiki fiction character and
   as a TV character played by an actor surnamed Adeyemi (no real person); Nadia Fitzgerald and
   Tobias Marchetti match private individuals on social platforms — **private-citizen homonyms
   are unavoidable for any plausible name and are not the bar** (Part A's "Jonas Lindqvist"
   has thousands); the bar, met by all nine, is no officeholder or public figure, full-name,
   past or present.
4. **In-game name space:** no overlap with the 16 existing portrait names — including given
   names, which is stricter than the existing space holds itself to (it doubles "Marcus").

## 5. RULINGS NEEDED

- **R1 — the name list (§4).** Sign off the nine, or edit; portrait slugs and the D1 request
  derive from whatever survives.
- **R2 — decision write-target set.** Add `TradeBalanceShock` and `YouthUnemploymentShock` to
  `CabinetDecisionOption` (each mirrors an established channel: the meeting option's
  trade shock; the mean-reverting-stat rule). Defense adds nothing. *Recommend: yes, both.*
- **R3 — passive competence channels.** Defense: decisions-only, recorded (no channel exists).
  Foreign Affairs: decisions-only, recorded (the candidates need rule-11 audits the batch
  doesn't otherwise need). Education: the youth-U reversion-target term (appointment-gated,
  no downstream readers; the R4-1 record's own named candidate). *Recommend as stated; the
  uniform alternative — all three decisions-only — is cleaner but wastes the one free channel.*
- **R4 — Foreign Affairs vs `ForeignPolicySystem`.** Coexist (recommended) or absorb. And the
  stale-cadence finding: keep 0.01/day and fix the 121-era comment (recommended), or re-rule
  the meeting cadence itself.
- **R5 — content volume.** Part A parity (2 scenarios per pool, 18 total — recommended) or 3
  per pool (27) to cut repetition.
- **R6 — the trajectory acceptance criterion, concrete form.** (i) The byte-diff matrix keeps
  **byte-identical, zero NEW fields, 6/6** as its criterion — movement is a fail, per §1's
  derivation, and this holds under every option in R2–R4 because every channel is
  appointment-gated and the dump appoints no one. (ii) The bounded-and-explained standard
  applies to the anomaly matrix's `cabinetstress` scenario (which auto-appoints all six
  portfolios by enum iteration): **zero new anomaly TYPES, counts in the same range as the
  other scenarios** (the recorded Part A / Phase 2 form), with every authored magnitude inside
  §3's classes. Rule on both halves as the batch's standing bar.
- **R7 (minor) — portfolio area colors.** `ForeignAffairs → Trade`, `Education → Labor`,
  `Defense → Political` (default) vs a new distinct hue for Defense.

**Stop.** Nothing builds until R1–R6 land; R7 can default.
