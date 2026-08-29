# Elections Day-1 — the report (2026-08-29)

**Authority:** Elias's Day-1 kickoff. Anchor verified at start: HEAD `996f4b5` == `origin/main`,
tree clean. Discipline v2; **R-N2 holds throughout — the election system is UNWIRED and every
part ends with the trajectory suite byte-identical**; R-SP1 push per part.

## Phase 0 — the spec: STOP CONDITION MET

**`ELECTIONS_CAMPAIGN_SPEC.md` is absent: 0 sections found.** It did not arrive in the kickoff
message (which carried the kickoff text alone), and it is on no reachable path — searched the
repo root and tree, `PoliSim-captures/` including `inbox/`, Downloads, Desktop and Documents
for `*ELECTION*` / `*CAMPAIGN*` and for any `.md` newer than 2026-08-29 00:00. The only hits
are the work already on disk (`ElectionsData/`, `Assets/Scripts/Elections/`,
`ELECTIONS_ARCHITECTURE.md`) and old capture PNGs. **This is the second consecutive attempt in
which the spec did not travel with its instruction** (the overnight queue was the first).

Per Phase 0's own instruction: **Phases 1 and 2 are NOT run** — the 44-section gap table, the
§7 types and compatibility core, and the §39 chain (§8 loyalty damping, §26 turnout, §27
regional aggregation) stay parked, unguessed. **Phases 3, 4 and 5 run.**

Where Phase 4 needed a chain that Phase 2 would have built, it uses an explicitly-declared
placeholder instrument instead, labelled as such in code and here (see Phase 4) — never the
spec's model wearing the spec's name.

## The call log (every line strikeable)

- **[call]** Phase 4's instrument is the smallest defensible spatial model — a 2-D Gaussian
  electorate over the two CHES axes actually sourced (lrecon, galtan), proximity choice by
  softmax, four parameters total (mu_econ, mu_soc, sigma, tau) and NOT ONE party-specific
  constant. It is `VoteModel.cs`, whose class doc lists in full what its silence does not
  claim. When the spec arrives, its chain REPLACES this file; nothing here is meant to survive
  contact with §39.
- **[call]** Issue-to-axis mapping for the salience weights (from the sourced EB105 / Gallup
  tables): economic issues (prices, cost of living, economic situation, government debt) →
  lrecon; immigration, crime, threats to democracy, environment, security & defence → galtan;
  everything else (health, education, international situation, Middle East, Ukraine,
  government/leadership) dropped rather than forced onto an axis. `wEcon` floored at 0.15 so no
  country collapses to a single axis — Sweden is the country the floor binds on (EB105 puts no
  economic issue in its top five).
- **[call]** Party aggregations where the sourced position data and the sourced returns
  disagree in granularity, each stated in the harness: Poland TD = mean of Polska 2050 and PSL;
  Italy AVS = mean of SI and EV; France NFP = mean of LFI, PS and EELV, and RN is taken as
  RN+UXD (the alliance, 33.22 %) — the Ministry's own nuance grid keeps them apart.
- **[call]** Actual shares are renormalised over the MODELLED parties on both sides of the
  comparison (minor lists and any party CHES does not cover are excluded from both), so the
  coverage percentage is printed per country rather than the remainder being silently absorbed.
- **[call]** The USA case is run and reported as DEGENERATE: two parties on a plane cannot test
  a spatial model's discrimination, and its positions are GPS-2019 (pre-2020 vintage). It is
  included for completeness, not as evidence.

## Phase 3a — R-EL8, the USA's real allocation: BUILT AND EXACT

`Assets/Scripts/Elections/ElectoralCollege.cs` (pure, unwired) implements the rule from the
statutes, not from the answer: winner-take-all as the **state choice** 48 states and DC direct,
and the congressional-district method where Maine and Nebraska direct it instead. Sourcing is
`ElectionsData/usa/district_method_2024.md` — **Me. Rev. Stat. tit. 21-A §802** ("One
presidential elector shall be chosen from each congressional district and 2 at large"),
**Neb. Rev. Stat. §32-710** (structure) with **§32-1038(1)** (allocation: highest statewide vote
elects the two at-large electors, highest vote in a district elects that district's elector),
under **U.S. Const. art. II §1 cl. 2** and NARA's 538/270.

**Result: Trump 312 / Harris 226 — EXACT, computed from the seven sourced ME/NE
statewide-and-district pluralities rather than read from a pre-split column.** The allocator
asserts its own elector total (538) and reports the no-majority contingent case rather than
resolving it.

**The counterfactual that justifies the ruling.** Forcing every jurisdiction to winner-take-all
*also* yields 312/226 — because Maine's district elector (ME-2 → Trump) and Nebraska's (NE-2 →
Harris) are one each way and cancel exactly. Two electors are misassigned and the national total
is unchanged. Last night's run had matched 312/226 with the split *supplied as data*; that match
was luck, and R-EL8 is why it is no longer load-bearing. Both numbers print side by side in the
harness so the coincidence can never be mistaken for a validation again.

**Three corrections and cautions the sourcing forced, each recorded rather than quietly fixed:**
1. **The kickoff's Nebraska citation was wrong.** §32-714 governs elector vacancies and
   faithless electors, not the district method. The correct pair is §32-710 + §32-1038(1).
2. **Nebraska's statute text is an Internet Archive capture, not a live fetch** —
   `nebraskalegislature.gov` refused connections throughout; corroborated by the NE Secretary of
   State's own canvass book, which recites the rule and cites §32-1038. Re-verify when reachable.
3. **A live-rule expiry, flagged `[UNCONFIRMED]`:** LB3 in Nebraska's 109th Legislature is
   described as amending §32-1038 to make all five electors statewide. Unverified (same
   unreachable host). **Do not model Nebraska as permanently district-method.**
   Also recorded: Maine's presidential race is legally RCV-eligible (§1(27-C)(D)) and was decided
   in round one only because Harris cleared §723-A(2)'s inclusive 50 % bar at 51.71 % — treating
   ME as plurality is correct for 2024 and must not be generalised.

## Phase 3b — R-EL9, Italy's Rosatellum: SOURCED, BUILT, EXACT

The overnight pass refused to run Italy because its *structure* was sourced but its *allocation
arithmetic* was not, and R-N4 forbids running un-sourced arithmetic. That gap is now closed to
the same statute-cited standard as the other five: `ElectionsData/italy/rosatellum_allocation.md`
carries DPR 361/1957 art. 83 as consolidated and in force at 25-9-2022, quoted clause by clause.

**The method is Hare with largest remainders applied TWICE, with the quotient truncated to an
integer both times** ("non tiene conto dell'eventuale parte frazionaria") — which is why it could
not run on the existing divisor-method machinery and needed its own implementation,
`Assets/Scripts/Elections/Rosatellum.cs` (pure, unwired).

**Result — ITALY 2022 CAMERA, proportional stage, 245 seats: total absolute seat deviation 0**,
every one of eleven lists exact: FdI 69, Lega 23, FI 22, PD 57, AVS 11, M5S 41, Azione–IV 21,
SVP-PATT 1, and correctly ZERO for Noi Moderati, +Europa and Impegno Civico. The run reproduces
three distinct statutory behaviours at once:
- the **1 % strip** (Noi Moderati 0.90 % and Impegno Civico 0.62 % struck from their coalitions'
  figures, not redistributed, while still sitting in the threshold denominator);
- the **1–3 % transfer** (+Europa at 2.83 % counts toward the centre-left's figure — buying it
  seats — but is excluded from the intra-coalition distribution, so its votes end up with PD and
  AVS);
- the **minority route** (SVP-PATT admitted at **0.42 % nationally**, an eighth of the 3 % bar,
  under art. 83 c.1 lett. e) n.2 — it met both limbs: 23.15 % regionally and 2 of Trentino-Alto
  Adige's 4 colleges).

**A textual ambiguity settled by arithmetic, not by preference:** lett. g)'s divisor is the sum of
the coalition's ADMITTED lists, not the coalition's own figure. Reading it the other way puts the
result **nine seats out** (PD 50 / AVS 9). The implementation states the correct reading and the
data file records the check.

**Structural fact the model depends on:** the two tiers are **parallel, not compensatory** — there
is no *scorporo*. The 146 college seats come out of the POOL; nothing in art. 83 reads a party's
college wins. The Camera's own 2018 dossier proves it empirically (the centre-right won 111
colleges and still took its full proportional share; LeU won none and got no compensation).

**Scope stated, so silence claims nothing:** this is the NATIONAL stage. The devolution to the 28
circoscrizioni (lett. h/i), the 49 collegi (art. 83-bis) and the art. 84 *incapienza* cascade are
NOT implemented — they need per-circoscrizione and per-collegio *cifre elettorali* that exist only
as HTML on Eligendo, and they change WHICH deputies sit, not the per-list national totals. One
trap recorded for whoever attempts them: **the Ministry's comune-level open-data CSV must not be
fed into the allocation** — its `VOTILISTA` sums undershoot Eligendo's published figures by
2.6–4.6 %, almost certainly because art. 83 operates on the *cifra elettorale* after art. 58 c.3
redistributes uninominal-only ballots.

## The seat table after Phase 3 — five of six reproduce EXACTLY

| country | rule implemented | deviation |
|---|---|---|
| Sweden 2022 | national modified Sainte-Laguë 1.2, 4 % threshold, 349 | **0** |
| Germany 2025 | national Sainte-Laguë/Schepers on exact counts, 5 % + SSW exemption, 630 | **0** |
| Poland 2023 | d'Hondt in each of the 41 okręgi, national eligibility, MN exempt, 460 | **0** |
| Italy 2022 | Rosatellum national proportional stage, floored Hare ×2, 245 | **0** |
| USA 2024 | Electoral College: 49 WTA jurisdictions + ME/NE district method, 538 | **0** |
| France 2024 | — **NOT RUN, and not a gap in the model** | n/a |

**France is the honest exception, not an omission:** a two-round majority system in 577
single-member circonscriptions has **no national allocation to implement** — seats are decided
constituency by constituency across two rounds, so reproducing it requires the per-circonscription
results of both rounds (the Ministry publishes them; the night's pass deliberately did not fetch
577 × 2 tables for a model that claims nothing about them). Reporting it as "5 of 6 exact, the
sixth structurally out of scope" is the accurate statement; "6/6" would require building a French
constituency model that no part of the plan has yet asked for.

## Phase 4 — the vote-share backtest (the centerpiece): THE TABLE

Sourced positions (CHES 2024 / GPS-2019) + sourced salience (EB105 Spring 2026 / Gallup July
2026) → `VoteModel` → predicted national vote shares, against the official returns.
Campaigns OFF. Two runs per country: **prior** = zero fitted parameters (a neutral electorate,
μ=(5,5), σ=2, τ=2); **calibrated** = the one allowed pass, four numbers per country, all four
printed. Log: `votebacktest_20260829.log`.

| country | modelled parties | vote coverage | prior MAD | calibrated MAD | the four calibrated parameters |
|---|---|---|---|---|---|
| Sweden | 8 | 98.5 % | 6.43 pp | **3.25 pp** | μ=(3.25, 6.25) σ=3.00 τ=0.50 |
| Germany | 8 | 95.4 % | 7.17 pp | **5.78 pp** | μ=(4.50, 6.50) σ=1.00 τ=16.0 |
| Poland | 5 | 96.3 % | 10.27 pp | **6.99 pp** | μ=(3.50, 7.00) σ=1.50 τ=8.00 |
| Italy | 7 | 88.8 % | 8.84 pp | **5.61 pp** | μ=(4.25, 7.00) σ=1.00 τ=4.00 |
| France | 4 blocs | 87.9 % | 10.00 pp | **1.16 pp** | μ=(4.25, 6.50) σ=1.50 τ=2.00 |
| USA | 2 (degenerate) | 98.1 % | 19.84 pp | 0.00 pp | μ=(5.75, 5.75) σ=1.00 τ=8.00 |

**Read the last two rows as structure, not skill:** France's 1.16 and the USA's 0.00 are what
happens when the party field is pre-aggregated into 4 blocs and 2 parties — with that few
points on the plane, four parameters can place the cloud almost exactly. The real tests are the
8-, 8-, 5- and 7-party fields, and they land at **3.3–7.0 pp**.

**The deviations are not noise — they have two systematic signatures, and both name the same
missing layers:**

1. **Empty-quadrant inflation.** A party alone in a region of the plane collects its whole
   geometric catchment: Germany's **BSW +10.2 pp** (sitting at 2.78, 7.06 — left-economic,
   authoritarian-social, a quadrant no established party occupies), Poland's **TD +15.9 pp**,
   Sweden's **KD +8.2 pp**. Real voters do not flow to a position merely because it is
   unoccupied — they need a reason to believe the party is a live option.
2. **Large-party under-prediction.** The biggest parties are systematically short: Germany
   **CDU −9.3**, **AfD −11.7**; Poland **KO −16.7**; Sweden **M −10.0**. Mass concentrates on
   established parties beyond what proximity alone explains.

Both signatures are the same absence stated twice: **there is no partisan loyalty / party-size
persistence term** — which is exactly what the spec's **§8 loyalty damping** is for. A third,
narrower deviation implicates a different layer: Germany's **CSU +7.4 pp** is a party that
contests one Land only, modelled as if it stood nationally — **§27's regional aggregation**.
Poland's TD error is partly an artefact of my own aggregation call (a coalition modelled as the
CENTROID of its two members lands in the middle of a bipolar system, which is the one place
Polish votes are not) — a caveat on the input, not a finding about the model.

**Verdict for sizing the E-phases:** a four-parameter, loyalty-free, region-free spatial model
reproduces multiparty national vote shares to ~3–7 pp mean absolute deviation. That is a usable
floor to build up from, and it says the first two rungs worth building are precisely the two the
deviations implicate — **§8 loyalty damping first, §27 regional structure second**. It also says
nothing more should be fitted: the fix is a layer with real content, not a fifth knob.
