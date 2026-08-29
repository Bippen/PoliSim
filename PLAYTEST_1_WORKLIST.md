# PoliSim — Playtest-1 Work List (Elias's Sweden session, 2026-08-29)

**Same contract as the elections list:** work down in order, one commit per item, stop at item
boundaries, R-SP1 push, report last-completed / first-unfinished. Reversible forks decided and
logged (R-N1). This list interleaves with `ELECTIONS_PROTOTYPE_WORKLIST.md` per the SEQUENCING
section at the end — read it before starting.

**Record first:** these eleven findings are §P's first real output. Write them into §P/§V in
Elias's words, dated, before any item runs.

---

## Track P-A — cuts and hygiene (quick; a session's tail)

**P-A1 — The meta-text census and cut.** Developer-facing text is leaking into player surfaces
("COMPLETED" in the laws tab, progress markers, anything addressed to the builder rather than the
player). Census every UI string against a banned-token review (completion/progress language,
internal section references, build vocabulary); classify player-facing vs developer artifact; cut
every artifact. *Done when:* the census table is in the report with counts, the cuts are filmed on
touched screens, and **`MetaTextCheck`** is armed — a guard that scans player-reachable UI strings
for the banned-token list (enumeration stated in its header) so the class cannot silently return.

**P-A2 — The "as published" graph block dies.** Remove the redundant published-series graphs at
the bottom of Statistics. **The `PublicationSystem` mechanism is untouched** — it is load-bearing
(the election model's §19 perceived-performance reads Published, never State) and its honesty
conventions (PRELIMINARY, revision frames) stay on the main graphs where they already live. This
is a display cut, not a mechanism cut; say so in the commit. *Done when:* filmed, and a harness
line proves the election model still reads the published series.

## Track P-B — fiscal UX (small features, existing machinery)

**P-B1 — Yearly budget impact on drafts.** Every tax/spending draft change shows its estimated
annual fiscal impact before enactment: revenue delta, spending delta, net budget delta, as a
range in the established estimate idiom (the law browser's convention — margin stated, never
false precision), derived from the model's own projection. *Done when:* filmed on the Budget and
Tax/Welfare surfaces, and the displayed range's width matches the model's actual projection
uncertainty.

**P-B2 — The first-year budget.** Entering office opens the budget process immediately for the
first fiscal year — the player lays a budget on arrival instead of waiting for the calendar's
next cycle. Model it as the incoming-government budget window (Sweden's own practice), as data on
the campaign/fiscal calendar, per country. The seeded budget remains the starting point; the
player amends it through the normal draft→enact flow. *Done when:* a new Sweden game can enact a
first-year budget in month one, the trajectory with no player change is byte-identical to today's
(the window's existence must not move the no-policy baseline), and the calendar sheet shows the
window.

## Track P-C — currency correctness (display first; the seed question surfaced, not smuggled)

**P-C1 — National currency display.** Every domestic figure renders in its country's currency —
kr (SEK), €, zł, $ — symbol, placement and formatting per locale convention, `InvariantCulture`
parsing preserved, `MoneyUnit`'s load-bearing behaviour extended rather than replaced. *Done
when:* filmed across all money surfaces for Sweden and one euro country, guards silent, and a
unit test covers each country's format.

**P-C2 — The basis question, RULED before built.** Determine what unit the seeds actually store
(the Desk shows Sweden's GDP as $620B — a USD basis). Report the finding, then execute the
ruling: **figures store and display in national units, with cross-country views converting at a
sourced, vintage-dated rate.** If that requires re-basing seeds, it is a seed change: full
sim-math bar, per-country diffs explained, perimeter-consistency respected. If the model turns
out to be unit-agnostic (index-based) with only display affected, say so and close cheap. *Done
when:* the basis is documented in the seed doc, whichever branch ran.

## Track P-D — central bank independence

**P-D1 — Rate control leaves the player's hands.** For all six countries the central bank decides
rates by its own reaction function — declared parameters, documented, tagged (sourced where a
published rule exists, `[AUTHORED-DRAFT]` where not), running on the model's own inflation and
output readings. The player's remaining levers: **appointments** (the Fed-chair machinery is the
template — generalise it) and nothing else in the prototype; pressure mechanics are recorded as a
future feature, not built. Riksbank first, filmed; the others follow as data. *Done when:* no
player-reachable control sets a rate, the bank's decisions appear on the calendar/ledger as its
own actions, a full run shows plausible rate paths against the model's inflation, and the
no-policy baseline change (rates now move where they didn't) is captured as a new explained
trajectory family.

## Track P-E — the international browser

**P-E1 — Country pages on International.** The empty tab gains a browsable per-country summary:
each of the other five in relation to the player's country — headline stats side by side (GDP,
growth, unemployment, inflation, debt), trade volume between the pair (the map's own data),
compass positions compared, and the relations facts the model actually holds. Browsable
prev/next, structural v3 idiom, flag + party marks where relevant. **Only what the model holds** —
no invented "relations score." Gaps in what the model can say about a pair are left visible, and
each becomes a line in the Design ask. *Done when:* filmed at four sizes for at least three
country pages, every figure derived, and the empty-tab dead space measurably consumed.

## Track P-F — the Policy Web, structural comprehension (board 2b stays Design's)

**P-F1 — Focus mode and encoding.** Within R-W2's fence, ship structurally what comprehension
needs now: click a node → non-connected nodes and edges dim, the selected node's edges highlight
with **direction arrowheads** and **weight-scaled thickness** (the coupling table's own
magnitudes), the DERIVED/DECLARED distinction preserved, a second click or empty-space click
restores. No legend yet (board 2b's), no invented edges, neutral ink. *Done when:* filmed in rest,
focused, and restored states at 1280 and 2560, and every encoded weight traces to the coupling
table.

**P-F2 — Board 2b status, surfaced.** Report whether Design ever received the D7 ask (the
`85690abf` package). If not: the ask stands, the paste is Elias's, and P-F1's films join its
annex as the new current state.

## Track P-G — economic legibility and responsiveness (the deep finding, split honestly)

**P-G1 — The shadow baseline.** The live game computes a parallel no-policy run — same seed, same
events where player-independent, zero player deltas — so every economic graph can show **"with
your policies" against "without"**. Deterministic, computed incrementally at the turn boundary.
This is the single strongest answer to "the economy feels disconnected": the counterfactual is
the impact, drawn. *Done when:* a harness proves the shadow run equals the recorded no-policy
baseline for an untouched game, the divergence after a known dial change equals the batch-diff
for the same change, and the cost per turn is measured and stated.

**P-G2 — The impact ledger.** The trace-term infrastructure pointed at the player: a "your
policies" view attributing the live-vs-shadow divergence to enacted changes (the delta-composed
law system makes this computable). Same idiom as the approval attribution ledger. *Done when:*
attribution lines sum to the actual divergence within stated tolerance, on film.

**P-G3 — The responsiveness audit (measure, then calibrate only with sources).** Controlled
dial-response experiments: each major tax/spending dial stepped ±(small, large) from the Sweden
baseline, trajectory deltas measured at 1/2/5 years, tabulated as the model's implied
multipliers/elasticities. Compare against sourced literature values (named studies/institutions,
vintage). Where the model is materially off, **recalibration is proposed with the sourced basis
attached — not applied** until Elias rules; where the literature disagrees with itself, the range
is reported. No constant moves in this item. *Done when:* the multiplier table exists with
sources beside it and a recommendation list, each line strikeable.

**P-G4 — Graph legibility of cause.** Enactment markers on the Statistics graphs (the release-tick
idiom, weight per 1l) so "what did I do and when" is visible on every series the player reads.
*Done when:* filmed, and markers derive from the enactment record.

## Track P-H — the tax system, deepened (spec-let first; the big build second)

**P-H1 — The tax spec-let and sourcing bill.** One document before any code: the real revenue
instruments per country (Sweden: kommunalskatt, statlig inkomstskatt with its brytpunkt, capital
at 30 %, payroll/arbetsgivaravgifter, moms tiers, corporate — and the equivalents for the other
five), which become player-changeable and at what granularity (brackets as data), how revenue
computes (requires the income distribution — depends on Track P-I's cohort/income data or an
interim sourced distribution), how the existing budget decompositions' revenue lines map onto the
new instruments, and the full sourcing bill with candidate primary sources. Sized in sessions.
*Done when:* the spec-let is at root and Elias has a strikeable design to rule on — **no tax code
is built from this list until the spec-let is ruled.**

## Track P-I — age-structured demographics (the substrate build)

**P-I1 — The cohort spec-let.** Same discipline: single-year vs 5-year cohorts (recommend 5-year:
sourced cleanly from Eurostat pyramids, sufficient for pensions/labor/education), the aging step
at year boundary, births/deaths/migration from sourced rates, what the cohorts drive (labor force
participation by age, pension and education cost weights, and — explicitly — **the election
system's voter groups, which become views over this same substrate rather than a parallel
population**: one demography, two consumers). The collision map with today's scalar demographics.
*Done when:* the spec-let is at root, ruled before built.

**P-I2 — Build per the ruled spec-let** (sized there; sourced pyramids per country; sim-math bar;
new explained baselines; the election voter groups migrated onto the substrate with the backtest
re-run to prove nothing regressed).

---

## SEQUENCING against the elections list

1. **The elections list keeps priority to its playable milestone** — W-E1/E3/E4, then the order as
   written through W-G — because 13 September is fixed and this list is not.
2. **P-A and P-B ride session tails** — they are small, independent, and the meta-text cut
   improves every screen the elections track films.
3. **P-F1 and P-G4 are small and high-leverage** — slot them after the elections screens' first
   milestone.
4. **P-C, P-D, P-E, P-G1/G2/G3** follow the elections list's W-G (wiring) — P-D and P-C2 move
   baselines, and baseline moves should not interleave with the wiring's own baseline change.
   Two explained baseline changes at once is one too many.
5. **P-H and P-I start as spec-lets only** (P-H1, P-I1 can be written any time — they are
   documents) and **build after** the elections prototype stands, unless Elias re-orders. They are
   the next era, not this sprint.

## RULINGS Elias owes this list (each one line)

The P-C2 basis ruling executes as written unless struck · P-G3's recalibrations apply only on his
strike-or-bless of each line · P-H1 and P-I1 spec-lets need his ruling before code · board 2b's
paste (P-F2) remains his gesture.
