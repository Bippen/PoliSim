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

## RULINGS NEEDED (accumulating)

*(none yet)*

## State (re-derived at each boundary)

- Queue arrival: HEAD `c6cd7d3` == origin/main; tree carries R-W1 + the census tool
  (uncommitted, by design — they commit with their proofs).
