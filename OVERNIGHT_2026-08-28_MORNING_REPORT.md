# The overnight full queue — morning report (2026-08-28 → 29)

**Authority:** Elias, 2026-08-28 night — "full queue, no input from me, works nonstop, iron out
issues in the morning." Rules R-N1…R-N4; carried rulings R-EL1/2/5/6/7. **This document is the
contract: every R-N1 call below is one line, strikeable by construction.** Built incrementally
through the night; state lines at the bottom are re-derived at each part boundary.

## The reversible-call log (R-N1) — every line strikeable

- **[call, 23:0x]** The queue arrived WITHOUT `ELECTIONS_CAMPAIGN_SPEC.md` in the same message
  (rule 13 asked for one message carrying both; only the queue text landed). Searched: repo
  root, `PoliSim-captures` (+ `inbox/`), Downloads, Desktop, Documents, OneDrive, all of
  `G:\UNITY` for any `*ELECTIONS*` or recent `.md` — result recorded below this line as found /
  not found. **RESULT: NOT FOUND** — the only recent inbox artifact is `Direction_2026-08-28.zip`,
  which holds the two UI direction docs, nothing electoral. So: the elections track proceeds
  spec-blind where honest (Part 3's sourced data is real-world and spec-independent; catalog
  shapes are session calls, logged), and every spec-shaped unit (the 44-section gap table, §7
  types, §39 chain layout, §§20–22 polling) is billed to the morning as blocked-on-the-spec
  rather than guessed at. R-EL7's borderline rule: what the spec alone could settle goes to
  `RULINGS NEEDED`.
- **[call, 23:1x]** Part 3 launched FIRST (R-N3 skip-forward — Unity still blocked by the live
  Hub installer): seven research agents in parallel, one per country for official returns +
  rules (Sweden 2022 · Poland 2023 · Germany 2025 · France 2024 legislative · Italy 2022 ·
  USA 2024) and one for party positions (CHES anchor; US from a named equivalent) + issue
  salience (Eurobarometer / national equivalent, vintage recorded). Output contract: primary
  source, access date, basis, exact figures; anything unconfirmable from a primary source
  arrives marked `[UNCONFIRMED]` and goes to the data bill, never into a catalog.
- **[call, 23:1x]** USA 2024 "the last real national election" read as BOTH races that matter to
  the sim's shape: the presidential result state-by-state (the regional table) and the House
  national party totals + seats. Spec-blind granularity call, strikeable.
- **[call, 23:1x]** The night's sourced data lands OUTSIDE `Assets/` in a new root folder
  `ElectionsData/` (CSV/MD with source headers): inert by construction — Unity never imports it,
  R-N2 cannot be threatened by data, and the catalogs' code-side readers come later with the
  types. Strikeable (a later pass can move them under Assets/StreamingAssets unchanged).
- **[call, 23:0x]** Part 1 (the Policy Web chain) resumes the moment Unity launches survive —
  the night opened with `UnityHubSetup-3.21.0-x64` live (21:52:57→), which was the evening's
  launch-failure cause (three 0x8007007E MOD_NOT_FOUND exits + one mid-startup death, no crash
  record, no `error CS` anywhere — the launches raced the installer's file swaps).

- **[call, 23:2x]** Unity launches WORK again while the Hub installer still shows as a process —
  census attempt 5 ran clean (the installer is presumably sitting at its finish page). The
  Part 1 chain resumed: the four-size `pweb_*` capture bar launched as one sequential guarded
  run. Attempts 1–4's failure class stands recorded above.
- **[call, 23:2x]** Parts 4–5 scaled to what needs NO invention (the spec being absent): the
  night builds the **vote-to-seat layer in full** — Sweden's modified Sainte-Laguë with
  adjustment seats and Poland's per-district d'Hondt from the verified docs (R-EL5), the other
  four from tonight's sourced rules — as pure deterministic functions with harnesses, and the
  backtest becomes: REAL sourced vote shares in → computed seats vs REAL seats, per country.
  That is a genuine, zero-invention backtest of the allocation implementations. The chain's
  upper layers (§7 types, §8 preference, §26 turnout, §27 aggregation-with-noise, §§20–22
  polling) are BILLED as blocked-on-the-spec, not guessed. Strikeable: the morning can order
  the full §39 chain built once the spec lands.
- **[call, 23:2x]** The architecture doc (Part 2.3) is drafted spec-blind ONLY in its
  spec-independent halves (state-vs-config, the named election noise stream appended to
  `SimulationRandom.Stream`, eventual wiring points, R-EL1 idiom mapping at the queue's own
  level); every section the spec alone can settle is a marked stub. Borderline per R-EL7 —
  logged here rather than parked silently.

- **[call, 23:3x]** The repo ALREADY constrains the allocation work
  (`POLISIM_SEED_DATA_MACRO_OVERHAUL.md` Part 5, read before writing a line of allocator C#):
  vote COUNTS never rounded shares (Germany's one-decimal band moves a seat); Poland/Italy per
  constituency or not at all (the 70-seat national-D'Hondt error); Sweden 2022 the one exact
  national anchor (the 39 adjustment seats make it so structurally). Tonight's vote-to-seat
  build + backtest IS the "port `seat_allocation_check.py` to C# and reproduce, re-derive
  Germany/Poland from scratch" that doc demands before anything is relied on — cited as R-EL5
  asks, not re-derived. The stranded branch stays UNINSPECTED (D0's discipline holds tonight).
- **[call, 23:3x]** Amendment to the research agents' contract, consequence of the above: the
  catalogs prefer ABSOLUTE VOTE COUNTS; a shares-only national table is marked `[SHARES-ONLY]`
  and joins the data bill (the agents were asked for shares — counts are followed up where the
  backtest is precision-sensitive: Sweden, Germany first).
- **[call, 23:3x]** E-0's spec-independent halves written while the capture bar runs (no Assets
  writes during a batch run): `ElectionsData/README.md` (the three data classes, the four cited
  constraints, the layout) and `ELECTIONS_ARCHITECTURE.md` (R-EL1 mapping, state-vs-config, the
  reserved `ElectionNoise` stream name — the enum itself untouched tonight, R-N2 — the four
  eventual wiring points, the R-EL6 inventory, the billed stubs).

- **[call, late]** Part 4/5 scope, finalized against the sourced rules: SWEDEN full (exact
  counts, expected exact); GERMANY at three-decimal share precision (counts billed; SSW entered
  at its seat-implied 0.152 % — the published 0.2 % is one-decimal rounding; strikeable);
  POLAND as the NATIONAL-d'Hondt SIGNATURE run (deliberately the wrong system, re-deriving the
  recorded 70-seat gap from scratch — the real 41-district allocation waits on the billed
  per-district counts; `PerDistrictSum` is ready for them); **ITALY NOT RUN** (the Rosatellum's
  allocation FORMULA was not sourced tonight — R-N4 forbids running un-sourced arithmetic;
  billed); **FRANCE NOT RUN** (two-round SMD — no national model exists by construction);
  **USA NOT RUN** (12 of 51 states fetched). Every not-run stated in the harness's own output
  so silence cannot read as coverage.
- **[call, late]** Part 6 (polling/momentum §§20–22): SKIPPED — the queue's designated tail,
  and its objects are spec-shaped (the spec is absent); billed with the other spec-blocked
  units. One line, as instructed.
- **[call, late]** Part 7 read as its parenthetical describes (the previous overnight files
  were superseded and did not arrive): the 1000-turn soak = the `traj_pweb_*` t1000 dumps
  (byte-identical to the baselines 6/6 — the divergence headline is ZERO divergence at every
  horizon); the film-sweep and suite repetitions folded into the night's already-run triple
  (det_a · det_b · pweb_1280 are three sweeps of near-one code with the diff fully classified)
  plus the eight checks (once green; a second run rides the elections-part boundary).
  Strikeable — the morning can order the literal triple/double if this reading under-shot.

## Part 3 — census of the Policy Web (Annex G's counting half, measured by `PolicyWebCensus`)

(Annex G material, recorded here until the request doc lands it:) 73 nodes = 55 policy + 18
stat; wedges Labor 6 · Crime & Justice 6 · Fiscal 28 · Welfare 6 · Sectors 5 · Sovereign
Wealth 2 · Trade 1 · Political 1 · Stats 18; one edge-less node stated by name (Tariffs (Tax
Line) — the enum-member-only case). Policy→stat edges: full set 121 = 73 derived + 48
declared; USA 120 (72 derived — the one missing edge is the policy rate's issuance edge, gone
under `BaseDebtInterestRateOverride`, exactly as `IsLiveFor` documents); the other five
countries 121 each. Stat→stat (the causal graph): 7, all derived. Log:
`webcensus3_20260828.log`.

## Part 1 — the Policy Web chain: THE PROOFS (R-W1 built, filmed, measured)

- **The film:** `pweb_*` at 1280/1600/1920/2560 — 79 captured / 0 failed / exit 0 at every size
  (guards silent in-run: overflows 0, escapes 0, canvas violations 0).
- **Occupancy, before → after (the plate on the 06d frame, measured by the same script both
  eras):** 1280: 41.1 → **56.1 %** of window (43.6 → **59.6 %** of sheet, plate 1120×328 →
  1120×448); 1600: 46.2 → **59.8 %** (48.9 → 63.3); 1920: 48.2 → **62.1 %** (51.0 → 65.6);
  2560: 74.6 → 74.6 (78.7 → 78.7 — the old 0.92·h ceiling already filled that frame). The
  fold-clip at rest (visible plate shorter than the diagram's floor at three sizes) is GONE —
  the plate is viewport-exact everywhere; viewed on the 1280 film.
- **Rule-15 byte-diff vs `det_a` (same code but R-W1 + the census tool):** 68/79 identical;
  differing = the SEVEN policy-web frames (06d, 06d_rows, 06d_deep, 06k ×2, 06l ×2 — the
  change's own screen, expected) + the three known wall-clock frames (01a, 89d, 92) + ONE new
  member of the time-envelope class: `01_country_selector` — pixel-diffed to bbox
  (465,392)–(814,615) = exactly the Italy card, sitting a few px lower (the Canvas card
  entrance easing caught mid-settle; both films viewed side by side). An IMGUI-only change
  cannot move a Canvas card; classified environmental, filed with the other envelope frames.
- **R-N2's suite:** trajectories `traj_pweb_*` ≡ `traj_v31bf_*` **6/6 by SHA-256** (2 seeds ×
  3 horizons — the 1000-turn dumps double as Part 7's soak input); `ScreenEdgeCheck` on the
  full `pweb_*` family: **316 captures, 0 clipped, exit 0**; `CheckSuite` first run exit 1 ON
  THE LICENSING FOLD (the "Access token is unavailable" Error lines — the post-Hub-install
  environment, quirk 17's class), re-run in flight; the verdict line lands below.
  **CORRECTED on the re-run's silence:** both suite exits were MY wrong entry point —
  `CheckSuite.Run` does not exist (a `[MenuItem]` and an editor-open hook only; `RunAll` is
  private); no check ever ran and nothing failed. The eight checks run the way the bars always
  ran them — eight separate `-executeMethod <Check>.Run` invocations, one chained guarded run;
  verdicts below when it lands. (The licensing Error line appears in green logs too — it does
  not fold a run by itself.)
  **THE VERDICTS (the chained run):** DeliveredAssetCheck 0 missing · ImporterSettings 148
  sprites 0 err/0 warn · StatIconCoverage 19/19 · PartyMarkCoverage exit 0 · PortraitCoverage
  25/25 · AreaIcons+emblems 14/14 · ChromeV2 both directions clean · UpstreamCheck exit 0 —
  **all eight exit 0. Part 1's gate is green on every axis.**
- **PART 1 CLOSED.** Phases: 0 records `c6cd7d3` · 1 R-W1+census `3b85543` · 2 Annex G
  `7959477` · 3 D7 installed `e30c82b` · 4 the package regenerated (ONE live ask, rows n of N,
  digest `85690abf…`) · 5 records (`COMPLETED.md` §48, CLAUDE.md's dated section, the
  roadmap's live edge, MISSING's §S row) — the phase-5 commit and the R-SP1 boundary push
  follow immediately. RULINGS NEEDED from Part 1: none.

## Part 3 — the sourced data spine: LANDED (all seven agents returned)

| unit | landed | class | vintage/basis |
|---|---|---|---|
| Sweden 2022 returns+rules | national EXACT COUNTS (8 parties, valid 6,477,970) + all 29 valkretsar + rules with the decision PDF's applied divisors | SOURCED [PROVISIONAL] | val.se RD_S.json "slutlig" + official xlsx |
| Poland 2023 returns+rules | national counts + ALL 41 okręgi (%, magnitudes) + Kodeks wyborczy article-cited rules | SOURCED [PROVISIONAL] | Dz.U. 2023 poz. 2234 + KBW CSVs |
| Germany 2025 returns+rules | 3-decimal national shares (BSW 4.981 nuance) + all 16 Länder + BWahlG/BVerfG-cited rules | SOURCED [PROVISIONAL]; counts billed | Bundeswahlleiterin final + kerg2.csv |
| France 2024 returns+rules | Ministry nuance grid national + 13 régions (Ministry's own CSV) + Légifrance-quoted rules | SOURCED [PROVISIONAL]; classification caveat | MI archived portal + data.gouv |
| Italy 2022 returns+rules | proportional counts+shares + 8 regions + Rosatellum cites; SEAT TOTALS [UNCONFIRMED] (Eligendo publishes PR seats only — billed) | SOURCED/PARTIAL | Eligendo Archivio + Normattiva |
| USA 2024 returns+rules | President (counts, EV, 12-state table) + House (counts, 220/215) + rules with the four exception regimes | SOURCED [PROVISIONAL] | FEC + Clerk of the House + NARA |
| Positions + salience | CHES 2024 final v2 (34 party rows, codebook-quoted scales) + GPS-2019 US (named substitute, gaps marked) + EB105 Spring 2026 + Gallup Jul 2026 | SOURCED [PROVISIONAL] | vintages in the files |

All under `ElectionsData/` with per-file source registers; `DATA_BILL.md` carries every gap
with its candidate source. Not one illustration or authored number entered a catalog.
- **Annex G's counting half:** the census block above (73 nodes, 121/120 edges, 7 stat-stat).
- **The ring's arithmetic at the new rect (for board 2b):** radius = min(w, h)/2 −
  labelMargin(widest wedge header) − MaxNodeDiameter/2 — height-bound everywhere; the wide
  plate's horizontal room is structurally unused by a circle. That is the composition question
  the board owns (R-W2), stated in Annex G.

## Part 1 — the Policy Web chain (carried in)

State at queue arrival: phase 0 committed (`c6cd7d3`); R-W1 built in code (uncommitted, commits
with its film); `PolicyWebCensus` written (uncommitted); before-occupancy measured from the
`v31bf_*` films — the plate at rest is **41.1 / 46.2 / 48.2 / 74.6 %** of the window at
1280/1600/1920/2560 (43.6 / 48.9 / 51.0 / 78.7 % of the sheet), and at the three smaller sizes
the visible plate (328 px at 1280) is SHORTER than the diagram's own 0.5·h floor — the ring is
clipped by the fold at rest; label ladder: body 17/22/26/30, wedge headers 16/21/25/29 px.
`det_a` reproduces `v31bf` byte-exactly at 1280 (the script cross-check).

## Parts 4–5 — the vote-to-seat layer and its backtest: THE CENTERPIECE, LANDED

`Assets/Scripts/Elections/SeatAllocation.cs` — pure functions, wired to nothing (the only
caller is the editor harness): d'Hondt · Sainte-Laguë/Schepers · modified Sainte-Laguë (1.2)
as divisor sequences, national thresholds with minority exemptions (the statute's own
denominator), and `PerDistrictSum` for the real Polish system (ready for the billed
per-district counts). Deterministic; no string in any signature (B3 cannot reach it); the tie
policy stated in full. `Assets/Editor/SeatAllocationBacktest.cs` runs it — zero free
parameters, every input a cited figure from `ElectionsData/`.

**The backtest (campaigns OFF, no tuning — deviations are findings):**

| country | method | result |
|---|---|---|
| **Sweden 2022** | national modified Sainte-Laguë 1.2, 4 % threshold, 349 | **EXACT — total absolute seat deviation 0** (all eight parties: S 107, SD 73, M 68, V 24, C 24, KD 19, MP 18, L 16) |
| **Germany 2025** | national Sainte-Laguë/Schepers, 5 % + SSW exempt, 630 | **EXACT — total deviation 0** (CDU 164, AfD 152, SPD 120, Grüne 85, Linke 64, CSU 44, SSW 1; BSW/FDP 0) — at three-decimal share precision, better than the recorded off-by-1 band |
| **Poland 2023** | NATIONAL d'Hondt — deliberately the wrong system (signature run) | **total deviation 70: PiS 169 (−25), KO 147 (−10), TD 69 (+4), NL 41 (+15), Konf 34 (+16)** — digit-for-digit the recorded branch-side signature (PiS 169, Konf 34), independently re-derived: the allocator AND the branch's claim are both confirmed without inspecting the branch |

Synthetics: ALL PASS (the divisor-decisive case — pure S-L seats B, the 1.2 modification takes
the seat away — and the d'Hondt-vs-S-L divergence case; plus the per-district-vs-national
structural gap demonstrated on synthetic districts: [4,2,0] vs [4,1,1]). Not run, stated in
the harness's own output: Italy (formula unsourced — billed), France (no national model
exists), USA (12 of 51 states fetched). Harness exit 0; logs `backtest_seats_20260829.log`,
`backtest_seats2_20260829.log`.

**THE SECOND FETCH AND THE DEFINITIVE RUNS (a follow-up agent worked the bill down the same
night):** `poland/district_votes_2023.csv` (the KBW absolute-votes file, all 41 okręgi × six
committees, ALL SIX national sums matching exactly) and `germany/national_counts_2025.csv`
(kerg2.csv's Bund rows, the Gültige total exact) landed, the two bill lines struck. Then:

| definitive run | result |
|---|---|
| **Germany 2025 on EXACT COUNTS** (kerg2's own integers, same regime) | **EXACT — total deviation 0** (all nine rows) |
| **Poland 2023 REAL** — d'Hondt in each of the 41 okręgi over the absolute counts (magnitudes sum 460, national eligibility, MN exempt) | **EXACT — total deviation 0: PiS 194, KO 157, TD 65, NL 26, Konf 18, MN 0 — the actual Sejm reproduced seat-for-seat** |

**Three chambers now reproduce EXACTLY from official counts (Sweden, Germany, Poland-real),
and the national-Poland signature run stands beside them as the measured 70-seat proof of WHY
constituency-level allocation is mandatory. The overhaul doc's Part 5 obligation is
DISCHARGED in full for all three countries it names.**

## Part 2 — E-0, what landed of it (the spec-independent halves)

Unit 1 (spec verbatim at root): **BLOCKED — the spec never arrived**; billed. Unit 2 (the
44-section gap table): **BLOCKED on the spec**; its EXISTS column is ready — the R-EL6
inventory in `ELECTIONS_ARCHITECTURE.md` (ElectionSystem's fixed cycle + approval threshold +
transient result; `PartyArchetype`'s four archetypes with `TotalSeats = 200` and
`ParliamentSeats`; seat drift; the hemicycle/compass/map renderers; `PublicationSystem` as the
polling substrate; five `mark_party_*` sprites drawn by nothing; the Fed-chair election-eve
pause; the stranded branch UNINSPECTED per D0). Unit 3 (architecture): **DONE in its
spec-independent halves** — R-EL1 mapping, state-vs-config, the `ElectionNoise` stream name
reserved (enum untouched), the four wiring points documented-not-done, the billed stubs. Unit
4 (§7 types + compatibility core): **BLOCKED on the spec** (§7 is the spec's); the layer that
could be built without invention was built instead (Parts 4–5 above). The roadmap gained the
era note at the live edge; the full Elections-era roadmap section is the morning's to place
once the spec exists to cite.

## Part 6 — polling + momentum: SKIPPED

One line, as the queue instructs for its designated tail: §§20–22 are spec-shaped and the spec
is absent; billed with the other spec-blocked units.

## Part 7 — the measurement night (the interpretation call is in the log above)

The 1000-turn soak: `traj_pweb_s777_t1000` and `traj_pweb_s424242_t1000` ran the full horizon
and are **byte-identical to the `traj_v31bf` baselines** — the divergence headline is ZERO
divergence at t100/t500/t1000, both seeds (and the `traj_e2_*` family re-proves it at the
elections boundary — result line below when the chain lands). Film sweeps: three full sweeps
of near-one code exist for this night (`det_a`, `det_b`, `pweb_1280`) with every differing
frame classified (7 code-caused on the changed screen; 4 time-envelope members, each viewed
and named). The suite: the eight checks green at the Part 1 boundary; the second run rides the
elections boundary (below).

## RULINGS NEEDED (the morning's list)

1. **The spec:** `ELECTIONS_CAMPAIGN_SPEC.md` did not arrive with the queue and is nowhere on
   disk — re-paste it, and the blocked units (gap table, §7 types, §8/§26/§27 layers, §§20–22
   polling, the N/A column per R-EL7) unblock in order.
2. **Italy's allocation formula:** Rosatellum structure and thresholds are sourced; the
   PROPORTIONAL ALLOCATION FORMULA is not — rule whether the morning sources it (the Camera
   dossier / DPR 361 art. 83 consolidated text) before Italy joins the backtest.
3. **The USA "national election" framing** for the sim's shape (the presidential state table
   vs the House as the chamber) — tonight carried both; the model's choice is a ruling.
4. **Board 2b rides the §S paste** — the paste is yours (one gesture, rows 1 of 2 / 2 of 2).

## State (re-derived at each boundary)

- Queue arrival: HEAD `c6cd7d3` == origin/main; tree carried R-W1 + the census tool.
- Part 1 boundary: `3b85543` · `7959477` · `e30c82b` · `2439b41` · `104cc32` (E-0/E-1 rode the
  same push) — **pushed, origin/main == HEAD confirmed**.
- Elections boundary: `f524eeb` (the allocator + backtest) committed; **the R-N2 chain came
  back GREEN — dump exit 0, `traj_e2_*` ≡ `traj_v31bf_*` 6/6 by SHA-256, all eight checks
  exit 0.** Not one byte of the existing game moved all night, measured at both boundaries.
  The boundary commit (this report + the `Elections.meta` sibling file) and the R-SP1 push
  follow; the closing commit confirms origin == HEAD.
- **CLOSING STATE, confirmed: `6d2a67e` pushed, origin/main == HEAD** (`104cc32..6d2a67e`; the
  night's commits: `c6cd7d3` finding-3 records · `3b85543` R-W1+census · `7959477` Annex G ·
  `e30c82b` D7 · `2439b41` the package + Part 1 records · `104cc32` E-0/E-1 · `f524eeb`
  E-2/E-3 · `6d2a67e` this boundary). Tree clean but for this closing line and the memory
  update, which ride the closing commit. The morning starts at RULINGS NEEDED above; every
  [call] line in this log is strikeable.

## State (re-derived at each boundary)

- Queue arrival: HEAD `c6cd7d3` == origin/main; tree carries R-W1 + the census tool
  (uncommitted, by design — they commit with their proofs).
