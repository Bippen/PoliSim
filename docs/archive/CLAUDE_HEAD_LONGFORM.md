# PoliSim — the session-start head of `CLAUDE.md`, in full, as it stood on 2026-09-22

> Archived VERBATIM when the head was condensed (`COMPLETED.md` §579). Every rule here survives in `CLAUDE.md` in one to three lines with its § pointer;
> this is the long form each of those lines was cut from, kept so that nothing was deleted that had not first been migrated. Read by name, never auto-loaded.

# PoliSim

> ## START HERE — the reading order for a session's start (ruled 2026-09-17, `COMPLETED.md` §524 (c), built §525)
>
> 1. **Run the session brief:** `powershell -NoProfile -File Tools/session_brief.ps1`. It prints, read at the moment it runs: the branch against origin and anything uncommitted, whether Unity holds the project, the last commits, the last bars and their exits, the newest record sections with their line numbers, the residue the last bar printed, the errands still open, the trajectory sentinel's baseline, and the newest memory file.
> 2. **Read this file down to `## Genre & Scope`** - the standing rulings and notes. Below that line it is reference: search it by name, never read it through, never dump its heading outline.
> 3. **Read `# THE WORKING DISCIPLINE`** at the end of `POLISIM_FEATURE_LIST.md` - the rules every pass is bound by.
> 4. **Read the newest memory file, then the record sections the brief names** - each by its line number. `COMPLETED.md` is never read whole.
> 5. **Before every commit, `powershell -NoProfile -File Tools/bar_tier.ps1 -Staged`**, and run what it says the commit owes.
>
> The brief stores nothing and decides nothing: every line is read from where it lives, so it cannot go stale, and a source it cannot read it names. What the order replaces is §524 (c)'s measurement - sessions spending their first quarter hour and up to 1.2 MB re-deriving this from transcripts and the heading outline.

> ## ⚠ READ FIRST — what "N anomalies detected" actually means
>
> This file quotes anomaly counts in roughly a hundred places as evidence that a change was safe. **That
> number is a 5-field check, not a whole-simulation health measure**, and every one of those quotes
> should be read with this in mind.
>
> | Check | Coverage |
> |---|---|
> | `CheckFinite` (NaN / Infinity) | **29 of 29** `EconomyState` floats, plus Country-level fields — complete. No NaN can escape |
> | `CheckSwing` (>20% turn-over-turn) | **5 of 29** — GDP, Unemployment, Inflation, InterestRate, DebtToGdpRatio, and *only* these |
> | Range checks | **4 of 29** — GDP, Unemployment, Inflation, GovernmentDebt |
>
> Anomaly counts are overwhelmingly swing anomalies, and `CheckSwing` is structurally incapable of seeing
> the other 24 tracked values because `Snapshot` stores only those five. **A runaway in PovertyRate,
> Population, Consumption, Investment, CrimeIndex, LaborForceParticipationRate or 19 others produces zero
> swing anomalies and a clean-looking run.**
>
> So "0 anomalies" proves those five fields stayed within 20% turn-over-turn and nothing went non-finite
> anywhere. It does not prove the simulation is healthy, and it has historically been read as though it
> did.
>
> **Elias's decision (2026-08-01): leave coverage at five and state this plainly instead of extending it.**
> Extending would mean ~24 threshold choices plus a third baseline discontinuity in one day, and several
> fields (NetMigrationRate, PopulationGrowthRate) legitimately exceed 20%, so blanket coverage would bury
> real signal in noise. **Revisit if something ever slips through unnoticed.** Full analysis in the
> harness audit section below.
>
> Two further reading notes for anomaly counts anywhere in this file: they are **not comparable across
> the two baseline discontinuities of 2026-08-01** (the near-zero swing floor and the pre-epoch calendar
> fix), and before `f178263` a `-runmatrix` run silently ignored `-seed`, so any matrix count predating
> that commit came from an unseeded run.

> ## ⚠ EIGHT BASELINE DISCONTINUITIES — READ BEFORE COMPARING ANY TWO NUMBERS IN THIS FILE
>
> **Eight, within about a month.** A figure recorded on one side of any of these cannot be
> compared with a figure recorded on the other. When quoting a number from this file, check which era it
> came from first.
>
> | # | date | what changed | what it invalidates |
> |---|---|---|---|
> | 1 | 2026-08-01 | **near-zero swing floor** in the anomaly detector | every anomaly count recorded before it — the floor lowers counts against all historical figures |
> | 2 | 2026-08-01 | **pre-epoch calendar fix** (the frozen-calendar harness bug) | every run before it, which never advanced the calendar and so never exercised any date-driven system |
> | 3 | **2026-08-10** | **`DaysPerTurn` 121 → 365**, the 3.017x fiscal defect | **every trajectory, debt path, deficit, population figure and anomaly count ever recorded before it** |
> | 4 | **2026-08-17** | **the EROSION TERM** — the debt identity's missing −π·b arrives (rulings R1–R3; a RECALIBRATION BY CONSTRUCTION) | every debt path, debt-to-GDP figure, rating trajectory and divergence-signature number recorded before it — deliberately: the pre-erosion signature was the defect being closed. Non-debt figures are largely comparable (the term touches only the stock's drift) |
> | 5 | **2026-08-17** | **the MATURITY RATE-LAG** (ruling R4; recalibration by construction, MEASURED NARROW) | debt paths in RATE-MOVING regimes only — exactly {GovernmentDebt, Budget} moved of 38 dumped fields, ≤0.2 ratio-points at every baseline horizon except France's s777 overshoot window. Stable-rate figures are effectively comparable across it |
> | 6 | **2026-08-26** | **the FISCAL SEED RECALIBRATION** (build-order item 1; trajectory-moving by construction, named a recalibration throughout) — the five EU pairs re-anchored to one-basis real figures, the mandatory transfer block seeded, Sweden's UO10/11/12 flipped mandatory with its PotentialGDP re-solved (614.25) | **every EU-five trajectory, debt path, deficit, primary balance, FRF-multiplier reading and anomaly count recorded before it** — deliberately: the pre-recal era's +14..+22% year-1 primary surpluses (and the FRF crushed to 0.58–0.76 hiding them) were the defect being closed. USA figures are comparable across it — seeds untouched, T1 control-verified byte-identical. The post-recal era's baselines are `traj_postrecal_*` |
> | 7 | **2026-08-26** | **the TAYLOR GAP TERM** (build-order item 4, pass 4; a recalibration by construction — the rule reads the unemployment gap against NAIRU instead of the raw level output gap, which was a per-country constant of −14.5% for the USA and pinned its suggestion at the 0-floor) | **every USA and Eurozone trajectory recorded before it**: rate paths (USA ~0 → ~5–6%, the zone blend +0.8–1.0 pp), chair behaviour, inflation, GDP (USA −2.3% at t100, the level gap −14.3 → −16.6 by construction), debt-to-GDP (the denominator; USA debt interest is anchored and does not read the rate), approval, the house-price index (the pinned rate's +3.9%/yr runaway ends) — deliberately: the floor was the defect. Sweden/Poland RATES are byte-identical across it; their trajectories move ≤0.1% of GDP through the partner-rate channel. `PotentialGDP`, the demographics block and the crime/corruption stocks are byte-identical everywhere (14 of 42 dumped fields). The post-pass-4 era's baselines are `traj_post_pass4_*`; the pre side is `traj_pre_pass4_*` |
> | 8 | **2026-08-27** | **TARIFF COSTS** (pass 6, the queued shelf item; force-kind ON THE POLICY PATH ONLY — partners mirror an override's excess over the standing rate onto the overrider's exports, the change in the tariff take passes through to prices for one year with expectations looking through the part that printed, and the Trade bill's vote reads the change in the import-weighted average tariff, overrides included) | **every figure ever recorded under a partner override or a Trade bill**: pass 5's free-lever measurements (Sweden 33.8 → 24.7 and the partner damage — now 33.8 → 27.5 with GDP −5.9%, +11.1 pp of inflation for a year, and the bill failing at seed), the `tariffoverride` matrix cell (+2 anomalies at both horizons: Germany's own pass-through year), `TariffTakeDiagnostic`'s exploit section — deliberately: the free lever was the defect. **The no-policy trajectories are byte-identical across it (42 of 42 dumped fields, both seeds, all horizons — `post_pass6` ≡ `pre_pass6` ≡ `post_pass5b` 6/6)**; of the 30 matrix cells only the four carrying a tariff decision move (`stress` ×2 by the USA's +1 base-point years, ≤0.01 pp; `tariffoverride` ×2). The post-pass-6 era's baselines are `traj_post_pass6_*`; the pre side is `traj_pre_pass6_*` |
>
> ⚠ **Discontinuity 3 is the widest of the three.** The first two changed what was *measured*; this one
> changed what the simulation *was*. Every baseline captured before 2026-08-10 measured a fiscal engine
> charging a full year of spending, revenue and interest every 121 days, on a calendar where 100 turns
> was 33 years. Per-turn fiscal figures happen to be unchanged by the fix — the flows were always a
> year's worth and a turn is now a year — but **population, calendar span, election frequency and the SWF
> draw all moved**, so a post-fix run compared against a pre-fix number will agree on debt and disagree
> on everything demographic, which is the most misleading possible shape for a false match. See "A turn
> is now a year" below.
>
> There is also a fourth, narrower caveat that is not a discontinuity but bites the same way: before
> `f178263` a `-runmatrix` run silently ignored `-seed`, so any matrix count predating it came from an
> unseeded run and is not reproducible at all.

## Overview
PoliSim is a turn-based political/economic simulation game built in Unity (C#). The player governs a country — starting with six real-world-seeded countries (USA, Sweden, Germany, France, Italy, Poland) — and makes policy decisions (a portfolio of individual taxes, category-specific spending, tariffs, interest rates) each turn. The core of the economy (GDP, unemployment, inflation) is driven by named macroeconomic theory rather than tuned-by-feel curves; a handful of surrounding mechanics (approval rating, currency strength, trade/tariff dampening) are still intentionally simple heuristics, though approval is now itself a Phillips-curve-adjacent formula rather than an ad hoc one (see "Political Layer" below). The player must balance economic performance against public approval to stay in power — literally: they face re-election every `ElectionCycle` turns and lose (game over) if approval has fallen below `ElectionSystem.LosingThreshold`.

This is an early scaffold: core data model and a minimal simulation loop, plus a first functional (unstyled) play loop - not final game content or polished UI.

## Accounting Convention — the book in current prices (P5-B6, 2026-09-05; supersedes ruling R2 of 2026-08-17)

**The macro block is real; the book is nominal; one price level joins them.** `EconomyState.PriceLevel` is 1 at the seed and compounds every day at the inflation the Phillips curve prints (`MacroSystem.ApplyPriceLevelDaily`). The national-accounts fixed point (C+I+G+NX reverting to potential), potential output (P5-B7's labour × productivity), Okun and the Phillips curve run in CONSTANT PRICES as they always did; `EconomyState.GDP` is real GDP and is the constant-price view, the derived readout. The BOOK is in CURRENT PRICES: `EconomyState.NominalGdp` (= GDP × PriceLevel) is what the screens print as GDP and what every ratio divides by (`EconomyState.DebtToGdpRatio`, the shares of GDP, the debt bounds, the SWF cap); the tax bases are nominal (`TaxBases.Base` = the real base × PriceLevel, so revenue is rate × nominal base); the spending lines carry the year's prices as the ratio of the price level now to its level at the last index (`SimulationManager.IndexSpendingLines`, `Country.PriceLevelAtLastIndex`) beside their drivers - the term §314 withdrew, returned now that the book carries what it indexes to; the GDP-share flows (unemployment benefits, welfare, the SWF contribution, baseline government spending) read nominal GDP; the tariff take is the real take × PriceLevel at the period's planning; the debt accrues nominal balances and bears the nominal rate. **The erosion term (R2/R3, −π·b) is RETIRED**: it bridged a nominal stock to a real ledger, and a nominal ledger has no bridge to build - the debt-to-GDP ratio erodes through nominal GDP instead, which is the identity. Where the book meets the identity, G is deflated: `ApplyNationalAccountsDaily` receives the planned nominal spending divided by today's price level. The sourced GDP deflator (World Bank WDI NY.GDP.DEFL.KD.ZG, 2010–2024 means: USA 2.36, Sweden 2.43, Germany 2.42, France 1.55, Italy 1.70, Poland 3.27; Eurostat nama_10_gdp within a twentieth of a point) is the YARDSTICK the century-run diagnostic prints the model's mean inflation beside and the base year's definition (the seed's prices are the seed year's) - not a driver: the Phillips block produces the model's inflation. `PriceLevelDiagnostic` asserts the level compounds at the print, the bases are real × level, and no country's inflation reaches half the cap in a hundred years. Anyone comparing `Budget` or `GovernmentDebt` across the B6 commit is comparing a real figure with a nominal one - the fifth baseline discontinuity, and the trajectory diffs of §327 say so per country. **The rule the first dump of this book taught (§327): a nominal figure is compared with a nominal figure and a real one with a real one, never across** - the tariff pass-through once measured a real take against a nominal plan and read the price level as a tariff move every period, which ran every country into the net-creditor guard by year 450 through expectations and the wage bill. A quantity that lives on both sides (`FiscalPeriod.PlannedTariffRevenue` / `PlannedTariffRevenueReal`) carries both figures, named.

### The convention that stood before (R2, 2026-08-17; kept as history)


**The model's dollars are constant-price (real) units.** GDP is a volume identity (C+I+G+NX
reverting to potential); no price level scales any dollar quantity; all growth is real growth.
Inflation exists as a RATE (driving confidence, approval, wages, the Taylor rule's nominal
convention) — never as a deflator. **The one nominal quantity is `GovernmentDebt`**, as sovereign
debt is nominal in reality, bridged to the real ledger by the EROSION TERM at the stock update
(`ApplyRevenueAndSpending`): the real stock erodes at π per year, deflation grows it, and per
ruling R3 the erosion is SYMMETRIC — a net creditor's real claim erodes the same way (no free
money in either direction; the term shrinks whichever position exists toward zero). This is the
standard debt-dynamics identity's −π·b term, previously missing — see
`COMPLETED.md` §22 and commit `bcbba47` (the derivation — the stock-vs-flow mechanism report,
consumed there 2026-08-26) and "The erosion term" below (the build). Flows (interest bills, revenue, balances) remain in the single-unit convention they were
validated in; only the stock's drift carries the bridge. Anyone comparing debt figures across
the erosion commit is comparing across a RECALIBRATION BY CONSTRUCTION — the fourth baseline
discontinuity, listed with the other three at the top of this file.

## Numeric Inertness — a byte-inert instrument leaves the play path's expressions untouched (ruled standing 2026-09-08, `COMPLETED.md` §400, §402)

**Unity's Mono evaluates `float` intermediates at higher precision than `float`.** An expression such as `x = a * b * c; x += d * e * f;`
rounds to `float` when it is STORED, not at every operator; a refactor into locals - `float t1 = a * b * c; float t2 = d * e * f; x = t1 + t2;` -
rounds twice where the original rounded once. The two are the same arithmetic on paper and a different trajectory in the run: §400's first
Okun ledger did exactly this, and the 1000-turn dumps parted from their baseline in every field while both bars stayed green.

**The rule.** A measurement seam, a ledger, a probe hook - anything that must not change the simulation - leaves the play path's statements
as they are and recomputes what it wants for the subscriber alone, behind a null check. **A refactor into locals is a numeric change**, to be
treated as one: its own family, its own dump diff, its own explanation. **Inertness is proved by the dump diff against the baseline and never by
reading the code**: the diff is the only instrument that sees a one-ulp shift compound over a millennium.

**The IL-shape case (EN-7b, 2026-09-15, `COMPLETED.md` §500, §501): inertness is about the shape of the store, not only about locals.** EN-7b gave
`ApplyRevenueAndSpending` a flow and meant to keep the old expression for the zero case to the bit - first as a ternary over the two expressions,
then as the old statement followed by a branch that overrode it. Both moved the no-policy budget by a float's last digit, and so did that branch
reduced to `if (false)`: **a dead block writing the same local a second time moved the trajectory** (`sentinel_en7b_g5r` red; the same tree with only
that block deleted, `sentinel_en7b_g5`, green). What held is one expression with the flow always summed - one store, and `x + 0` exact at any
precision. A guard written *so that* the zero case is the old arithmetic is not inert by construction; the sentinel and the dump are the proof.

This sits beside the accounting convention above because both are rules about what the model's numbers ARE, not about what they should be:
one book in current prices, one price level joining it to the real block; one arithmetic in the play path, untouched by the instruments that read it.

## The preview's clone is audited by field, not by memory (2026-09-15, `COMPLETED.md` §506)

**`SimulationManager.ClonePreviewCountry` is a hand-list, and a hand-list drifts.** The class (R4-1) was paid for at C-N4, §391/§398, §497, EN-7b and Q1, each
time found by a figure that happened to read the dropped field. T-3's condition - *probe the preview path before it lands* - found the list short by
twenty-seven value fields at once, and the preview wrong in two more ways (its identity handed the plan nominal; the year never committed to the clone's
pyramid). `PreviewParityDiagnostic` now MARKS every value field of `Country` on a copy and reads each mark back through the clone, so a field added to
`Country` and not to the list fails the simulation bar by name the day it lands - a field equal to its default by coincidence cannot hide; an exemption
carries its reason in the check. Beside it, the preview's identity G is held to the boundary's with a line raised. **Add the field to the list; never to the exemptions without a reason a preview could not contradict.**

## A session's context is not a binary transport (ruled standing 2026-09-09, `COMPLETED.md` §424, §427)

**Bytes that pass through a model's context come out changed, and they come out changed quietly.** §424 carried
fifteen delivered PNGs out of the design project the only way a session can - read the base64, write it back - and
**four of the first eight arrived corrupt**. Every one of them still had a valid PNG signature, the right dimensions
in its header and a plausible size; the damage was inside the compressed image data, where nothing that merely looks
at a file would find it.

**The rule.** A binary artifact reaches this repo as a FILE - a delivered pack that is imported, a copy someone makes
on disk - never by being retyped through a conversation. That is not a preference: `DeliveredAssetCheck` reads
deliveries out of their zips precisely because the pack is the unit that can be verified, and every asset this project
holds arrived that way. A session may name what it needs, print the mapping, and record the errand; it may not be the
courier.

**What to do when a binary must be checked.** Walk its own structure and recompute its own checksums - for a PNG:
the signature, then every chunk's CRC32, then that it ends at `IEND` with no trailing bytes (`Tools/pngcheck.pl` is that walk, and it is the tool the rule points at). ⚠ **A file that DECODES is not a file that is INTACT.** Size, dimensions and a successful
header read are all satisfied by a corrupt file; the CRC is the only thing that is not.

**Small text is a different case and stays allowed** - a JSON table, a markdown row, a log line. Text damage is
legible where binary damage is not, and the standing habit for anything that carries figures is stronger anyway:
generate it, do not transcribe it (§419).

## Bars are tiered by what a commit touches, and a UI item iterates on the dry film (2026-09-17, `COMPLETED.md` §524)

**The tiers are the working discipline's rule 1** (`POLISIM_FEATURE_LIST.md`); `Tools/bar_tier.ps1` prints the tiers a commit touches and what it owes. Per-item bars had crept back into running the full trajectory dump twice per BASELINE item and a film per country per layout cut, while the closing-gate rule said once per track; §524 is the measurement.

**The dry film.** `UiScreenshotCapture.RunDry` runs the film's own driver and choreography in `-batchmode` play mode, one play session per country per geometry, all in one Unity process:

```
Unity.exe -batchmode -projectPath <proj> -executeMethod PoliSim.EditorTools.UiScreenshotCapture.RunDry -skipsimulationtestrunner
          -shotlabel=<label> -shotcountries=Sweden,Italy -shotgeometries=1280x720,2560x1440 [-shotstop=<capture>] -logFile <log>
```

- **Why no window is needed.** GUILayout computes every rect on the CPU in the Layout event and hands it back at Repaint, where `UiOverflowGuard` and `UiContainmentGuard` already measure. A Game View was needed only to deliver OnGUI's events. `DryGuiPass` delivers them itself through the IMGUIContainer entry points (`GUIUtility.BeginContainer`, `GUILayoutUtility.BeginContainer`, the controller's `OnGUI`, `GUILayoutUtility.LayoutFromContainer`), once a frame and once at each capture, with the game skin and the mouse off the screen.
- ⚠ **Those are Unity internals, reached by reflection.** `DryGuiPass.Prepare` names a member it cannot find, and a pass whose own machinery throws stops the run failed after one line - the first form, which reset the GUI state outside the container, wrote 1.5 GB of one exception in ten minutes.
- **The size seam.** A `-batchmode` Editor reports a 640x480 screen and every style here scales with the height, so the UI reads `UiScreen.Width` / `UiScreen.Height`: the real screen in play and in films, and in a dry film the frame a film of that geometry captures (the height less the Game View's toolbar). ⚠ **The dry film never takes the seam's other branch.** The seam's first form read itself there - a stack overflow no dry film could reach and the one film at the item's end found at once. That is the standing reason a UI item still films one width.
- **The label table.** `GUIStyle.onDraw` sees every styled draw, so both kinds of film write `<shotdir>/labels/<label>_labels.tsv`: each text draw of a captured frame with its rect, type size, what its text needs, and whether it straddles or leaves its clip. It is filtered to the game skin - a film's hook otherwise also records the Editor's own chrome (the Game View toolbar, the Hierarchy). It reports and never judges: the verdict stays the guards'. A film and a dry film of one tree wrote it byte-identically (§524).
- ⚠ **What it does not claim.** `ScreenEdgeCheck`, the capture-identity token and the frame-size traps read pixels, and a dry film has none.
- **Every dry capture logs its IMGUI pass time** (`SHOT: dry pass for <capture> - the IMGUI pass took N ms, its Layout event M ms, the chamber cache's drift verification K ms of it`). It found France's Budget frames at 2–4 s a pass against Sweden's tens of milliseconds, in films as much as in dry films: the tax screen asked the parliament about every line on every IMGUI event (PF-1, §524). **Fixed at §525:** the screens ask `ChamberVerdicts`, which answers once per chamber and question. ⚠ **A film or dry film re-verifies every cached answer against the model**, so France's and Italy's Budget passes still read hundreds of milliseconds under the harness: subtract the verification before reading a pass time as the player's. A `CHAMBER CACHE DRIFT` line is a defect in the cache's key, never noise.
- **The launch after a batch step can quit in seconds** (exit 0, the log ending after package resolution, no `SHOT:` line) when the step stopped the licensing client and the Hub is respawning it; re-run it. A film's verdict is `SHOT: done, N captured`, never its exit code.
- ⚠ **Never run `RunDry` without `-batchmode`** - a Game View would deliver OnGUI as well and every frame would be laid out twice; it refuses.

## The bars read the tree once and run the no-policy centuries once (2026-09-17, `COMPLETED.md` §525)

- **A check that reads source reads it through `SourceText`** (`ReadBytes`, `Read`, `ReadLines`, `ReadWithoutComments`). `CheckSuite` opens a per-process cache around each group, so one read per file serves every check; each ask re-stats the file, and outside a group every form is a plain disk read. `CommentImmunityCheck`'s census of text readers counts `SourceText.Read`, `SourceText.ReadLines` and `SourceText.ReadWithoutComments` as well as raw `File.ReadAllText` and `File.ReadAllLines`. Each group logs `SOURCE: N file read(s) from the disk and M served from the source cache`.
- **`DeadStateCheck` tallies every declared name in one pass over the corpus.** What counts as a read is decided in one place, `IsRead`, with the old per-occurrence rules.
- **A diagnostic that needs a no-policy century reads `NoPolicyCentury.For(country)`** - seed 777, the default world, no decision for anyone, a hundred years, every year's readouts recorded, run once per process. A reader that needs a readout the record does not carry adds the field there, not a private loop. ⚠ `InfrastructureReadoutDiagnostic` keeps an independent run of Sweden's first twenty years and fails when the shared run's year twenty differs by a bit: a planted defect in the shared loop printed every century figure identically at two decimals, and only that comparison caught it.

## Playtest 6's standing notes (2026-09-17, `COMPLETED.md` §526–§536)

- **A control that draws as a bare sentence gets the chrome's face** through `CanvasChrome.FacedButton` - brass commits, paper navigates (6b's split). A form Design drew (the desk chips, the rail's tongues, the laws board's underlined words) is not given a face by a pass; that is a D21 row. ⚠ Write asset names as literals: a name built as `stem + "_hover"` made the D18 inventory report delivered assets as unreached.
- **A glyph in a slot is drawn through the label style, never the tab style, and `LedgerRow.CellStyle` keys its cache on the source as well as its size** (§536). The tab style is Unity's default button underneath: its face survived nulling all eight state backgrounds on a copy, and it carries the strip's fixed height; through the cell cache, keyed on the font size alone, a second source at the same size inherited the first's clone (the Desk's verdict cell at 2560, found by the second filmed width). The rail's missing-icon fallback is `DrawRailInitial`: the label style with the tab's font, fitted to the slot.


- **A digest certifies sameness, not sanity** (FT-10, §541). `TrajectorySentinelCheck` also runs a CENTURY at seed 777 and holds every country's energy prices over its own price level to a LEVEL and a TAIL measure (1 % for a clean country); FT-10's cells are recorded with whole-point ceilings under `EnergyBreachCeiling`, each with its CAUSE printed on its line - a fix lowers them in its own commit, nothing raises them, and a cell that closes is DELETED, never re-cut (thirteen before P-A, eleven after it, SIX after P-B). **FT-10's families, 2026-09-21:** P-A LANDED (§545, `traj_p6pa`) - the scale reads the line's move as a SHARE of its own path times K (`EnvironmentSeeds.EnergySupportLineToLevySeed`, captured at the seed, asserted live by `EnergyLayerCheck`); P-B LANDED (§552, `traj_p6pb`) - Sweden's water value is kept IN THE SEED'S PRICES, each neighbour's clearing divided by the price index it was cleared at (`EnergyMarket.Result.PriceIndex`, stamped on every path of the market's clearing; an unstamped clearing THROWS); **P-A′ is BUILT AND HELD (§553)** - the support share has no single sourced test yet, three rulings asked, the work kept under `PoliSim-captures/held/`. ⚠ **P-A changes AI-GOVERNED books only:** `IndexSpendingLines` gives real growth to a ministry's driverless lines, never to the player's, so for the played country the new form IS the old arithmetic and the page's *one for one* is exactly true (`EnergyLedgerDiagnostic` 3b). ⚠ **A probe that scales every country alike cannot see a cross-book defect** (§552's second reader): a price level doubled everywhere cancelled in the water value's old form too, so the two B6 probes double Poland's TWICE. ⚠ **Write a family's per-country sentences AFTER the dump, never before it** (§552's first reader found three false ones written from expectation), and **a sourced FIGURE is not a sourced TEST** (§553: four shares, each from its own primary document, read under two different definitions of what was being counted - the review caught it, the family did not land). A family's *"cannot move"* is a claim about a country's OWN mechanism: the euro's one rate and the trade links move every other row from turn 3.
- **An end-named dial's caption band holds some sixty characters at 1280, and an empty band is now told to the guard** (§542). The catalog check's ninety-six is for a dial without end-names. The Energy tab's four dials are the Energy sector's own - one draft, one bill, no sixth control (S9); both dial checks read every `GameController*.cs`.
- ⚠ **The game holds several worlds at once - the played one, the shadow baseline's, the impact ledger's forks - and a process-wide static keyed by a country's id is shared by all of them** (§544: §539's fleet table was zeroed by the shadow's turn after every real one, and no edit-mode check could see it). State a reader needs belongs on the Country; where the readers are keyed by id and run deep, a scope names the country (`EnergyFleet.For`, opened by `EnergyMarket.Clear(Country)`, `EnergyLedger.Compute`, the page), no scope reads the record, and `EnergyFleet.RecordOnly()` forces it for the seed calibrations. Orders land by TURN at the top of the boundary (the calendar's year drifts across the 365-day boundary). The market's turn state (Sweden's water value) is still the process's: the controller sets the played world's again after the shadow's turn and on load.
- ⚠ **A money path gets its review, and since §546 THE BAR SAYS SO** (§524's rule; §539 had none and §544's found that defect). `ReviewLedgerCheck`, in the cheap bar, fails where a money-path file stands in a state no row of `Tools/review_ledger.tsv` covers, where a commit since the guard left such a state, or where the sentinel's digest has no reviewed row. **The order of work on a money path:** finish the change, run the review (without Elias's word for a workflow: one independent read-only reader on the change and one on the rework), commit the reports VERBATIM under `Reviews/` with a header saying what changed after the last reader, add the rows with `Tools/review_row.ps1` (the file's final state, and both digests for a family), lower `GrandfatheredCeiling` if a grandfathered file moved - THEN bar. There is no waiver row; a comment-only edit asks for its row like any other. The cheap bar reads 44 of 44 since §556 (2026-09-21), when Design's energy icon landed.
- **A range caption between two end-names hangs on THEIR line** (§550, Design's sighting): same band, same line - its top is the band's top lifted by the difference of the two faces' ascents (`LedgerRow.Ascent`), and the caption guard's band now covers the end-names' lane as drawn (`LedgerRow.LastEndNameLeftInk` / `RightInk`), proved failing first on the case that was sighted. A dial row on a PAGE takes the page's name face and lane (`nameFace` on `LedgerRow.Draw` and `DrawDialRow`): one name, one pitch, on one page.
- **Tooling lessons, 2026-09-21:** the Design project's files are reachable from the MAIN session only (a subagent has no DesignSync - fetch first, hand it the file); a long Bash heredoc holding apostrophes, backticks or `\\n` inside quoted C# fails or silently rewrites the escape - write a long perl patch or record with the Write tool; never trust a session summary's *"the user said"* for a standing rule - go to the transcript (the commit attribution line, §555).
- ⚠ **A check that loads a save into a MANAGER has not loaded it into the GAME** (§557). The play protocol's staged save threw in `GameController.RestoreUiDrafts` from before the day it was first cut - one unguarded read of a UI layer a batch-written save does not carry - and the game said *Load FAILED* while `PlayProtocolCheck` said it loads. The check now adopts the save through `GameController.RestoreFromSave` on a controller in edit mode. A save cut in batch must also SEAT THE PLAYER'S PARTY (§558): the game seats one at selection, a batch cut does not, and a player with no party has no run-up and is refused every campaign verb.
- **RECORDS: ONE COMMIT PER PASS, AND THE SHORT FORM** (§574, measured): a § record is **what changed · the evidence line · the commit**, and anything longer has to earn its lines. The sitting pass of 2026-09-22 spent 26.6 minutes of model time and 2.8 minutes of document bars on seven per-item records; one records commit at a pass's close pays ONE document bar (23 s) instead of seven. The per-item COMMIT MESSAGE carries the item - it is the thing a reader bisects to - and the § record carries the pass. **A report is one screen**: the table, what changed, what is open. A record that repeats what the commit message already said is the model's own time being spent twice.
- **THE LOOP** (§573, measured): edit → `dotnet build` on the generated csproj pair (≈5 s, no Unity) → a TARGETED check - `CheckSuite.RunNamedBatch -checks=A,B,C` (25 s) for a guard, `MultiRun.Run -methods=Type.Method,…` (79 s for three) for diagnostics and the sentinel - then edit again. At the item's close, CHEAPEST-FAILING FIRST: cheap bar (45 s) → the dry film scoped to the countries and widths THE ITEM NAMES (145 s for two, against 522 s for twelve) → one filmed width (60 s) → the simulation bar (13 min) where the tier asks it. ⚠ A Unity launch costs 23 s before it does anything (1.2 + 5.2 s of domain reload, 2.8 s of compilation bookkeeping, and 8-17 s more after a source edit), which is why several methods ride ONE launch - and why the sentinel was proved identical alone and batched before anything relied on it.
- **A number's locale is the SURFACE's, not the call site's** (§568): `UiCulture.Install()` sets the UI thread to the machine's culture with the INVARIANT number format - so every interpolated figure reads with a point - while DATES keep the machine's culture, because the calendar sheet is drawn from them (board 1k). 195 interpolated sites are left alone on purpose; `NumberLocaleCheck` holds the install to one runtime site. **The stamp has one face** (`PoliSimWidgets.StampSize(text, style)` / `Stamp(rect, text, style, ink)`), **a call to action wears brass to commit and paper to navigate, both untinted, at `CtaWidth()`**, and **a headline reading is one cell** (`DrawReadingCell`) on the desk's foot, the Budget's header and the Statistics head - which supersedes board 1m-r2's no-plates line for the desk's foot.
- **A NAME carries its diacritics; a STEM is ASCII** (§566): the party table holds the published spellings (restored from the ElectionsData sources, refusing any the source does not carry), and every asset stem derived from an abbreviation folds the combining marks out first - `D18MarkAssignment` and `D18Inventory` both slug that way, and both went red the hour the abbreviation `Grüne` arrived. **The six COUNTRY inks are a registered channel** (`UiPalette.CountryInks`) with `CountryInkFenceCheck` running D16's fence inside it; a country ink is never drawn beside an area accent, which is why the fence binds within the channel and not across.
- **IMGUI hangs a slider's thumb from its rect's TOP** (§565): a knob taller than its track overhangs downward, into whatever the row keeps under it. `LedgerRow` centres the knob by painting the sprite through the thumb style's `overflow` while the thumb's RECT stays the track's height - paint moves, the control's geometry does not - and the caption band starts below the knob's foot. **The DOCUMENT face (Courier Prime) carries no `→` and no `▸`**, and the body serif (Pagella) carries both: an arrow in a desk caption or a row's trailing is a fallback font's glyph, not the desk's. **`MetaTextCheck` now also reads identifiers in prose, `HUE:` tokens, the ASCII arrow, a central bank named by literal and instruction prose** - with interpolation holes blanked, diagnostics skipped, and two allow-tables (by literal, and by the source line around it). **And a KEY is ASCII while a SHORT NAME is the authority's** (§575): `PoliticalParty.Abbrev` is the persisted key - saves, fixtures, seat tables, ink and mark tables, asset stems - and `PoliticalParty.ShortName` is what a player reads (the Bundeswahlleiter's `GRÜNE`), defaulting to the key. A site that DRAWS an abbreviation takes the short name; every lookup keeps the key, and `PartySystems.ShortName(id, abbrev)` serves the sites that hold only a key.
- ⚠ **A DRY FILM CANNOT CLAIM CANVAS TEXT - A CANVAS SURFACE OWES A REAL FILM** (ruled 2026-09-22, §578): a dry film runs without a window, so the real backbuffer is 640x480 whatever geometry the run names, and the canvas-text guard says it asserted nothing. Between 2026-08-28 and 2026-09-22 every film of the Canvas surfaces was dry or stopped before them, and the signing screen's stance captions clipped unseen for that whole time (PF-13). **A UI commit that touches a Canvas surface owes a REAL film that REACHES it** - `Tools/bar_tier.ps1` names the surface and its shots - **and at a track's close every Canvas surface is filmed real at 1280 AND 2560.** The Canvas surfaces are `SigningScreen`, `ElectionNightScreen`, `CountrySelectorScreen` (the selector and both party pickers) and `CanvasChrome`, which is the furniture the others are built from.
- ⚠ **TOOLING A PASS DEPENDS ON LIVES UNDER `Tools/` AND IS COMMITTED** (standing rule, ruled 2026-09-22, §576): a script the loop uses is part of the loop, not a note. §574 wrote the warm Editor's client into the SESSION'S SCRATCHPAD; the session ended mid-bar and took it, while the host it had started kept running and answering with nothing on disk able to talk to it. The scratchpad is for intermediate results a pass throws away - patch scripts, parsed figures, a working copy - and anything a LATER pass would have to rewrite belongs in `Tools/`, in the commit that first needed it.
- **A WARM EDITOR SERVES THE RUN, NOT THE EDIT** (§575, measured): `Tools/warm.ps1 -Start` then `-Command "bar all"` / `"bar sim"` runs a bar in a host that survives it (`CheckSuite.RunTableNamed`; the batch entries end with `EditorApplication.Exit`, which `CheckExit.Collect` cannot suppress - the host's first real use ran the cheap bar and DIED of it). Host up 20-25 s, cheap bar 25-35 s, simulation bar 601 s, both from one launch. ⚠ **It does not recompile**: after a source edit, `-Stop` and `-Start` again, and stop it before any film - a film needs the project.
- **A call to action is a sentence and the family's one-width button** (`DrawSentenceAction`, §564): every Introduce / Create / Dissolve / Set override on a ledger page is that primitive, which lays its sentence out WRAPPED on the row's two lines at full size before it lets `MeasuredLabel` shrink it - `MeasuredLabel` measures unwrapped and shrinks to one line, so a long sentence handed to it alone lands at the 8 px floor. A dial SET BY LAW is `LedgerRow.Draw(…, knob: false)` - the track, the ticks and the end-names with no knob and no control, not a disabled knob (that says "you could move this, and now cannot"). **The film harness scrolls to a row by its recorded geometry** (`RowTop(" / <row name>")` off `LedgerRow.GeometryByRow`, interactive rows only - a read-only row is not recorded, so ask for the interactive row it sits under), never by a fixed offset: three offsets were the heights of blocks §564 moved.
- ⚠ **The live game's campaign calendar is the next election turn's boundary, not a country's own dates** (`SimulationManager.CurrentCampaignCalendar`; §558, CL-5, Elias's to rule). `CampaignCalendar.Sweden2026` is read by the staged campaign film, the protocol's staging and its check - never by play. Anything that says *"Sweden's 2026 election"* about a played game is describing a game this build does not run.
- **A range caption fits in what is clear of the end-names AS DRAWN** (§559, PF-8): the guard learned the measured ink at §550 and the placement did not until a film reached the state pins. The pins (`-shotstates`) sit past where a partial film stops, so NO BAR RUNS THEM - they had gone stale unseen (PF-12), and what they show (PF-9, PF-10) had never been on a guarded frame.
