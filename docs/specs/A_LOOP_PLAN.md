# A-LOOP - the political loop: the plan put to Elias before any code

**Ruled by Elias, 2026-10-18** (`docs/RULINGS_2026-10-18.md`, the audit block): *"A-LOOP, the political loop: one plan, put to me before any code, measured on the idle and fuzz runs. Every coefficient is sourced, or [AUTHORED-DRAFT] on the calibration list."* Sub-items q15, q13, q24, q37, q34, q5; then q4. Feature list row: `POLISIM_FEATURE_LIST.md` **3. A-LOOP**. The questions and their evidence: `docs/audit/AUDIT_FOR_ARCHITECT.md` 5, 13, 15, 24, 34, 37 (and 4); ledger rows GA-0-23, GA-0-24, GA-0-25, GA-2-3, GA-2-5, GA-2-12, GA-5-1, GA-12-3; inventory B D-32 and D-33 (`docs/audit/inventory/B_money.md`); `COMPLETED.md` §846 (US-9's saturation).

This is a PLAN: nothing here is built, and no constant moves until Elias rules on §9.

**How to read the pointers.** Each points at a member by name (the durable pointer). The line number beside it is a dated reading at the base commit `40f33c75`, a record of where the member was on 2026-10-18, not a claim about the tree later. The ledger's measured figures are the ledger's, quoted with their run ids.

**Sources.** Every figure quoted from the literature was read from a stored file under `docs/reference/raw/aloop/`, fetched byte-exact with curl on 2026-10-18; each file's digest is in `docs/reference/raw/aloop/SHA256SUMS.txt`. The register is §8. A source that could not be reached is BILLED there, with what is missing.

---

## 0. The loop in one picture, and the one place it plugs in

```
 published unemployment, inflation ── Duch & Stevenson economic vote (built, §638/§752) ─┐
 approval, net of its economy terms ── popularity function (q15) ────────────────────────┤
 the budget balance over the term ──── Brender & Drazen (q13) ───────────────────────────┤→ the RECORD SHIFT, per party,
                                                                                          │   split by responsibility
 tax moves (q37, q34) ── a decaying tax term ──→ approval                                 │
 events (q5) ── salience ──→ the campaign chain, the stance model                         │
                                                                                          ▼
 campaign contacts (q24) ── Kalla & Broockman / Green & Gerber, in share points ──→ vote shares ──→ seats ──→ formation ──→ office
                                                                                                                     │
                                                         the new government's term starts (EconomicVote.TookOffice) ◄┘
```

- **The plug.** The economic vote already reaches every live count by one route: a per-party share shift, delivered exactly, absorbed by the parties it does not name (`EconomicVote.RecordShiftOf`, `EconomicVote.ApplyRecordShift`, EconomicVote.cs:187, :200).
  - It enters the campaign's preference (`CampaignRun.CurrentPreference`, CampaignRun.cs:1161).
  - It enters the no-campaign predictions (`NationalElection.TryPredictShares`, NationalElection.cs:207: Poland's Sejm and first round, the US count).
  - It is judged again on the eve for the count (`SimulationManager.StepPlayerCampaignDay`, SimulationManager.cs:4594).
- **A-LOOP's q15 and q13 terms join that shift as two more components of one record.** No second path into the vote is built.
- **The record is split among the governing parties by the sourced responsibility shares the economic vote already carries** (`EconomicVote.Magnitudes`, EconomicVote.cs:61: the PM's party by cabinet type [BK-D] Table 9.4, each partner by Figure 9.7, the US president's party by Table 9.1). The split is that vector normalised to sum to one (DERIVED). The opposition gets none, as Duch & Stevenson's pooling gives it none, so its share of a loss arrives by absorption.
- **q24 moves to the same share space.** The campaign stops writing compatibility, which is where it saturates.

---

## 1. q34 - a new levy is a rise from zero; a removal is relief. Size **S**. *(first to land)*

**(1) Today.**
- **The levy costs nothing.** A tax programme bill sets `IsImplemented` and nothing else: no hike penalty on PASS (`ParliamentSystem.ApplyTaxProgramBillResult`, ParliamentSystem.cs:617), although its direction is already read from zero (`ParliamentSystem.GetTaxProgramBillDirection`, :604).
- **The burden side misses it too.** `HouseholdTaxBurdenShare` (MacroSystem.cs:146) skips a line not levied today on both sides (:151). Its seed side reads `Country.BaselineTaxRates`, which the seed fills with EVERY line's rate, placeholders included (WorldFactory.cs:1154, :1212). So:
  - a levy's seed rate is its own placeholder, and the gap stays zero (D-32);
  - a removed seed tax drops out of both sides, so households get no relief (D-32's mirror).
- **Measured (GA-12-3, run x_se_x05 against x_se_idle):** five levies, one a month, pass. They cut the debt by 14 billion over three years, with approval at 49.6 and participation at 65.24, the idle run's figures to the tenth.

**(2) Form.**
- **The seed's levied set is stored as the record:** `Country.SeedLevied`, the lines `IsImplemented` at the seed (the ruling's "which lines the seed levied is the record").
- **The burden reads it.**
  - The seed side counts a line at its seed rate where the seed levied it, whether or not it is levied today.
  - A line the seed did not levy has a seed rate of zero.
  - The "now" side counts only what is levied today.
- **The levy and the removal enter q37's tax term** as moves of that line's rate: from 0 to its rate (a rise), from its rate to 0 (a cut, and so the smaller mirror). This uses q37's fiscal-year reference, so a levy and its repeal in the same year net to zero.

**(3) Coefficients.** None new. The penalty is q37's (§2), the burden is the existing MPC term (`MacroSystem.MarginalPropensityToConsume`, D-8, held).

**(4) Measured.**
- **Runs:** x_se_x05 against x_se_idle (`Tools/audit/area12_exploits.plan`, through `BatchSimulationRunner.RunAudit`).
- **Pass:**
  - each levy charges q37's rise and the burden gap opens by the line's revenue share;
  - a removal of a seed tax gives the mirror and a negative burden gap;
  - a levy undone in the same fiscal year leaves approval within 0.01 of the run without it;
  - `HouseholdBurdenAnchorDiagnostic` still holds both anchors at zero at the seed.

**(5) Families.**
- **Expected inert to the no-policy dump:** an AI government never levies or removes a line, and an unlevied line is skipped on both sides today.
- **Proved by the dump diff** (`TrajectorySentinelCheck`, `Tools/traj_diff.pl`), never by reading. If the dump moves, the piece is its own family, `aloop-levy`.

**(6) Risk.** The seed's four always-off lines are a modelling convention, not the statute. The excise, property, wealth and stamp duty lines are seeded inactive at a uniform placeholder (`WorldFactory.SeedTaxLines`, :1171), yet Sweden levies excise duties and a municipal property fee in fact. So "the seed levied it" is the game's seed, not Skatteverket's list. This plan does not widen q34 to re-seed them; it is recorded here as a finding for the model families.


**Built (2026-10-19, `COMPLETED.md` §877, `wave/aloop1` 8af3bfa0):** `Country.SeedLevied` is the record and the burden reads it; a tax programme bill returns its `TaxLevyMove` for q37. The penalty is q37's (§2). Inert to the no-policy dump; the levy audit (x_se_x05) steps the burden up by each line's revenue share.
---

## 2. q37 - the tax term reads a change against the fiscal year's opening rates; a cut earns a smaller mirror; the term decays. Size **M**.

**(1) Today.**
- **Two sites charge 1.5 approval points per point raised, rises only** (`MacroSystem.TaxHikeApprovalSensitivity` = 1.5, MacroSystem.cs:2146, [AUTHORED-DRAFT]):
  - the turn's policy decision (`SimulationManager.ApplyTaxRateChanges`, SimulationManager.cs:6297, read into `MacroSystem.ApplyApprovalRating`, MacroSystem.cs:2265);
  - every passed budget bill, as an immediate shock (`ParliamentSystem.ApplyBillResult`, :592-593).
- **The rise is measured against the line's rate the moment before**, so a cut is free and the bill that restores it pays the whole penalty.
- **The units are mixed (D-33):** lever points for a rate, effective-rate points for the bands and sub-rows (`TaxBases.EffectiveRate`), and the dial's per cent for a per-tonne line (EN-4c).
- **Nothing decays the penalty but approval's own reversion**, 5 % of the gap a turn (`MacroSystem.ApprovalReversionSpeed`, :2098, CONVENTION). A turn is 365 days (`SimulationManager.DaysPerTurn`, :216), so a shock keeps 95 % of itself a year: effectively permanent.
- **Measured (GA-2-12, §828's recheck):** Sweden's sub-row floor bill, some 23 points of the effective rate down, left approval at 50. The bill putting the statute back took it from 52 to 18 in a day.

**(2) Form.**
- **Reference.** At each fiscal-year start (`FiscalYearData.GetFiscalYearStart`), each line's effective rate is snapshotted, sub-rows included.
- **The year's charged position per line is P(net):** net = rate now − rate at the year's opening, in ONE unit (D-33's fix: effective-rate points for every rate line, the dial's per cent for a per-tonne line).
  - P(x) = s·x for a rise;
  - P(x) = m·s·x for a cut, with 0 < m < 1;
  - s is today's 1.5.
- **Each change books the impulse P(net after) − P(net before)** into a tax-term stock T, from either site. A cut then its restore inside one year nets to zero; across a year boundary the restore is a rise from the new opening.
- **T decays geometrically** at the popularity persistence below. Approval reads it. Approval's other terms and its reversion are untouched.
- **q34's levy and removal are moves of the same P.**

**(3) Coefficients.**
- **s = 1.5: unchanged, [AUTHORED-DRAFT].** It stays on the list under its existing remark. Re-unitting the bands onto the same scale leaves s's figure as is: a ruling-free unit change, stated at the constant.
- **m, the mirror: [AUTHORED-DRAFT].** Proposed 0.5. The DIRECTION (a cut earns less than a rise costs) is Elias's ruling; the grievance asymmetry Nannestad & Paldam name is the literature's support, but no reachable page gives a magnitude.
  - Calibration-list entry text: *"q37's mirror `m` (a cut's approval gain per point, as a fraction of a rise's cost): 0.5 drafted; the direction ruled 2026-10-18. Look for in play: does a tax cut buy anything a player notices, and does the restoring bill still sting?"*
- **The decay, SOURCED (US) and transferred:** the quarterly persistence of presidential popularity in the popularity functions LB&S 2000 tabulate [LBS00].
  - Table 2: "Popularity lagged 0.81 (0.04)" and "0.79 (0.04)" (quarterly Gallup approval, 1954-1996, Erikson et al. 2000).
  - Table 1: 0.84 (quarterly, 1953-1986) and 0.89 (monthly), Beck 1991.
  - Proposed: 0.81 a quarter. A one-off shock then halves in about 3.3 quarters (some 300 days); the range of the four is about 270-360 days at the quarterly ones.
  - The transfer from US presidential approval to every country is the authored part. Calibration-list entry text: *"q37's tax-term persistence: 0.81 a quarter, from the US popularity functions [LBS00] Table 2 (0.79-0.84 across the tabulated quarterly ones), transferred to every country. Look for in play: a year after a rise, is the player still being punished for it?"*

**(4) Measured.**
- **Runs:**
  - GA-2-12's replay: the plans `Tools/audit/p4_ga2_12*.plan`; X-02S/X-02R through `BatchSimulationRunner.RunAudit`.
  - The money poles: `Tools/audit/area2_money.plan` a2_*_deficit/surplus/big/small.
  - The fuzz runs a2_se_fuzz … a2_fr_fuzz (seeds 501-507).
- **Pass:**
  - a cut and its restore in one fiscal year: |Δapproval| ≤ 0.01 against the run with neither;
  - a cut alone: +m·s·points;
  - a rise's impulse is at half its size within ±10 days of the declared half-life;
  - a band move and a rate move of the same effective points charge the same;
  - no approval outside [0, 100], no NaN;
  - the attribution ledger's identity (`ApprovalLedgerRecorder`'s self-audit, ATTRIB) holds with T as a named term.

**(5) Families.**
- **`aloop-tax`:** the AI governments move rates by the fiscal reaction function through the same `ApplyTaxRateChanges`, so approval moves in the no-policy dump wherever an AI raises or cuts a rate.
- Dumped, explained per country (`Tools/traj_diff.pl`), and the sentinel's label and digests moved in the same commit. A money path, so `Tools/bar_tier.ps1`'s review row is owed (§546).

**(6) Risk.** None beyond the family. T must ride the preview's clone (`PreviewParityDiagnostic` fails the simulation bar by name if it does not) and the save, which is a format bump.

---

## 3. q15 - approval reaches the vote, through a sourced popularity function. Size **M**.

**(1) Today.**
- **Approval is a stock** (`EconomyState.ApprovalRating`). Every country seeds it at 50 (WorldFactory.cs:130-180, CONVENTION). It moves by `MacroSystem.ApplyApprovalRating` (:2265, at turn boundaries) and by events, cards and bills at any day.
- **Nothing in any vote reads it** (GA-0-24): not the stance model, the campaign, the count or a motion. The party stock `Country.PartyApprovalRating` (Country.cs:1008) is written only at seating and read by nothing (inventory D-33).
- **Measured (GA-2-5, a2_se_big, seed 403):** M's government at 1.5 % approval and 10.2 % unemployment on polling day takes 66 seats against 75 at 50.5 % (a2_se_deficit). Its bloc keeps 183 of 349. The 9 seats are the economic vote's (unemployment), not approval's. Poland's KO fuzz government sat at 0.0-0.5 for 600 days with no motion, fall or vote.

**(2) Form.**
- **The approval term of the record:** shift_gov = b × (A_E − A_0).
  - A_E is approval on the count's day: the eve, as §752 judges the economy.
  - A_0 is approval on the day the government took office (`EconomicVote.TookOffice`, :115), so a seeded government's reference is the seed's 50.
  - Reading the change since office keeps the CONVENTION origin out of the vote, and a government formed in play starts at zero, as §752 rules for the economy.
- **shift_gov is split among the governing parties by the responsibility vector (§0)** and added to the record shift.
- **Which approval (Q1, Code recommends the net form).** The sourced vote functions estimate popularity's coefficient with the economy as a separate regressor (Lewis-Beck & Tien's has GNP change beside July approval). The game's economy already reaches the vote by the economic vote, and approval contains three economy terms: the growth gap and the unemployment and inflation misery terms (`MacroSystem.ApplyApprovalRating`).
  - **Recommended (net):** the vote reads approval net of those three. A second stock carries just those terms under approval's own reversion, and the vote reads approval minus it. The attribution ledger already names each term (`ApprovalAttribution`).
  - **Alternative (whole):** the vote reads approval whole, and the economy partly counts twice.
- **What it gives COAL-1 (q1/q21: "a partner's decision to leave reads its own projected vote").** The record shift is computed per party for any day, decomposed into its three terms:
  - **Staying:** the party's projected share is the prediction plus its shift.
  - **Leaving:** the party is opposition, so its own shift is zero, and the absorbed share is its gain.
  - Both are one call on the day, so COAL-1 reads them without its own vote model.
- **What it gives R-US20 (I4: "revisit against (c) once approval reaches the vote").**
  - **The US House midterm gets the sourced literature model (c) asks for:** Tufte's midterm equation as LB&S 2000 tabulate it [LBS00] Table 7 col. 1: the president's party's standardized vote loss = −10.74 + 0.13 (SE 0.04) × presidential approval + 0.62 (SE 0.17) × income change, R² 0.825, N = 8 (midterms 1946-1978).
  - Its approval input is the game's approval of the president; its income input is the game's real disposable income growth.
  - R-US20's (b) and (c) can then be run on US-13's record side by side. That needs a presidential approval series of record for the backtest: Gallup's series, BILLED in §8.

**(3) Coefficients.**
- **US presidential: b = 0.16 per approval point, SOURCED (secondary).**
  - Lewis-Beck & Tien 1996 as tabulated in [LBS00] Table 4 col. 2: the incumbent party's two-party presidential vote, 1952-1992. Gallup July approval 0.16 (t 2.11), GNP change 1.83 (t 3.33), peace/prosperity 0.14 (t 2.35), constant 27.34; adjusted R² 0.88, SEE 2.26, N = 11.
  - Secondary because the 1996 original was not fetched (BILLED).
- **US House midterm: 0.13, SOURCED (secondary).** As above, Tufte 1978 via [LBS00] Table 7.
- **US House on-year: no coefficient on approval fetched.** [AUTHORED-DRAFT] to carry 0.13; calibration-list entry text: *"q15's on-year House approval coefficient: the midterm's 0.13 carried; no on-year estimate fetched."*
- **Sweden, Germany, Italy, Poland (Q2).** No reachable page estimates a government-approval coefficient on a parliamentary vote in these countries.
  - The bracket fetched: the US presidential 0.16, the US midterm 0.13, and France's Assembly vote −0.38 on presidential popularity six months before (the opposition's first-round share, 1958-1993, N = 10, t 3.69; Lewis-Beck 1995 via [LBS00] Table 10 col. 2).
  - **Code recommends the transfer of 0.16** (the smaller of the two presidential-vote figures that are not cohabitation-bound), as [AUTHORED-DRAFT] with the calibration-list entry text: *"q15's approval-to-vote coefficient outside the USA: 0.16 per point of approval change over the term, Lewis-Beck & Tien's US presidential figure [LBS00] transferred; the fetched bracket is 0.13-0.38. Look for in play: does a government the player has driven to 20 % lose the count it would otherwise win?"*
  - France holds no modelled election (§631), so its own figure has no consumer.
- **A reading, not a term: the cost of ruling.** Nannestad & Paldam: "It costs the average government almost 2% of the vote to rule" [NP94]. The idle runs print each governing party's term-on-term share change beside it. It is not built as a term (the ruling names none).

**(4) Measured.**
- **Runs:**
  - Idle: area3's f1_se_m/s/l, f3_de_spd/afd, f4_pl_pis/sld/psl/mn, f5_it/us (`Tools/audit/area3_elections.plan`).
  - The low-approval cases: a2_se_big (seed 403), a2_se_deficit, a2_pl_ko_fuzz.
  - The fuzz runs: a2_*_fuzz, f1_se_c, f3_de_linke_fuzz, f4_pl_konf_fuzz, and the thirty-year w_se_fuzz30 / w_de_fuzz30.
  - The figures `ElectionsBacktests.RunBatch` prints (ElectionNightHarness, GateReRun, VoteShareBacktest; OutOfSample2026Diagnostic refuses a second run by design, so its kept record is read, not re-run).
- **Pass (declared here, before any run; Elias may change the numbers):**
  - (a) **Idle no worse:** per country, the idle count's mean absolute share miss against the record the night prints ("as it happened") worsens by at most 0.5 point, and the government formed is the same or closer to the record's. Both are printed against the same seeds before A-LOOP.
  - (b) **A 1.5 % government loses:** in a2_se_big the governing bloc loses its majority on polling day (below 175 of 349), and the night's WHY names the approval term with its size. With b = 0.16 the term is about −7.8 points for the government side (48.5 points × 0.16), split by responsibility.
  - (c) **A 50 % government is untouched:** where A_E = A_0 the term is exactly zero (a dump-diff-style byte check on an idle run whose approval does not move).
  - (d) **Fuzz:** shares sum to one and stay non-negative, no NaN, and |shift_gov| ≤ 0.16 × 100 by construction.

**(5) Families.**
- **None in the no-policy dump:** the dump sets no player, and only the player's country votes. Proved by the dump diff, not argued.
- The audit runs' counts move. They are recorded as the piece's readings under its § with the run ids, not as a baseline. The campaign harnesses' digests are unaffected (the record shift is zero in their staging) and are checked.

**(6) Risk.** Approval's persistence (Q3). Approval's own reversion keeps 95 % of a shock a year, against the 0.79-0.84 a QUARTER the tabulated popularity functions measure [LBS00]. Read over a term, this term will carry shocks a real electorate has forgotten. The term-change form limits it to the current term. Changing approval's reversion is a family of its own across every country.

---

## 4. q13 - deficits and debt reach the vote (Brender & Drazen 2008, AER). Size **M**. *(after q15; it shares the plumbing)*

**(1) Today.**
- No approval, vote or office term reads the debt, the deficit or the rating (inventory B X-07, X-04).
- The one fiscal-strain touch is a discount on spending's approval BENEFIT above 60 % debt (`MacroSystem.DeficitAwarenessDebtToGdpThreshold`, :2190).
- **Measured (GA-2-3):** Sweden's debt ratio goes 35 % → 95 % in two years while approval rises 50 → 65 (a2_se_deficit, seed 401). Germany's goes 63 % → 121 % at 46-50 (a2_de_deficit, seed 411). Full welfare lifts Sweden's approval 50 → 72.8 as the debt doubles (a2_se_x04, seed 481).

**(2) Form.** Brender & Drazen's two fiscal variables, read as they define them [BD06] Table 2 note 2, on the game's closed-year balances (the year the player reads, after AF-17's fix lands):
- **BALCH_term:** "The change in the ratio of the government deficit to GDP in the two years preceding the election year, relative to the two previous years". In the game: the mean balance/GDP of fiscal years E−2 and E−1, minus that of E−4 and E−3.
- **BALCH_ey:** "The change in the government deficit ratio to GDP In the election year, compared to the previous year". In the game: the election year's balance to date, annualised (DECLARED), minus E−1's.
- **The probit index shift** Δz = β_term·BALCH_term + β_ey·BALCH_ey (ratios as fractions) **is converted to the government's share by the spread of incumbent vote swings: shift_gov = σ_v × Δz** (Q4).
  - The premise: re-election is the incumbent's vote change clearing a threshold under a normal spread σ_v, so one unit of the probit's index is σ_v points of share.
  - It is split by responsibility, like q15's term.
- **Debt reaches the vote through the deficits that make it** (Q5). Brender & Drazen carry no debt-level term, and no fetched source gives one.
- **Growth is not added.** Brender & Drazen find it insignificant in developed democracies [BD06] Table 2 col. 2: −3.077, p 0.615. The economic vote's reading of unemployment and inflation stays the economy's channel.

**(3) Coefficients.**
- **β_term = 11.339 (p 0.026) and β_ey = 22.073 (p 0.001): SOURCED,** probit coefficients, developed countries, narrow sample (the leader runs again), N = 164 [BD06] Table 2 col. 2.
  - The expanded sample's 7.172 (p 0.108) and 19.390 (p 0.003), N = 180, are printed beside them.
  - The authors' own reading: "an increase of 1 percentage point in the central government surplus ratio to GDP can increase the probability of reelection by 3-4.5 percentage points in the developed/established democracies and … an increase of 1 percentage point in the surplus during an election year increases the probability of reelection by 7-9 percentage points" [BD06] p. 15. The published abstract has the same direction [CR-AER].
- **σ_v: FITTED,** with n and its interval printed. The standard deviation of the PM's (US: the president's) party's vote-share change between consecutive elections, over the estimating sample's frame (developed democracies, 1960-2003).
  - The source is the ParlGov election table, BILLED until fetched in this piece (§8). The six countries' own returns on disk are too few to fit it.
  - For scale only: at σ_v = 4 points, one point of GDP improvement over the term is worth +0.45 points of share and one in the election year +0.88.
- **A bound: [AUTHORED-DRAFT].** Δz is held inside the estimating sample's range of the two variables. Linear extrapolation of a probit index to a 30-point-of-GDP deficit is not a measurement. The sample's ranges are in the replication data (openICPSR 113276, behind a login: BILLED). Until fetched, ±10 points of GDP is drafted. Calibration-list entry text: *"q13's support bound: BALCH_term and BALCH_ey held within ±10 % of GDP until Brender & Drazen's replication data gives the sample's range."*

**(4) Measured.**
- **Runs:** a2_se_deficit, a2_de_deficit, a2_se_x04 (GA-2-3's own), the surplus poles a2_*_surplus, the idle and fuzz set of §3.
- **Pass:**
  - the deficit poles' governments lose share by the term's printed size, with the term named on the night;
  - the surplus poles gain;
  - an idle government whose balance path is the no-policy one moves by what the no-policy balance implies, printed per country;
  - (a) of §3 holds with both terms on.

**(5) Families.** None in the no-policy dump (the same reason as q15). Proved by the dump diff.

**(6) Risks.**
- The unit conversion (Q4) is a premise.
- B&D measure the LEADER's re-election in 347 campaigns, 1960-2003. The game reads it as the share of the parties that govern.
- The published AER tables are not reachable (403); the 2006 working paper's are used, its figures matching the 2005 NBER version's text. GAP noted in §8.

---

## 5. q24 - a campaign moves the vote within sourced bounds, never saturating at the clamp; each run-up purchase buys a measured effect or is removed. Size **L** (5a persuasion, 5b mobilisation).

**(1) Today.**
- **§42's chain** (`CampaignActions.Resolve`, CampaignActions.cs:169) makes persuasion pressure: people reached × attention × relevance × credibility × an authored weight.
- **The pressure becomes compatibility points** at `CampaignPressure.PersuasionPerCompatibilityPoint` = 40 000 (:330, [AUTHORED-DRAFT], calibration entry 1), added to compatibility (`CampaignRun.CurrentPreference`, :1161-1163).
- **The preference model clamps compatibility at 100** before exponentiating (`PreferenceModel.PersuadedShares`, PreferenceModel.cs:54; `ElectionScales.Clamp`, ElectionTypes.cs:24).
- **Saturation (§846):** pressure is counted per person reached, so at a national audience the parties close past the ceiling. Measured: 115-1265 before the clamp in every US run; Germany 8 of 9 parties; Sweden 2 of 8. Once past it, the clamp pins the result whatever a campaign does.
- **Measured (GA-5-1, `Tools/audit/area5_campaign.plan` h1-h3, `area5b_paced.plan` n1-n5):** every mix leaves the player's share within 0.1 point of an idle player's.
- **Turnout:** enthusiasm and the ground game's mobilisation (`GotvModel`, [AUTHORED-DRAFT] `MobilizationScale`) reach no live count. `ElectionDay.Count` is Editor-only (inventory D-19); q2 rules turnout on in play, under GATE-18.
- **The run-up's purchases** (`PreCampaignRun`: OFFICE 100 000, POLL 120 000, TV BUY 500 000, five hires at 1 800 a day) took 1.08 of the 2.4 million chest for nothing measured. The TV BUY is never charged and moves only the AI's pace reserve (SD-33).

**(2) Form.**
- **The campaign leaves compatibility alone and moves shares directly,** as the record shift does. Compatibility is positioning again, and nothing the campaign does passes through the clamp.
- **Per party and per contact class** (personal: rally, town hall, doors; impersonal: TV, digital, post, social; earned: interview, announcement), the chain's EXPOSURE (people who noticed) accumulates over the campaign.
- **Coverage** c = 1 − exp(−exposures / electorate): each voter is persuadable once, so repeated contact covers the same voters (the DERIVED Poisson arithmetic). The premise that a second contact adds nothing is [AUTHORED-DRAFT].
- **The persuasion effect** on party p's share is Σ_class CACE_class(timing) × c_class,p × q_p.
  - q_p is the chain's relevance × credibility as a ratio to the electorate-wide mean, so the chain still decides WHERE an effect lands and zero anywhere stays zero (§42's invariant). The normalisation is [AUTHORED-DRAFT].
  - Attack messages enter with the sign against the target.
  - The effect is absorbed by the other parties as the record's is.
- **Timing (Kalla & Broockman's own split).**
  - Contact within the eight-week campaign takes the close-to-election CACE.
  - Contact in the run-up takes the early CACE, which decays: it shows in the run-up's polls and is gone by polling day.
  - A TV buy's effect lasts its week (Gerber et al.).
- **5b, mobilisation:** a contacted supporter's turnout rises by the turnout CACE at the electorate's base-turnout band. The share effect is the extra votes over the votes cast (DERIVED). It lands with q2's turnout in play (Q6).
- **Each run-up purchase**, with Code's reading (Q7 rules the table):

| purchase | what it would buy, sourced | Code recommends |
|---|---|---|
| TV BUY (500 000) | an early TV effect of its week, decayed a week later [GGGS11]: it moves the run-up's polls, not the count | keep, CHARGED (SD-33's fix), priced by its weekly reach, the HQ saying the effect fades; remove if Elias wants no purchase without a count effect |
| POLL (120 000) | information: the ± its sample buys (`PollingSystem.MarginOfErrorPp`), measured by `ChainBandHarness`'s band width | keep: its effect is a measurement, not votes |
| OFFICE (100 000) | volunteers' hours, hence doors, hence 5b's turnout CACE | keep only once 5b is live and an office's contacts are counted; until then removed from the run-up |
| the five hires | the field organiser's volunteer hours (doors, 5b); the manager gates the TV plan; the pollster, media and digital roles have no sourced effect of their own | keep the field organiser with 5b and the manager as a gate; the other three go to q4's list |

**(3) Coefficients (CACE = effect among the contacted, in points of vote share or turnout).**
- **Persuasion close to election day: SOURCED** [KB18], "general elections … treatments … delivered within 2 months of election day".
  - All contact: "the most optimistic estimate that could be warranted from the literature is that campaign contact persuades about 1 in 175 voters, but that our best guess is that it persuades about 1 in 800 voters" (0.125 points best, 0.57 upper).
  - Personal contact: "0.58 percentage points, with a 95% confidence interval ranging from -0.50 to 1.66".
  - Impersonal contact: 0.28 [−0.27, 0.83]. Both are the pooled rows of Figure 4, "Effects Within 2 Months of Election Day With Original Studies, By Contact Type", panels (a) and (b).
  - Proposed: the best estimate as the effect, the upper limit as the bound the check holds.
- **Persuasion early, measured immediately: SOURCED** [KB18].
  - "in the subset of studies in which treatment is delivered long before election day and its effects are measured immediately, the effects … are clearly positive on average". Excluding Shaw, Blunt & Seaborn: "an average effect of 3.16 percentage points with a 95% confidence interval from 1.34 to 4.99".
  - "Panel 1(c) shows that in the two existing studies that examined whether these initial positive effects persisted, they were found to decay."
- **TV, early: SOURCED** [GGGS11].
  - "the current week's advertising raises Perry's vote share by 4.73 percentage points per 1,000 GRPs (SE = 1.42)"; a week later "the effects of these ads have receded to -0.17 percentage points (SE = 1.42)".
  - "One GRP is equal to 1% of the viewing audience".
- **Turnout (5b): SOURCED, from a draft the authors mark "[EARLY DRAFT: NOT FOR CITATION]"** [GG14]. The published chapter and *Get Out the Vote* are BILLED (§8).
  - Door-to-door canvassing, random-effects meta-analysis by the control group's turnout: under 30 % 4.6 [2.4, 6.9]; 30-50 % 6.2 [4.2, 8.2]; 50-70 % 1.4 [−1.2, 4.0]; **70 % and higher 0.6 [−1.5, 2.8]** (Table 4). The higher band applies: Sweden 84.21 %, Valmyndigheten, already on disk.
  - Phone banks: "one additional voter for every 280 people they spoke with" (2002) and "one vote was generated per fifty-five completed calls" (2004).
  - Kalla & Broockman's own 2016 canvass is a cross-check: "nearly 2 percentage points (p = 0.04)" on turnout.
- **The transfer from US experiments to European multiparty elections: [AUTHORED-DRAFT].** Kalla & Broockman cite UK and Italian candidate-canvassing work as "more muted". Calibration-list entry text, replacing entry 1: *"q24's campaign effects: Kalla & Broockman's and Green & Gerber's US CACEs applied to every country, per contact class and timing; persuadable once (coverage 1 − e^(−exposure/electorate)); relevance × credibility normalised to the electorate mean. Look for in play: after a hard campaign, did the party move by a fraction of a point, and could the player see why it was not more?"*
- `CampaignPressure.PersuasionPerCompatibilityPoint` and `EnthusiasmPerTurnoutPoint` leave the vote path; calibration entry 1 is retired with them.

**(4) Measured.**
- **Runs:**
  - GA-5-1's own: h1-h3, n1-n5.
  - The US staging of §846: `UsCampaignGeographyCheck`, re-run.
  - `CampaignAiHarness` and `ChainBandHarness`, each re-declared with its new digest.
  - The idle set of §3 (the AI's campaigns run in every idle game).
- **Pass:**
  - (a) the player's full campaign moves its own share by a non-zero amount;
  - (b) every party's campaign effect lies inside Σ CACE_upper × coverage at the close, printed per party;
  - (c) the campaign writes no compatibility, so the count of compatibility values above 100 entering `PreferenceModel.PersuadedShares` from a campaign is zero in every run, the US's included;
  - (d) a run-up TV buy's effect on the count is zero, and on its week's poll it is the sourced size within its SE;
  - (e) §3's idle tolerance still holds, because the AI campaigns now move the idle count by their bounded effects, not by the clamp's arithmetic.
- **Honest expectation, stated before the run:** by the sources, a national campaign moves its party by tenths of a point, not points. The ruling asks for that.

**(5) Families.** None in the no-policy dump (no campaign is staged there). The campaign harnesses' declared digests move and are re-declared with their own explanation.

**(6) Risks.**
- The AI personalities' choices (`CampaignAi`'s expected value in compatibility points) must be re-unitted to share points, or they choose on a dead scale. This is part of 5a's size.
- The night's attribution ledger (GA-4-4's 20.8 % baseline) must read the new path, which AF-23 already owns.

---

## 6. q5 - events move salience. Size **M** (with a data fetch).

**(1) Today.**
- **Events act through the economy and approval only** (`EventSystem.ApplyEvent`, EventSystem.cs:460). An `EconomicEvent` carries a GDP shock, an inflation shock and an approval effect, and its `Tag` is "never read by the model" (:17).
- **Salience is a constant per country:** Eurobarometer 105 (Spring 2026), Germany's snap campaign EB102 (`ElectionsData/salience/issue_salience.md`). It is set once in the campaign staging (`LiveCampaignSetup`, :219-231, :314-320) and in the stance model (`StanceModel.SalienceOf`, StanceModel.cs:177).
- No crime or war event exists (GA-0-25).

**(2) Form.**
- **Salience becomes a country's stored vector,** seeded from the sourced wave.
- **An event of a tag moves the issue its tag names** (Commodity/Financial → Economy, Health → Healthcare, Political → … stated per tag, DECLARED) by a jump that decays back to the seed. The vector is held within the survey's own scale; Eurobarometer's two-mention shares sum to about 2, stated.
- **Its readers:** the campaign chain's relevance (`CampaignRun.Setup.TrueSalience`) and the stance model's opinion cost. A later issue-weighted vote reads it too; none is built here.
- A crime event and a war or security event join the pool, each with its own analogue line, as the pool's P4-D2 rule requires.

**(3) Coefficients.** **FITTED,** with n and intervals: the jump per event class and its decay, from Eurobarometer's QA3 "most important issue" series around dated real events of each class. For example: immigration in 2015 (DE), rising prices 2022, the pandemic 2020, all EU27 waves twice a year. The series are BILLED until fetched in this piece (GESIS / the EB trend annexes; §8). Where a class has no dated analogue in the series, its jump is [AUTHORED-DRAFT] on the list.

**(4) Measured.**
- **Runs:** the fuzz set (events fire in every run) and area9's events plan (`Tools/audit/area9_events.plan`).
- **Pass:**
  - each fired event moves its issue by its class's fitted jump, logged;
  - the vector returns within the fitted half-life;
  - no salience leaves [0, 1] per issue;
  - the stance model's and the campaign's readings move with it, and the idle count's tolerance (§3 (a)) holds.

**(5) Families.** `aloop-salience`: the stance model reads salience in the AI's bill votes, so the no-policy dump can move wherever an event fires. Dumped and explained.

**(6) Risks.**
- The event-to-issue map is a DECLARED table that only play judges.
- Sweden's top EB105 issue ("threats to democracy") still has no slot (the salience file's W-F3 note); events cannot fix that.

---

## 7. The order, one at a time, and q4

| order | piece | size | moves the no-policy dump? | lands when |
|---|---|---|---|---|
| 1 | q34 levy/removal | S | expected no (proved) | first: it is q37's input |
| 2 | q37 tax term | M | yes, `aloop-tax` | after 1; approval must be right before it reaches the vote |
| 3 | q15 approval → vote | M | no (proved) | after 2 |
| 4 | q13 deficits → vote | M | no (proved) | after 3 (shared plumbing; ParlGov fetched) |
| 5a | q24 persuasion | L | no | after 4 (or beside it: it touches no shared file but the record's absorption) |
| 5b | q24 mobilisation | M | no | with GATE-18's q2 (Q6) |
| 6 | q5 salience | M | yes, `aloop-salience` | last (EB series fetched) |
| 7 | q4 the list | S + builds | per build | after 5a |

- Each piece is one commit series on its own green bars, with its readings recorded under its own § (what changed · the evidence line · the commit).
- Elias's ORDER places A-LOOP's plan third. AF-17 (the one-offs in the year the player reads) and AF-9 (fund terms to the vote) are fixes ahead of it, and q13 reads AF-17's balance.

**q4 - how the list of absent campaign actions with a sourced effect is made** (after 5a, put to Elias as one table):
1. **Enumerate** every action ELECTIONS_CAMPAIGN_SPEC names that the game refuses or lacks:
   - the run-up's refusals (`PreCampaignRun.Refusal`: FUNDRAISE, POLICY, TRAIN, STRATEGY);
   - the absent verbs of inventory D §2 and GA-0-23: attack and defend, debate choices, regional tours, office siting, targeting, outlets, staff roles, buying polls' breakdowns, the swing index;
   - §5's three hires with no effect.
2. **For each, search the field-experiment evidence in a fixed order:**
   - Kalla & Broockman's 49 studies by mode (their Figure 1 and online appendix, stored);
   - Green & Gerber by tactic (the stored overview; the book BILLED);
   - Gerber et al. for broadcast;
   - then one named search per remaining action (for example field offices; debates).
   - Each row records: the action, its contact class, the sourced effect (CACE, CI, outcome: vote choice or turnout, timing), the source's id and digest, and BUILD or CUT.
3. **The rule.**
   - An action with a sourced effect on vote choice or turnout is built on 5a/5b's path.
   - So is a resource that sourced actions consume (money, volunteers, hours): fundraising buys contacts, never votes.
   - An action with none is CUT from the spec: its text moves to the spec's own "cut by q4" section with the reason, nothing deleted unmigrated.

---

## 8. Sources: fetched, and BILLED

Fetched 2026-10-18 with `curl -sL -A "Mozilla/5.0 …"`, stored unaltered under `docs/reference/raw/aloop/`; digests in its `SHA256SUMS.txt`. PDF quotes come from `pdftotext` of the stored files (layout and raw modes, cross-checked where a table's columns interleave). Nothing is typed from memory.

| id | what | file | note |
|---|---|---|---|
| [BD06] | Brender & Drazen, "How Do Budget Deficits and Economic Growth Affect Reelection Prospects? Evidence from a Large Cross-Section of Countries", Foerder Institute WP 13-2006 (Aug 2006), Tel Aviv University | `tau_foerder_13-2006_brender_drazen.pdf` | the tables used (Table 2, Table 5, the p. 15 reading) |
| [BD05] | the same, NBER Working Paper 11862 (2005) | `nber_w11862.pdf` | the same p. 15 sentences; kept as the earlier version |
| [CR-AER] | Crossref record of the published article, AER 98(5) 2203-2220, doi 10.1257/aer.98.5.2203, with its abstract | `crossref_10_1257_aer_98_5_2203.json` | the published direction |
| - | the AER PDF: HTTP 403 | `aea_pdf_10_1257_aer_98_5_2203_HTTP403.html` | the refusal, kept |
| - | UMD publication page | `umd_publication_bd2008.html` | no PDF linked |
| [KB18] | Kalla & Broockman, "The Minimal Persuasive Effects of Campaign Contact in General Elections: Evidence from 49 Field Experiments", APSR 112(1) 148-166 - the publisher's typeset PDF (its own stamp: downloaded from cambridge.org at Stanford GSB, 13 May 2018), from a gwern.net mirror of web.stanford.edu | `gwern_mirror_stanford_kalla_broockman.pdf` | Cambridge serves its HTML page to curl in place of the PDF |
| [KB18-A] | its online appendix (Cambridge static) | `cambridge_S0003055417000363sup001_kalla_broockman_2018_appendix.pdf` | for q4's per-study rows |
| - | Cambridge landing page and Crossref record | `landing_10_1017_S0003055417000363.html`, `crossref_10_1017_S0003055417000363.json` | the abstract; "Open access" |
| [GG14] | Gerber & Green, "Field Experiments on Voter Mobilization: An Overview of a Burgeoning Literature", 5 Apr 2014, NBER conference paper, marked "[EARLY DRAFT: NOT FOR CITATION]" | `nber_conf_f80841_gerber_green_gotv_overview.pdf` | Table 4 used, its caveat carried on every figure |
| [GGGS11] | Gerber, Gimpel, Green & Shaw, "How Large and Long-lasting Are the Persuasive Effects of Televised Campaign Ads?", APSR 105(1) 135-150, from a gwern.net mirror; Crossref record beside it | `gwern_mirror_2011_gerber_gimpel_green_shaw_tv_ads.pdf`, `crossref_10_1017_S000305541000047X.json` | the TV effect and its decay |
| [LBS00] | Lewis-Beck & Stegmaier, "Economic Determinants of Electoral Outcomes", Annu. Rev. Polit. Sci. 3 (2000) 183-219, from a mirror (almendron.com) of the Annual Reviews PDF (its own stamp: downloaded by University of Arizona Library on 01/10/09) | `almendron_mirror_economicdeterminants.pdf` | Tables 1, 2, 4, 7, 10 used: all SECONDARY reports of the originals |
| [NP94] | Nannestad & Paldam, "The VP-function: A survey of the literature on vote and popularity functions after 25 years", Public Choice 79 (1994) 213-245 - Springer's abstract page | `springer_10_1007_BF01047771.html` | abstract only |
| [LBS13] | Lewis-Beck & Stegmaier, "The VP-function revisited", Public Choice 157 (2013) 367-385 - Springer's abstract page | `springer_10_1007_s11127-013-0086-6.html` | abstract only |

**BILLED (what is missing, and what would close it):**
- **The AER 2008 printed tables** (pubs.aeaweb.org answers 403). The 2006 working paper's tables stand in, and its text matches the 2005 NBER version's. A library copy would close it.
- **Brender & Drazen's replication data** (openICPSR 113276, login), for q13's support bound.
- **The ParlGov election table** (parlgov.org), for q13's σ_v. Fetched in piece 4.
- **Nannestad & Paldam 1994 and Lewis-Beck & Stegmaier 2013, full texts** (Springer, paywalled). Abstracts only. Would give a European approval coefficient for Q2, if either tabulates one.
- **Lewis-Beck & Tien 1996, Tufte 1978, Lewis-Beck 1995 originals.** Their figures are LB&S 2000's tabulation, used as SECONDARY.
- **Green & Gerber, *Get Out the Vote* (Brookings), and Gerber & Green's published handbook chapter (2017).** The 2014 draft stands in, with its own "not for citation" caveat. The mail, TV and digital turnout rows for 5b come from the book.
- **A presidential approval series of record** (Gallup), for R-US20's (c) on US-13's record.
- **Eurobarometer QA3 trend series** (GESIS or the EB annexes), for q5's fitted jumps and decay. Fetched in piece 6.
- **The grievance asymmetry's magnitude** (Nannestad & Paldam's micro study, 1997), for q37's mirror. Until it is fetched the mirror stays [AUTHORED-DRAFT].

---

## 9. For Elias: seven questions, each with Code's recommendation

1. **Q1 (q15): which approval reaches the vote?**
   - (a) Approval net of its three economy terms (growth, unemployment, inflation), because the economy already votes through Duch & Stevenson. **Recommended.**
   - (b) Approval whole, which counts the economy partly twice.
2. **Q2 (q15): the coefficient outside the USA.**
   - (a) Transfer Lewis-Beck & Tien's 0.16 as [AUTHORED-DRAFT]. **Recommended.**
   - (b) France's Assembly figure, 0.38.
   - (c) No approval term until a European estimate is fetched (BILLED).
3. **Q3 (q15, risk): approval's own persistence.**
   - Approval keeps 95 % of a shock a year; the popularity functions measure 0.79-0.84 a quarter.
   - **Recommended:** leave approval's reversion out of A-LOOP (the vote reads the change over the term, which bounds it), and rule the reversion as its own model family later.
   - Alternative: adopt the sourced persistence now, as a family across every country.
4. **Q4 (q13): Brender & Drazen measure re-election probability; the game counts shares.**
   - **Recommended:** convert through σ_v, the FITTED spread of incumbent vote swings.
   - Alternative: read their probability points as share points, which has no premise behind it.
5. **Q5 (q13): debt.**
   - **Recommended:** debt reaches the vote through the deficits that build it, as Brender & Drazen measure.
   - Alternative: a separate debt-level or rating term, which needs a source not yet found (BILLED).
6. **Q6 (q24): mobilisation.**
   - **Recommended:** 5b lands with GATE-18's q2 (turnout on in play), since a turnout effect has no live count to reach before it.
   - Alternative: bring q2 forward into A-LOOP.
7. **Q7 (q24): the run-up purchases.** **Recommended:** §5's table:
   - TV BUY kept and charged, with its fading effect on the polls;
   - POLL kept as information;
   - OFFICE out until 5b counts its contacts;
   - three hires to q4.
   - Alternative: remove every purchase with no effect on the count.

**Declared here, standing unless Elias changes them:** the pass thresholds of §1-§6 (notably §3 (a): the idle count no worse than 0.5 point mean absolute share miss per country), and the [AUTHORED-DRAFT] entries' texts above.

**RULED (Elias's ruling J1, 2026-10-19; installed `COMPLETED.md` §868; verbatim in `docs/RULINGS_2026-10-19.md`):**
- **Q1 (a):** approval net of its three economy terms reaches the vote.
- **Q2 (a):** Lewis-Beck & Tien's 0.16, transferred as [AUTHORED-DRAFT]. A European estimate is BILLED to replace it (§8: Nannestad & Paldam 1994, Lewis-Beck & Stegmaier 2013).
- **Q3 as recommended:** approval's reversion stays out of A-LOOP. It becomes the first of ORDER step 7's model families (`POLISIM_FEATURE_LIST.md`, MF-13), at the sourced 0.79-0.84 a quarter.
- **Q4:** Brender & Drazen's probability converts through σ_v. **Q5:** debt reaches the vote through the deficits that build it. **Q6:** 5b lands with GATE-18's q2. **Q7:** §5's table.
- **The declared pass thresholds of §1-§6 stand.**
- **When:** A-LOOP builds as ORDER step 3, after step 2's remaining fixes (AF-8, AF-12, AF-6, AF-7, AF-36 and J8's fork fix, FORK-1), one piece at a time in §7's order. q18's factors (GATE-18) are fitted after A-LOOP lands, so the fit absorbs it.
