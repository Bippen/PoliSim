# K299 review: the US population path recalibrated on CBO's January 2025 Demographic Outlook (`wave/uspop` at 593c9e4e)

Reviewer: independent adversarial review, read-only, 2026-10-29. Diff reviewed: `git diff cf6bd007..wave/uspop` (86bee07c the prereg alone, edc776bc the build, ea5f24d6 the fetch log, 593c9e4e the catalog generator's note), read line by line. main's code equals cf6bd007's (553aadb8 adds only a prereg and RULINGS lines), so main's tree served as the base for every before/after run. Unity was not run.

Files reviewed: .gitattributes, Assets/Editor/CohortAgingStepDiagnostic.cs, Assets/Editor/GeneratedCatalogCheck.cs, Assets/Editor/NoPolicyCentury.cs, Assets/Editor/ParticipationCohortDiagnostic.cs, Assets/Editor/PopulationProjectionCatalogGenerator.cs, Assets/Scripts/Data/Generated/UsDemographicOutlook.cs, Assets/Scripts/Data/Generated/UsDemographicOutlook.cs.meta, Assets/Scripts/Persistence/SaveGameService.cs, Assets/Scripts/Simulation/CohortDemographics.cs, ElectionsData/usa/raw/uspop_cbo_demographics/SHA256SUMS.txt, ElectionsData/usa/raw/uspop_cbo_demographics/fetch_log.md, ElectionsData/usa/raw/uspop_cbo_demographics/wayback_20250811192155_cbo_60875-Data.xlsx, ElectionsData/usa/raw/uspop_cbo_demographics/wayback_20250811192155_cbo_60875-Data.xlsx.headers.txt, Reviews/2026-10-29_uspop_prereg.md, Tools/uspop_prep.pl. Also read for context: Assets/Scripts/Data/PopulationCohorts.cs (RebasedTarget, AnchoredNext), Assets/Scripts/Data/Generated/PopulationProjections.cs (For, the USA row), Assets/Scripts/Data/PopulationPyramids.cs, Assets/Scripts/Data/PotentialOutput.cs (Population15Plus), Assets/Scripts/Elections/WorldClock.cs (StartDate), docs/RULINGS_2026-10-23.md K276 and K290, COMPLETED.md §950, Reviews/2026-10-29_k276_review3.md and _integrator.md, Tools/bar_tier.ps1's money pattern.

## What I verified, and whether I reproduce the implementer's numbers

- **Gate.** `bash Tools/cgate/gate.sh G:/UNITY/Projects/PoliSim-wt/uspop`: GATE OK.
- **The generator.** `perl Tools/uspop_prep.pl --check`: "is what this tool writes", exit 0. `--dry` prints Figure 10 against the LTBO within 0.0164 % (worst 2047) and Figure 2's total equal to the LTBO's population at 61 shared years.
- **Bytes.** `sha256sum` of the xlsx is f5dc9842...084af, the SHA256SUMS line, the generated `SourceDigest[0]` and the header's line. The LTBO file is 8affca39...82095 in q29's SHA256SUMS and in `SourceDigest[1]`. Content-Length 4510541 equals x-archive-orig-content-length and the fetch log's byte count; the capture is the `id_` (raw) form, Last-Modified 9 May 2025, which agrees with the Contents sheet's "reposted May 2, 2025".
- **Hand check of cells,** with my own unzip-and-read script, not the tool's reader. Figure 2 row 74 (2040): 97.546 / 148.07 / 42.475 / 79.859 / 367.95; row 59 (2025) 105.963 / 137.451 / 42.507 / 63.886 / 349.806; row 89 (2055) 93.099 / 143.877 / 47.997 / 87.066 / 372.039. Figure 10 2040: 35.333 / 143.481 / 42.007 / 75.553 / 296.374. Each equals the generated array at index year-1975 (all ten arrays are 81 long, 1975 at index 0). The headers are "24 or younger | 25 to 54 | 55 to 64 | 65 or older | Total" and "16 to 24 | 25 to 54 | 55 to 64 | 65 or older | Total", as the tool asserts.
- **The LTBO cross-checks, recomputed independently.** Figure 2's total equals the LTBO's "Population (millions of people)" row exactly (worst difference 0.0000 million, 1995-2055). The LTBO's actual labour force compounded from "Labor force growth", over its rate index, against Figure 10's total index: +0.0039 % (2030), -0.0046 % (2040), -0.0164 % (2047, the worst), -0.0078 % (2055). Both hold. The total check pins the sheet and the year column; the exact header match pins the group columns, which the total alone could not (two swapped groups sum the same). CBO's potential labour force over its actual is +0.98 % at 2040 (the prereg's "+0.99 %": rounding).
- **No typed numbers** in the generated table; every figure is read from the xlsx. Typed in code, all declared or [AUTHORED] and named in the prereg: the age 25 (`UsRecalibratedFromAge`, which duplicates `GroupAges[1][0]`), the years 2030/2040/2055 and base 2025, the bands 0.01 and 0.015, and the 0.8 of 15-19.
- **Headless, reproduced.** `bash Tools/headless_hold/anchors.sh` on the worktree: USA (c) 2030 -1.80 %, **2040 -2.69 %**, **2055 -2.89 %**; LONG-RUN ANCHORS 0 FAIL, 0 REGISTERED MISS; PARTICIPATION COHORTS PASS; every (d) line as in the implementer's run. My output is byte-identical (`cmp`) to the implementer's final run left in the session scratchpad (`anchors_final.txt`). My main run is byte-identical to its `anchors_before.txt`. On main: 2030 -2.26 %, 2040 -3.43 % (REGISTERED MISS K290, as K276's integrator recorded in the engine), 2055 -2.98 % (review 3's figure). The implementer's first run of the tree (`anchors_after.txt`) differs from the final code's output only in the three (c) info lines' K299 addendum, so the only code change after the first measurement is information text.
- **The USA's LongRunAnchors windows move inside their bands:** (d1) 2024-2031 2.04 -> 2.16 % against the WEO's 2.11; (d2) 2032-41 1.60 -> 1.63 against CBO's 1.71; 2042-51 1.51 -> 1.46 against 1.46. The output gap at year 1 widens from -1.16 % to -1.57 % (see L1).
- **The EU five, byte-identical.** In code, `CohortDemographics.Projection` returns `PopulationProjections.For` unchanged for every country but the USA (CohortDemographics.cs:96-100). The other edits are read-only additions (a pyramid copy and the substrate year on the no-policy record, the (d) print, two catalog lines) or the USA-only path. In output, every Sweden/Germany/France/Italy/Poland line of the headless run, 60 lines each, hashes identically on main and on the branch; the whole file differs only in USA lines and the summary lines. The no-policy worlds hold all six countries, so an identical EU print also says no cross-country channel moved these lines.
- **`hold.sh` (DebtPathHoldDiagnostic, out of the engine), on main and on the branch:** `DEBT HOLD: PASS` with the same 2 REGISTERED MISS lines (Sweden h2, the K218 pair) on both. Only US lines differ. The USA opens the hold at 124.8 % in 2026 (main 125.6 %: two years of CBO's faster 25+ growth). US (h3) 2026-2036 is +10.9 points against CBO's +19.6 (main +12.5; band +/-10). US 2026-2056 is +50.5 against +74.5 (main +51.3; band +/-25), so it is **24.0 of its 25 points** off, against 23.2 on main (see L6).
- **K290's lift by its own rule.** I copied the branch's Assets to the scratchpad, put the K290 entry back (USA 2040, -0.0334) and ran anchors.sh. The line prints `ok (c) USA 2040 ... (-2.69%) ... - REGISTERED under K290 and holding: the registration can be lifted`. The implementer's own pre-removal run (`anchors_after.txt`, 18:54 local) prints the same line. Removing the entry changes nothing else: `Band()` takes `Check(ok)` for an unregistered key, and `ok` does not read the registry. The mechanism is intact: the dictionary is empty (ParticipationCohortDiagnostic.cs:92), `Band`, `RegisteredPopulationTolerance` and the REGISTERED MISS summary are untouched.
- **Nothing else reads the old path.** `PopulationProjections.For` is read only by `CohortDemographics` (now through `Projection`, and inside `BuildUsPath`), `CohortAgingStepDiagnostic` (now `Projection`), `GeneratedCatalogCheck` (its digests) and the catalog generator. Nothing under Tools/ reads it. Every runtime population reader reads `Country.Cohorts` or `State.Population`, which are stepped on `Target`.
- **Double and float.** `BuildUsPath` sums the Census group in double, forms the factor in double (float CBO figure over double) and stores `(float)(census[b] * factor)` once. IEEE double on x64 is the same under Mono and .NET, so the stored path is runtime-independent, as the comment says. `RebasedTarget` stays float, as for the EU five. `Projection` returns the cached array by reference, the same convention as `PopulationProjections.For`. No caller writes into it (`CohortAgingStepDiagnostic` clones before stepping). The lazy cache is unlocked, but any racing build produces the same arrays.
- **The prereg was committed alone, before code.** 86bee07c holds only the prereg (18:52:02 +0100). Its log has no later edit. The scratchpad shows the source-only target computations it discloses (`recal.pl` 18:38, the uniform factor; `recal2.pl` 18:42, the declared treatment). The first run of the K299 tree (`anchors_after.txt`) completed at 18:54:46, after the commit. (c)'s band and years are K276's, unchanged. (d)'s bands were fixed in the prereg, but with the source-only targets in hand (see L4).

## The hunt list, answered

1. **Sourcing.** Byte-exact. The tool reads Figure 2 and Figure 10 correctly (hand check above), and both LTBO cross-checks hold as claimed. No typed figures. **A2:** see M3. One more attempt round on the Internet Archive (2026-10-29 18:12-18:27Z) did not establish a by-age table: the CDX prefix queries over `cbo.gov/system/files/2025-01/` and `.../60875-` answered 504 and 503. The capture fetches of the publication page answered 429 twice. But the CDX exact query for the page itself answered 200, and it changes the picture.
2. **The mechanics.** They are as described: band = Census band x CBO group / Census group from age 25, 0-24 on the Census path, the factors held past 2055 (CohortDemographics.cs:117-141). I recomputed the rebased target out of the engine. It reproduces the run's (d) to within 0.01-0.06 %, the gap being the health term's excess deaths. The pyramid stays consistent in level: every band is continuous and every group's sum is CBO's. The anchored step follows the target band by band, so no flow is generated that the target does not imply. What the target implies is in L2: a step in implied migration at 25, 55 and 65, and a rate kink at 2055. Births stay Census's, because band 0 is unscaled. Deaths are the seed's or the health family's. Migration is the residual, so it now carries CBO's 25+ growth. The total population runs faster than CBO's because 0-24 is not scaled: +0.51 % at 2040 and +0.89 % at 2055, against -0.25 % and +0.53 % on main. The cause is 0-14 left on Census's higher fertility (M1). GDP per capita is lower by that much against a CBO-consistent total, and the school-age bands and the child-facing lines read Census's children. Voter groups from 18 are unaffected through about 2043, since 18-24 are all alive at the base until then. The faster GDP lowers the US debt ratio (L6).
3. **The prereg.** The bands were fixed before measurement (above). The disclosure is honest, and the scratchpad bears it out. But the allocation argument is justified only for the part of 0-24 that was alive at CBO's base (M1).
4. **K290's removal.** It lifted by its own rule before the entry went (above). The mechanism is kept.
5. **USA 2055's -2.89 %.** It is fragile in both directions, and its sign of fragility depends on two choices the item does not own: M1's 0-24 declaration (about -0.4 to -0.5 point against it) and M2's year alignment (about +0.6 point for it). Within the build as registered, 0.11 % of the labour-force index is about 0.07 participation points. The labour-tax term alone already holds the US rate down 0.50 points at 2055, so a later change to US creep, unemployment or any cohort's rate can tip it. The headless-to-engine difference has been within the printed tenth (K218; K276's 2040 line was -3.43 % in both), which is smaller than 0.11. So the engine run should agree; watch it.
6. **Baselines.** See L1 for which US readers move and when. The EU five do not move in code.
7. **CRLF, `.gitattributes`, digests.** See L5. GeneratedCatalogCheck's new loop (GeneratedCatalogCheck.cs:470-488) re-derives both source digests from disk against `UsDemographicOutlook.SourceDigest`, by path, with the drift message the other generated tables use. Both digests match the files.

## Findings

### High

None.

### Medium

**M1. The declaration that 0-24 stays on the Census path is justified only for the people alive at CBO's base. For everyone born after it (0-14 by 2040, all of 0-24 from about 2050), the argument's own logic says CBO's group applies. Under that reading, USA (c) 2055 falls out of the band.**
- **Where:** CohortDemographics.cs:130 (`if (ages[g][0] < UsRecalibratedFromAge) { continue; }`) and the doc at :90-93; Reviews/2026-10-29_uspop_prereg.md line 5 ("The group 24 or younger is NOT scaled") and line 7 ("The choice rests on the allocation argument above, which holds whatever either figure is").
- **The argument, on its own terms.** "One factor over 0-24 would charge a shortfall of births to people already born." That is true of the bands alive at the 2025 base: 15-24 in 2040 were born 2016-2025. It is false of the bands born after it. Their numbers are exactly what CBO's fertility (and its immigration and mortality since birth) sets. At 2040 those are the 0-14 bands, which carry most of the gap: the model's 0-24 is +2.26 % above CBO's group (d1's info line). At 2050-2055 every person in 0-24 was born after the base, so CBO's Figure 2 group IS the right number for the model's whole 0-24 by the argument's own criterion, and the declaration ignores it. Past 2055 the held factor then carries Census's fertility forward for good: the model's 0-24 is +4.1 % above CBO's at 2055 (d1's info line).
- **A treatment that keeps the argument exists, from Figure 2 alone.** In year y, keep the bands alive at the base (ages at least y-2025, uniform inside a band as (d2) already declares) on the Census path. Give the post-base bands CBO's group less that alive part, divided by their Census sum. It equals the declaration through about 2040 for the 15+, and equals the uniform factor from 2050.
- **Measured from the sources** (my out-of-engine recomputation of the rebased target, aligned as (c) reads it, 15-19 at 36 % and 20-24 at 70.7 % participation). The cohort-consistent treatment moves (c)'s labour-force index -0.00 point at 2030, -0.00 at 2040 and **-0.37 at 2055**, so USA 2055 goes from -2.89 % to about **-3.26 %**. The uniform factor moves it -0.08, -0.35 and -0.52, to about -3.04 % at 2040 and -3.41 % at 2055. Uniform scaling over-assigns to 15-24 if CBO's fertility gap widens over time, so the true cohort figure may be a point or two of a tenth smaller. Under either reading 2055 leaves the band.
- **Failure scenario.** The record says K299 put the US population on CBO's demographics and closed K290 with every US line inside +/-3 %. But CBO's fertility never enters the model. Its post-2025 births are Census's in 2040's children and in 2055's young adults, and stay Census's after 2055. The one line that sits 0.11 point inside the band is inside only under a declaration whose stated reason does not cover that year. The prereg's disclosure shows the treatment was chosen after both alternatives' 15+ direction was computed (`recal.pl` then `recal2.pl`), which is why the reason has to carry the weight.
- **What to do.** Not a re-run under this prereg: changing the treatment after measurement would be the fitting the prereg exists to prevent. Record in the K299 line that the 2055 pass holds under the declaration only, and why (the cohort-consistent split gives about -3.26 %). File the cohort-consistent split as its own item under its own prereg. Its acceptance would then meet M2 head-on, so file them together.

**M2. (c)'s US year labels trail the pyramid by one year: "2040 on 2025" is the pyramid's 2041 on 2026. K299's (d) reads the pyramid's own year, so the two checks of one diagnostic disagree about "2040". The shift costs the US line about half a point under K299.** (K276's instrument, kept unchanged by K299's prereg. Filed, not this branch's defect.)
- **Where:** ParticipationCohortDiagnostic.cs:354 (`int y = (int)Math.Floor(r.SpanMidYear)`) and :359-360 (the base is the first such year from 2025), against :431 (`byYear[r.SubstrateYear]`).
- **The evidence.** The USA opens about 2024.2 (the d3 lines print year 10 at 2034.2), so record N spans 2023.2+N to 2024.2+N. Its floored mid-year is 2023+N, but its pyramid stands at substrate year 2024+N. (c)'s 15+ indices, x1.0324 / x1.0694 / x1.0957, match my recomputed target's P(y+1)/P(2026) (x1.0325 / x1.0698 / x1.0965), not P(y)/P(2025) (x1.0351 / x1.0758 / x1.1035, which is (d)'s x1.0350 / x1.0755 / x1.1027). Main's x1.0281 / x1.0618 / x1.0933 match Census's P(y+1)/P(2026) the same way.
- **What it costs.** The base year absorbs CBO's 2025-2026 surge (25-54 +0.78 % in that one year), so the shift costs the 15+ index -0.25 / -0.56 / -0.64 point under K299, against -0.11 / -0.38 / -0.45 under main's Census path. K299 makes the misalignment dearer. It is also why K290's -3.34 % "population part" overstated the population's share, and why (c)'s "2055" reads the pyramid at 2056, a year past CBO's data (the held factor).
- **Failure scenario.** A later item aligns (c) by substrate year and every US line moves about half a point inward. Or one reads K299's record as "the population is now within a point of CBO's" while (c) still reports a -2.45 % population part at 2055 that is mostly this shift and the potential-against-actual term.
- **What to do.** File (c)'s US alignment (key by `SubstrateYear`, or by the calendar year the pyramid stands at) as its own item beside M1's. The two move USA 2055 by similar amounts in opposite directions, so neither should land alone.

**M3. A2: the evidence now points to a supplemental "Demographic Projections" file for the January 2025 vintage, which the fetch log's capture predates. Not established either way.**
- **Where:** ElectionsData/usa/raw/uspop_cbo_demographics/fetch_log.md line 17 ("Not established: a population table by single year of age for this vintage") and its "the only data file the publication's page ... (the Archive's capture of 2025-02-05 read for its links) ... link[s]".
- **What I found** (one attempt round, 2026-10-29T18:12-18:27Z, nothing fetched into the repo; scratch files under the session scratchpad's a2_fetch/):
  - The CDX index of `www.cbo.gov/publication/60875` (status 200, collapsed by digest) lists 58 distinct captures from 2025-01-13 to 2026-08-25.
  - The page is 11.7-14.3 KB through April 2025 and 15.3-16.0 KB in 2026 (the last five: 2026-02-04, 2026-02-11, 2026-05-10, 2026-07-11, 2026-08-25). The page the implementer read (2025-02-05) is not the page CBO serves now.
  - A web search's summary of cbo.gov/publication/60875 reports that "the demographic projections file was also reposted with corrected values and additional information", beside the May 2 repost of the data underlying figures. The 61390 update's supplemental "Demographic Projections" file holds at least nine tables, and its 100-and-older tables (8 and 9) suggest age detail.
  - The 2026-08-25 capture answered 429. The prefix queries that would list the files answered 503 and 504.
- **Why it matters.** The whole four-group construction, and M1's question, exist because "CBO's file publishes no population by single year of age or five-year band". If the vintage has a by-age table, the US path can be CBO's band by band, with no Census shape and no 0-24 declaration.
- **What to do.** Record the CDX finding in the fetch log (the page changed after the capture read). Retry the 2026-08-25 capture's links when the Archive answers, by the same client. If a by-age file for 60875 exists, K299's construction should be superseded by it. That is a ruling's question, and it does not block this branch.

### Low

**L1. The US readers move from day one of turn 0, not "from turn 1".**
- **Where:** CohortDemographics.cs:280 (`Apply(country, epochYear, 0f)`) and the daily `Apply`; prereg line 21.
- **What happens.** The USA's epoch year is 2024, so `WalkToEpoch` steps nothing. Its opening `Apply` reads `Target(2025)`, which K299 changed.
  - At day 0, `PopulationGrowthRate`, `NaturalNetMigrationRate`, `NetMigrationRate` and `BirthRate` (through the mid-year denominator) read the new year.
  - From day 1, `State.Population` and `DependencyRatio` interpolate toward it.
  - The pyramid itself, and through it the labour force, potential, the wage bill's base, income tax by cohort, pensions, health and the voter age groups, moves at the first commit.
- **The year-1 supply shock.** CBO's 2024-2025 step (25-54 +0.63 % against the Census path in one year) arrives as labour supply. The USA's output gap at year 1 widens from -1.16 % to -1.57 % headless, while real growth 2024-2031 rises from 2.04 to 2.16 %. That is CBO's surge, not a defect, but the dump will show it as an early unemployment movement.
- **Expected movers.** The House apportionment does not read the model's population. The 0-24 counts move slightly too, because the health term's excess deaths are charged on the total and spread over 5-74.
- **What to do.** The integrator's expected-movement list should say "turn 0 (readings), turn 1 (pyramid)".

**L2. Group-level scaling puts steps in the implied flows at 25, 55 and 65, and a rate kink at 2055.**
- **Where:** CohortDemographics.cs:128-137.
- **What happens.** The factor is a level applied to a whole group, so a cohort crossing into a scaled group gets the group's cumulative factor at once.
  - The 20-24 of 2025 grows x1.0665 to 25-29 in 2030, against Census's x1.0530 (+1.28 %). The next transition, 25-29 to 30-34, is +0.65 %. A cohort crossing 25 in 2040 is lifted +1.43 %.
  - Inside the group, cohorts are later removed against Census once CBO's group factor falls back (2040-45, 25-29 to 30-34: -0.35 %).
  - The 55 and 65 boundaries step by the difference of two groups' factors (2040: 1.0143 against 1.0184 against 1.0118).
  - Levels are continuous. At 2055 the year-on-year change of 25-54 jumps from CBO's -0.18 % to Census's -0.39 % when the factor freezes.
- **Failure scenario.** An age-band reader (a voter group split at 25, the pension response at 55+, a migration-by-age display) sees CBO's extra immigration arrive at 25 rather than at 18-34 where immigrants arrive.
- **What to do.** Name it in the K299 line as the price of group scaling; M3's by-age file would remove it.

**L3. K299 has no RULINGS line, and K290's line still reads FILED.**
- **Where:** docs/RULINGS_2026-10-23.md on main and on the branch. The diagnostic's doc (ParticipationCohortDiagnostic.cs, the RegisteredLabourForce para) says "the record names the run".
- **What is missing.** K292-K299 appear nowhere on main.
- **What to do.** The integrator must write K299 (with the 0-24 declaration, M1's condition and the held-past-2055 rule). They must add to K290 that it was lifted by K299, with the run that printed "can be lifted". They must also renumber if K299 is taken by the time this lands.

**L4. (d) is close to a construction check, and its bands were set with that construction computed.**
- **Where:** ParticipationCohortDiagnostic.cs:424-461, :469, :472.
- **Why it confirms little.** The source-only target reproduces every (d) figure to within 0.08 %, so (d) confirms the code implements the declared formula, not that the US population matches CBO's. The prereg set its bands knowing the reweighting (0.75 %) and the concept step (1.10 %). The worst measured figures are 0.81 % (65+ 2055) against 1.0 and 1.16 % (16+ 2030) against 1.5.
- **What it does not look at.** Nothing asserts or prints the total population against Figure 2's total, although `UsDemographicOutlook.Total` is generated for it. `CivilianGroups` is generated and never read.
- **Failure scenario.** M1's +0.9 % total overshoot at 2055 is visible only by arithmetic on the 0-24 info line.
- **What to do.** Print (information, not asserted) the total against `Total` and the 0-14 against CBO's group less the alive-at-base part. That makes M1's gap a line in the log.

**L5. `UsDemographicOutlook.cs` is stored CRLF in the index; the other `-text` generated tables are stored LF.**
- **Where:** Tools/uspop_prep.pl:192 (`$cs =~ s/\n/\r\n/g`); .gitattributes:48; `git ls-files --eol` shows `i/crlf w/crlf attr/-text`.
- **Why it is harmless.** `UnemploymentFloorData.cs`, `ValueOfLostLoadData.cs` and `DebtRateSeeds.cs` are `i/lf w/lf -text` ("the generator's LF output"). This file is internally consistent: `-text` keeps the bytes on any checkout and `--check` compares them. It is a one-off convention only.
- **Also fine.**
  - SHA256SUMS digests the xlsx and not its `.headers.txt`, as K274's folder does.
  - The fetch log's "within 0.02 %" is the measured 0.0164 %, under the tool's asserted 0.05 %.
  - The prereg is LF in the index, like every review here.
  - The `.meta` is ordinary text.
- **What to do.** Nothing required. Writing LF and dropping the `-text` line for the .cs would match the house form.

**L6. The US debt hold's 2056 line moves toward its edge: 24.0 of 25 points off CBO.**
- **Where:** DebtPathHoldDiagnostic's US (h3), measured with `Tools/headless_hold/hold.sh` on both trees.
- **What moves.** The recalibrated 25+ raises US labour and GDP, so the no-policy debt ratio rises less: 2026-2056 is +50.5 points against CBO's +74.5 (main +51.3), and 2026-2036 is +10.9 against +19.6 (main +12.5; band 10).
- **Why it matters.** It is the population CBO's own path is built on, yet the model's debt moves further from CBO's. The gap is the model's primary balance and growth terms, not the population: the decomposition prints real growth -70.3 against CBO's nominal growth -142.0. Not this branch's defect.
- **Failure scenario.** K299 alone moved the line 0.8 point toward the edge. A later item of that size that lowers the US debt ratio (more labour, more growth, a smaller primary deficit) tips it. M1's cohort split would move it back a little (fewer 15-24s, less labour). M2 does not touch the hold.
- **What to do.** Watch it in the integrator's simulation bar beside USA (c) 2055, and name it in the record.

**Also checked, no finding.**
- The save step. No format bump, with the "Nor for K299" comment at SaveGameService.cs:113-116. A v69 US save loads its stored pyramid and carries its distance from the new target as a displacement, which decays only as its cohorts age out. The game's shadow is carried in the save (AF-12), so the save and its counterfactual are displaced alike and no impact is misattributed.
- `CohortAgingStepDiagnostic`. The base-exact and neutral-tracking assertions hold by construction on the new path (`Target(2024)` is the seed: the factor at 2024 cancels in `RebasedTarget`). The step-vs-publisher figure is printed, never failed.
- The Social Security area concept (territories, citizens abroad) against Census's resident population enters only as an index. Neither file states a reference date (Census's is 1 July). Either would shift the index by a fraction of a year's growth.

## Conditions for integration

- Write K299 in RULINGS with the 0-24 declaration, the held-past-2055 rule, and M1's sentence: USA 2055 passes under the declaration; the cohort-consistent split gives about -3.26 %. Mark K290 lifted (L3). File M1's split and M2's alignment as one item, and M3's retry.
- Run the simulation bar. It must print:
  - `PARTICIPATION COHORTS: PASS` with **no** REGISTERED MISS line;
  - (c) USA 2040 about -2.69 % and 2055 about -2.89 %. Watch 2055: if the engine prints -3.0x %, it is a stopping result for Elias, not a band to move;
  - every (d) ok;
  - LongRunAnchors 0 FAIL / 0 REGISTERED MISS;
  - DebtPathHoldDiagnostic PASS with its 2 REGISTERED MISS lines, US (h3) 2056 about 24.0 of 25 (L6);
  - `CohortAgingStepDiagnostic` and `GeneratedCatalogCheck` clean (the latter with the two `UsDemographicOutlook` lines ok).
- Run the dump as the family `uspop`. The EU five's fields must be byte-identical. The US fields move from turn 0 (L1). Both moved sentinel digests need ledger rows citing the integrator's record. No money-path file is in the diff by bar_tier's pattern.

## Verdict

READY (for integration, on the conditions above). The sources are byte-exact. The generator reproduces its output and reads both figures correctly (hand-checked), and both LTBO cross-checks hold independently. The headless numbers reproduce exactly: USA 2040 -2.69 %, 2055 -2.89 %. The EU five are untouched in code and byte-identical in output. K290 lifted by its own rule before its entry was removed, and the mechanism is intact. The prereg preceded the first run of the tree. What is not settled is the substance behind USA 2055: the 0-24 declaration's reason does not cover the post-base births (M1), (c)'s one-year label shift works the other way (M2), and a by-age CBO table for this vintage may exist (M3). Each is to be recorded and filed, not fixed under this prereg.

## Round 2 (2026-10-29): `wave/uspop` at 33057a0c

Delta reviewed: `git diff 593c9e4e..33057a0c`, three commits: 6c801815 (prereg amendment r2, alone), a7308a54 (amendment r3, alone) and 33057a0c (the code). Read line by line, read-only; Unity not run.

Files reviewed (round 2): Assets/Editor/ParticipationCohortDiagnostic.cs, Assets/Editor/PopulationProjectionCatalogGenerator.cs, Assets/Scripts/Data/Generated/UsDemographicOutlook.cs, Assets/Scripts/Simulation/CohortDemographics.cs, ElectionsData/usa/raw/uspop_cbo_demographics/SHA256SUMS.txt, ElectionsData/usa/raw/uspop_cbo_demographics/fetch_log.md, ElectionsData/usa/raw/uspop_cbo_demographics/wayback_20250508101602_cbo_57059-2025-01-Demographic-Projections.zip, ElectionsData/usa/raw/uspop_cbo_demographics/wayback_20250508101602_cbo_57059-2025-01-Demographic-Projections.zip.headers.txt, Reviews/2026-10-29_uspop_prereg.md, Tools/uspop_prep.pl. Also re-read: Assets/Editor/GeneratedCatalogCheck.cs (its loop now covers three sources), Assets/Scripts/Simulation/CurrencySystem.cs, Assets/Scripts/Simulation/TradeSystem.cs and WorldFactory's trade pairing (for the currency channel), Assets/Scripts/Elections/WorldClock.cs (the starts).

### Answers

**1. The zip is byte-exact.**
- `sha256sum -c SHA256SUMS.txt`: both files OK. The zip is b61c2ff6...76d38a, the same digest as the generated header and `SourceDigest[0]`.
- It is 3328177 bytes, which equals Content-Length, `x-archive-orig-content-length` and the fetch log. The capture is the raw (`id_`) form of 2025-05-08, with an original Last-Modified of 2 May 2025.
- The zip's own README says "On May 2, 2025, CBO reposted the zipped archive to correct values in" the two civilian noninstitutionalized CSVs. That makes this the repost, and the file read is not one of the two corrected. The README opens: "This data supplements CBO's January 2025 report The Demographic Outlook: 2025 to 2055."
- The member `ssArea_pop_byYearAgeMaritalSex.csv` (1808027 bytes) hashes to c0fd48bb...ba3c80e, as the fetch log records.
- `.gitattributes`' `uspop_cbo_demographics/** -text` covers the zip and its headers file. GeneratedCatalogCheck's loop runs over `SourcePath.Length`, so it now re-derives all three digests.

**2. The prep tool reads the table correctly.** I checked with my own reader (`unzip -p` piped to a separate script, not the tool's code).
- The header is `year,age,marital_status,sex,number_of_people`, and every year 2022-2098 carries 808 rows (101 ages x 4 statuses x 2 sexes).
- Raw cells:
  - 2025, age 0, single: female 1833589 and male 1920221.
  - 2040, age 30, married: female 1118707 and male 901436.
  - 2040, age 100+, married: female 4107 and male 10814.
- Folded over sex and status, the totals are 346.5880 (2024), 349.8059 (2025), 367.9501 (2040) and 372.0388 (2055) million. The four groups are:
  - 2024: 105.9473 / 135.9304 / 42.6916 / 62.0186;
  - 2040: 97.5456 / 148.0705 / 42.4751 / 79.8590;
  - 2055: 93.0986 / 143.8772 / 47.9968 / 87.0662.

  Each equals Figure 2's figures (346.588, 349.806, 367.95, 372.039 and the groups) to the figure file's 0.0005 million, which is the assertion the tool makes (`--dry`: "each year's total and four groups equal Figure 2's to 0.001 million through 2055").
- The generated `Bands` (75 years, 2024-2098, 21 bands, 100+ in the open band) match my fold to 4.2e-7 relative, which is the %.7g rounding. `Population15Plus` and `Population16Plus` match to 1.6e-7.
- `--check` exits 0 and the gate prints GATE OK.

**3. The past-2098 rule is sound.**
- Through 2098 the path IS CBO's band (`CohortDemographics.cs`, `if (y <= cboLast) { path[...] = UsDemographicOutlook.For(y); continue; }`).
- For 2099 and 2100 each band is CBO's 2098 band x Census's band change since 2098, formed in double and stored once. It is continuous at 2098 and has a zero guard. Past 2100 the last pyramid holds, as for every country.
- It governs two years of the catalog. Census's band ratios carry CBO's 2098 pyramid without mixing levels, and the rebasing base (CBO's 2024 bands against the seed) is unaffected. No finding.

**4. M2's re-keying is right for every country, and the Sweden/Poland movement is honest and within r2-2's rule.**
- `int y = r.SubstrateYear` (ParticipationCohortDiagnostic.cs, the (c) loop) keys every record by the year its pyramid stands at, which is (d)'s key. The old floored mid-year equals it only for a run that opens in the second half of its year. From the starts (the d3 lines' printed years, and WorldClock):
  - first half, so the key shifts by one: the USA 2024.2, Sweden 2026.0, Poland 2023.1;
  - second half, so the key is unchanged: Germany 2024.85, Italy 2022.55, France 2024.55 (by about two weeks).
- Germany's, France's and Italy's lines are byte-identical to main's, as they should be.
- Sweden's bases move 2026 -> 2027 and Poland's 2023 -> 2024. My run's figures equal r3-3's exactly:
  - Sweden: +0.12 / -0.22 / **+0.31** / -0.12 %;
  - Poland: +1.67 / +2.25 / **+2.47** / -2.31 %, against main's +1.69 / +1.93 / +1.82 / -2.43.
- r2-2 provided for exactly this ("if any EU line moves, the item reports it as this alignment, with its figure, and does not re-band it"), and r3-3 does that: no band, year or rule is touched.
- Two blemishes for the record (L-r2-1 and L-r2-2 below).

**5. r2 and r3 were each committed alone, before the code they govern, and their disclosures are complete apart from one later mover.**
- **r2** was committed 19:57:28 +0100 and contains only the prereg. The r2 code existed uncommitted (scratchpad diffs at 19:55-19:56), which r2's wording allows ("before any code ... is committed and before any run"). The first r2-tree output (`anchors_r2.txt`) completed 20:00:59. The r2 code was never committed.
- **r3** was committed 20:23:29 +0100, alone. The zip was fetched at 19:21:57Z (20:21:57 local), before r3 and before any r2 code commit, so r2-4's pre-registered condition was met. The switch to the table was registered before the r2 tree was measured; it was not chosen on its result. The r3 code was written after r3 (scratchpad edits 20:24-20:27). The first r3 run completed 20:31:30 and the code was committed 20:36:50.
- **The disclosures.**
  - r2 discloses the review's figures it had in hand.
  - r3 discloses the r2 tree's results (USA -1.42 / -2.18 / -2.72 %, the (d) lines, Sweden and Poland).
  - r3 also discloses the table's source-only reweighting. I reproduce it exactly: 65+ +0.27 % at 2055, 25+ +0.13 %, 15+ +0.08 %, 16+ +0.12 % (the four-fifths approximation alone +0.05 %).
  - What neither could disclose is Poland's (d1) info mover, which appears only on the r3 tree (question 7). The record must name it.

**6. The headless figures reproduce.**
- **anchors.sh:** byte-identical (`cmp`) to the implementer's `anchors_r3.txt`.
  - USA (c): 2030 **-1.24 %**, 2040 **-2.17 %**, 2055 **-2.56 %**.
  - Every (d1), (d2) and (d3) line is ok. The (d1) groups are within 0.21 % (65+ 2055), and the whole population is within 0.01 % of CBO's total (review L4 answered).
  - The (d3) lines: 15+ within 0.01 %, 16+ within 0.05 %.
  - LongRunAnchors: 0 FAIL, 0 REGISTERED MISS. The USA's (d1) is 2.21 against 2.11, and its (d2) windows 1.61 against 1.71 and 1.44 against 1.46.
  - `PARTICIPATION COHORTS: PASS`, with no REGISTERED MISS line.
- **hold.sh:** byte-identical to `hold_r3.txt`. DEBT HOLD: PASS, with main's 2 REGISTERED MISS lines (Sweden h2).
  - US (h3) 2026-2036 is +10.4 points against CBO's +19.6, **9.2 of the 10** (main 7.1).
  - US 2026-2056 is +52.0 against +74.5, 22.5 of 25 (round 1: 24.0).
  - Every EU line of the hold is unchanged.

**7. The currency channel is plausible as the source of Poland's 0.01, and the probe confirms it.**
- **The mover.** LongRunAnchors' Poland (d1) info line: "the gap ... at year 10 -1.24 %" becomes "-1.23 %". It is information, not asserted. It is absent on the r2 tree (US path: the cohort split) and present on the r3 tree.
- **The mechanism exists.**
  - All six countries trade with each other: WorldFactory pairs every trader, 15 links.
  - `CurrencySystem.ApplyCurrencyStrength` moves a country with its own currency toward 100 + 5 x (its rate - its partners' average rate). The Fed's rate is in Poland's and Sweden's partner averages.
  - `TradeSystem.GetExportCurrencyFactor` scales a country's exports by 1 - 0.5 x (strength - 100)/100. Trade volumes are otherwise fixed (no partner-GDP term).
  - So the US population moves the US gap, the Fed's rate, Poland's zloty and its exports, then its gap.
  - The euro members cannot move this way: a shared zone's export factor is 1, and the ECB rule weights only its members. That fits Germany, France and Italy being byte-identical.
- **The probe.** In scratch copies of main and of 33057a0c, I removed the USA from every partner-rate average (one line in `CurrencySystem.cs`) and ran anchors.sh on both.
  - Every non-(c) line of all five EU countries is then byte-identical between the two probes. Poland's year-10 gap reads -1.25 % in both.
  - So with the Fed's rate cut out of the partner averages, the US path change reaches no EU line, and that rate is the only path by which it reached Poland's.
  - The probe also moves Sweden's and Poland's own info lines against unprobed main, which shows the Fed's rate is live in both zones' strength. That Sweden's printed figures did not move on the branch is a matter of size, not of a missing channel.
  - The claim holds: the 0.01 is the currency channel, small, and the dump may show small Swedish and Polish movers through it.

### Findings, round 2

**High:** none.

**Medium:** none. Round 1's M1 is moot: the path is CBO's by age, and CBO's fertility now enters (the model's 0-24 is within 0.07 % of CBO's at 2055, against +4.10 % before). M2 is fixed for all six. M3 is answered by the table it pointed to.

**Low**

**L-r2-1. r2-2's expectation was a checkable factual error, and r2-5 now contradicts r3-3.**
- **Where:** Reviews/2026-10-29_uspop_prereg.md, r2-2 ("the EU five's runs open in the second half of their years") and r2-5 ("the EU five's lines byte-identical").
- **What went wrong.** Sweden's start (about 1 January 2026) and Poland's (about February 2023) are in WorldClock and in the d3 lines' printed years. r3-3 corrects the expectation and applies r2-2's own clause, honestly. But r2-5's "byte-identical" still stands in the text, and the prereg's point 3 promised byte-identical EU lines out of the engine.
- **What to do.** The integrator's record should say which rule governed. The EU model is untouched (Germany, France and Italy byte-identical; Sweden's and Poland's model lines identical). Sweden's and Poland's (c) printouts moved by the instrument's re-key under r2-2. One Poland info line moved by the currency channel, named with its figure under point 3.

**L-r2-2. The re-key is uniform, not exact: it reads the EU's 1 January stock against the Ageing Report's annual labour force.**
- **Where:** the (c) loop's `int y = r.SubstrateYear`.
- **The half-year offset.** EUROPOP2023's pyramid is a 1 January stock. The substrate year Y is therefore half a year before the mid-year Y the Ageing Report's annual figures centre on. Before the re-key that was half a year early for Germany, France and Italy and half a year late for Sweden and Poland. Now it is half a year early for all five, which is the consistent choice and matches (d).
- **The USA.** It is the Census seed's 1 July rebased onto CBO's table. CBO's file states no reference date.
- **Scope.** Not a defect of this branch; a note for anyone reading (c)'s EU indices to the tenth.

**L-r2-3. Several margins tightened, and one is now the US line nearest its edge.**
- US debt hold 2026-36: **9.2 of 10** (0.8 point margin, against 2.9 on main).
- Poland (c) 2050: +2.47 % of ±3 % (0.53 margin, against 1.18 on main), moved by the re-key, not the model.
- USA (c) 2055 now has 0.44 margin (-2.56 %), better than round 1's 0.11.
- **What to do.** Watch all three in the integrator's simulation bar. A US line pushed outside is a stopping result under r2-5/r3-4.

**L-r2-4. (d3)'s band is about eight times the deviation the sources predict.**
- **Where:** ParticipationCohortDiagnostic.cs `UsAdultBand = 0.01`.
- **The gap.** r3 discloses "generous by design" against at most 0.08 % (15+) and 0.12 % (16+), and the run prints 0.01 % and 0.05 %. So (d3) catches a gross failure (a misread table, a wrong year index), not a drift. This is disclosed and acceptable, but it should not be quoted as a tight hold.

**L-r2-5. Carried from round 1, still the integrator's.**
- K299 and K290's lift in RULINGS (L3).
- The prereg's "Expected movement" still says "from turn 1". US readings move at day 0 of turn 0 (L1), and with the table the first-year step is CBO's by age.
- The generated .cs is still CRLF in the index (L5, harmless).

### Conditions for integration (round 2)

- Record K299 as the by-age path (amendments r2 and r3), K290 lifted, and the movers by name:
  - Sweden's and Poland's (c) lines (the re-key, with figures);
  - Poland's (d1) year-10 gap -1.24 -> -1.23 % (the currency channel).
- Simulation bar:
  - `PARTICIPATION COHORTS: PASS` with no REGISTERED MISS;
  - USA (c) about -1.24 / -2.17 / -2.56 %;
  - every (d1)/(d2)/(d3) ok;
  - LongRunAnchors 0/0;
  - DebtPathHold PASS with US 2026-36 about 9.2 of 10;
  - GeneratedCatalogCheck clean with three `UsDemographicOutlook` lines;
  - CohortAgingStepDiagnostic clean.
- The `uspop` dump. Germany, France and Italy must be byte-identical. Sweden's and Poland's model fields may move only by the currency channel (small), and any other mover is a defect. US fields move from turn 0.
- Ledger rows for both moved sentinel digests citing the integrator's record. No money-path file is in the diff.

### Verdict (round 2)

READY (for integration, on the round-2 conditions above).
- The by-age zip is byte-exact. The tool reads its single years correctly (hand-checked raw cells, an independent fold, and totals and groups equal to Figure 2), and the past-2098 rule is sound.
- The USA's path is now CBO's own, band by band, so round 1's M1 is moot. M2's re-key is right for all six countries. Sweden's and Poland's (c) movements are honest, and they are reported under r2-2's own clause.
- r2 and r3 were each committed alone before the code they govern, and r3's switch to the table was pre-registered in r2-4 before the r2 tree was measured.
- Both headless runs reproduce byte for byte: USA (c) -1.24 / -2.17 / -2.56 %, and US debt hold 2026-36 at 9.2 of 10.
- Poland's 0.01 is the currency channel, confirmed by the probe.
- What remains is for the record (L-r2-1 to L-r2-5) and for the engine bar's watch list (L-r2-3).
