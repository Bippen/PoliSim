using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// THE VALIDATION BAR for every Continuous Time phase, run BEFORE the scenario matrix:
    /// *"simulate 121 consecutive days and confirm the result is within ±3-5% of what the existing,
    /// already-validated single turn-level step produces for the same inputs."*
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.AggregationEquivalenceCheck.Run -logFile &lt;path&gt;`
    ///
    /// Phase 1 covers Sectors and Infrastructure. Both should come out EXACT rather than merely inside
    /// tolerance, because both translations were chosen to be algebraically equivalent rather than
    /// approximately so — a residual above float noise means a constant took the wrong shape.
    ///
    /// **Phase 3 is the first section where a residual is EXPECTED rather than suspicious**, and reading
    /// it as a bug would be the mistake. Its flows are exact by construction (DaysPerTurn × flow/DaysPerTurn), but the
    /// daily path charges interest against a debt stock that is itself moving daily and re-reads that
    /// stock through GetFiscalReactionMultiplier — a within-period feedback loop the turn form could not
    /// have had. A small drift is that loop; a LARGE one means a component took the wrong shape.
    /// </summary>
    public static class AggregationEquivalenceCheck
    {
        private const float TolerancePercent = 3f;

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1: the equivalence gate - fold any audit error into the exit (defensive; this path uses AdvanceDay, not AdvanceTurn, so it cannot host an ATTRIB today, but arming is free).
            int passed = 0, total = 0;

            // SELF-TEST FIRST: two independently-built worlds must start identical, or every comparison
            // below is measuring world-construction noise rather than the translation.
            World a = WorldFactory.CreateDefault();
            World b = WorldFactory.CreateDefault();
            Country ca = a.GetCountry(CountryId.Sweden);
            Country cb = b.GetCountry(CountryId.Sweden);
            bool sameStart = Mathf.Approximately(ca.Sectors[0].OutputShareOfGdp, cb.Sectors[0].OutputShareOfGdp)
                && Mathf.Approximately(ca.InfrastructureAssets[0].ConditionIndex, cb.InfrastructureAssets[0].ConditionIndex);
            Debug.Log($"SELFTEST two fresh worlds identical at start -> {(sameStart ? "OK" : "BROKEN - results below are void")}");

            // --- SECTORS -----------------------------------------------------------------------------
            // Driven OFF baseline so there is a real gap to close: without a policy offset every stat
            // already sits at its target and both paths trivially agree, which would prove nothing.
            foreach (Sector s in ca.Sectors) { s.SubsidyLevel = 90f; }
            foreach (Sector s in cb.Sectors) { s.SubsidyLevel = 90f; }

            MacroSystem.ApplySectorEffects(ca);                                        // one turn step
            for (int d = 0; d < SimulationManager.DaysPerTurn; d++)                     // DaysPerTurn daily steps
            {
                MacroSystem.ApplySectorEffectsDaily(cb);
            }

            for (int i = 0; i < ca.Sectors.Count; i++)
            {
                total += 3;
                passed += Compare($"Sector[{ca.Sectors[i].Type}].Output", ca.Sectors[i].OutputShareOfGdp, cb.Sectors[i].OutputShareOfGdp) ? 1 : 0;
                passed += Compare($"Sector[{ca.Sectors[i].Type}].Employment", ca.Sectors[i].EmploymentShare, cb.Sectors[i].EmploymentShare) ? 1 : 0;
                passed += Compare($"Sector[{ca.Sectors[i].Type}].Metric", ca.Sectors[i].SectorMetric, cb.Sectors[i].SectorMetric) ? 1 : 0;
            }

            // --- INFRASTRUCTURE ----------------------------------------------------------------------
            // Decay only, with no investment: that is the part that moved to daily granularity, and
            // isolating it is what makes a failure attributable to the decay translation specifically.
            World c = WorldFactory.CreateDefault();
            World d2 = WorldFactory.CreateDefault();
            Country cc = c.GetCountry(CountryId.Sweden);
            Country cd = d2.GetCountry(CountryId.Sweden);
            var noSpending = PolicyDecision.None();

            MacroSystem.ApplyInfrastructureCondition(cc, noSpending);                   // one turn step
            for (int i = 0; i < SimulationManager.DaysPerTurn; i++)                     // DaysPerTurn daily steps
            {
                MacroSystem.ApplyInfrastructureConditionDaily(cd);
            }

            for (int i = 0; i < cc.InfrastructureAssets.Count; i++)
            {
                total++;
                passed += Compare($"Infrastructure[{cc.InfrastructureAssets[i].Type}].Condition",
                    cc.InfrastructureAssets[i].ConditionIndex, cd.InfrastructureAssets[i].ConditionIndex) ? 1 : 0;
            }

            // --- PHASE 2: Labor Market and Crime & Justice -------------------------------------------
            // Driven off baseline the same way, and via the same policy dials a player would move, so the
            // targets differ from current state and there is a real gap for both paths to close.
            World e = WorldFactory.CreateDefault();
            World f = WorldFactory.CreateDefault();
            Country ce = e.GetCountry(CountryId.Sweden);
            Country cf = f.GetCountry(CountryId.Sweden);
            foreach (Country x in new[] { ce, cf })
            {
                x.PoliceFundingLevel = 80f;
                x.SentencingSeverity = 20f;
                x.BailReformLevel = 75f;
                x.DrugPolicyLevel = 25f;
                x.RetrainingProgramLevel = 85f;
            }

            // Turn path: one step each, in AdvanceTurn's documented order.
            MacroSystem.ApplyLaborForceParticipationRate(ce);
            MacroSystem.ApplyOrganizedCrimeIndex(ce);
            MacroSystem.ApplyCorruptionIndex(ce);
            MacroSystem.ApplyCrimeIndex(ce);
            MacroSystem.ApplyCrimeEffects(ce);
            MacroSystem.ApplyPrisonPopulationRate(ce);

            // Daily path: DaysPerTurn steps, same order preserved.
            for (int i = 0; i < SimulationManager.DaysPerTurn; i++)
            {
                MacroSystem.ApplyLaborForceParticipationRateDaily(cf);
                MacroSystem.ApplyOrganizedCrimeIndexDaily(cf);
                MacroSystem.ApplyCorruptionIndexDaily(cf);
                MacroSystem.ApplyCrimeIndexDaily(cf);
                MacroSystem.ApplyCrimeEffectsDaily(cf);
                MacroSystem.ApplyPrisonPopulationRateDaily(cf);
            }

            total += 6;
            passed += Compare("LaborForceParticipationRate", ce.State.LaborForceParticipationRate, cf.State.LaborForceParticipationRate) ? 1 : 0;
            passed += Compare("OrganizedCrimeIndex", ce.State.OrganizedCrimeIndex, cf.State.OrganizedCrimeIndex) ? 1 : 0;
            passed += Compare("CorruptionIndex", ce.State.CorruptionIndex, cf.State.CorruptionIndex) ? 1 : 0;
            passed += Compare("CrimeIndex", ce.State.CrimeIndex, cf.State.CrimeIndex) ? 1 : 0;
            passed += Compare("PrisonPopulationRate", ce.State.PrisonPopulationRate, cf.State.PrisonPopulationRate) ? 1 : 0;
            passed += Compare("BusinessConfidence (crime drift)", ce.State.BusinessConfidence, cf.State.BusinessConfidence) ? 1 : 0;

            // --- PHASE 3: the money resolution ---------------------------------------------------------
            // Unlike Phases 1-2 this one is NOT exact by construction, and is not expected to be. The
            // daily path recharges interest against a debt stock that is itself moving daily, and
            // GetFiscalReactionMultiplier re-reads the debt ratio each day - a within-period feedback loop
            // the turn form structurally could not have. That drift is the thing being measured here, so
            // both paths are given the IDENTICAL plan and an identical SWF return, leaving the feedback as
            // the only difference between them.
            //
            // Two countries, deliberately: Sweden owns a sovereign wealth fund (so the contribute/earn/
            // clamp/draw sequence is exercised) and Germany does not (so a failure can be attributed to
            // the plain revenue/spending/debt path rather than to the fund).
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany })
            {
                var turnGo = new GameObject($"AggEq_Turn_{id}");
                var dailyGo = new GameObject($"AggEq_Daily_{id}");
                World turnWorld = WorldFactory.CreateDefault();
                World dailyWorld = WorldFactory.CreateDefault();
                SimulationManager turnSim = turnGo.AddComponent<SimulationManager>();
                SimulationManager dailySim = dailyGo.AddComponent<SimulationManager>();
                turnSim.SetWorld(turnWorld);
                dailySim.SetWorld(dailyWorld);

                Country turnCountry = turnWorld.GetCountry(id);
                Country dailyCountry = dailyWorld.GetCountry(id);

                // The plan both paths spend, computed here rather than read out of either simulation, so
                // neither can quietly be measured against its own answer. Every country except the USA
                // uses the legacy baseline mechanic, so G is its structural share of GDP and Mandatory is
                // zero; a flat 2% period return stands in for the random draw.
                float governmentSpending = turnCountry.State.GDP * (turnCountry.GovernmentSpendingRate / 100f);
                float mandatorySpending = 0f;
                float swfPeriodReturn = turnCountry.SovereignWealthFund != null
                    ? turnCountry.SovereignWealthFund.TotalAssets * 0.02f
                    : 0f;

                // Pass 5 (the tariff flow): the period's take at the seed rates, planned into both
                // paths as one more flow - a fixed period figure distributed linearly, so it is exact
                // by construction like mandatory spending, and it rides these rows at each country's
                // own seed take (Sweden 1.01, Germany 4.08) rather than a synthetic value.
                float tariffRevenue = TradeSystem.ComputeTariffRevenue(turnCountry, turnWorld);

                float turnBudgetBefore = turnCountry.State.Budget;
                float dailyBudgetBefore = dailyCountry.State.Budget;

                turnSim.ApplyPeriodFiscalStepForValidation(turnCountry, governmentSpending, mandatorySpending, swfPeriodReturn, tariffRevenue);
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)
                {
                    dailySim.AccrueDayForValidation(dailyCountry, governmentSpending, mandatorySpending, swfPeriodReturn, tariffRevenue);
                }

                // The DEBT STOCK and the BUDGET BALANCE, not the Budget level: Budget is a running total
                // whose seeded starting value both paths share, so comparing it directly would divide the
                // difference by a number neither path produced and flatter the result.
                total += 2;
                passed += Compare($"{id}.GovernmentDebt", turnCountry.State.GovernmentDebt, dailyCountry.State.GovernmentDebt) ? 1 : 0;
                passed += Compare($"{id}.BudgetBalance",
                    turnCountry.State.Budget - turnBudgetBefore, dailyCountry.State.Budget - dailyBudgetBefore) ? 1 : 0;

                if (turnCountry.SovereignWealthFund != null)
                {
                    total++;
                    passed += Compare($"{id}.SwfTotalAssets",
                        turnCountry.SovereignWealthFund.TotalAssets, dailyCountry.SovereignWealthFund.TotalAssets) ? 1 : 0;
                }

                Object.DestroyImmediate(turnGo);
                Object.DestroyImmediate(dailyGo);
            }

            // --- THE EROSION TERM (mechanism report, rulings R1-R3) ---------------------------------
            // WHAT IT ENUMERATES (rule 14): the −π·b stock erosion at a VIOLENT 8% inflation drive
            // (the Phase 5 precedent) on the two structural extremes - Italy (the highest debt
            // ratio, where the term is largest) and Sweden (a near-zero-debt SWF country, where the
            // erosion must stay negligible while the fund path runs). π is HELD (the validation
            // accruals never run the macro engine), so both forms see the identical constant rate.
            // EXPECTATION, stated up front per the directive: NOT exact-by-construction - the power
            // slice composes to the turn factor at constant π, but its interleaving with the daily
            // budgetBalance subtraction is an affine composition, the same within-period feedback
            // Phase 3's own drift budget exists for; the rows must land inside the standard 3%,
            // and a LARGE residual means the slice took the wrong shape. The Phase 3 rows above
            // also now exercise the term at each country's seed inflation - these rows exist to
            // exercise it at a rate no seed reaches.
            foreach (CountryId id in new[] { CountryId.Italy, CountryId.Sweden })
            {
                var turnGo = new GameObject($"AggEq_ErosionTurn_{id}");
                var dailyGo = new GameObject($"AggEq_ErosionDaily_{id}");
                World turnWorld = WorldFactory.CreateDefault();
                World dailyWorld = WorldFactory.CreateDefault();
                SimulationManager turnSim = turnGo.AddComponent<SimulationManager>();
                SimulationManager dailySim = dailyGo.AddComponent<SimulationManager>();
                turnSim.SetWorld(turnWorld);
                dailySim.SetWorld(dailyWorld);

                Country turnCountry = turnWorld.GetCountry(id);
                Country dailyCountry = dailyWorld.GetCountry(id);
                turnCountry.State.Inflation = 8f;
                dailyCountry.State.Inflation = 8f;

                float governmentSpending = turnCountry.State.GDP * (turnCountry.GovernmentSpendingRate / 100f);
                float swfPeriodReturn = turnCountry.SovereignWealthFund != null
                    ? turnCountry.SovereignWealthFund.TotalAssets * 0.02f
                    : 0f;

                turnSim.ApplyPeriodFiscalStepForValidation(turnCountry, governmentSpending, 0f, swfPeriodReturn, TradeSystem.ComputeTariffRevenue(turnCountry, turnWorld));
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)
                {
                    dailySim.AccrueDayForValidation(dailyCountry, governmentSpending, 0f, swfPeriodReturn, TradeSystem.ComputeTariffRevenue(dailyCountry, dailyWorld));
                }

                total++;
                passed += Compare($"{id}.GovernmentDebt@8%erosion", turnCountry.State.GovernmentDebt, dailyCountry.State.GovernmentDebt) ? 1 : 0;

                Object.DestroyImmediate(turnGo);
                Object.DestroyImmediate(dailyGo);
            }

            // --- THE MATURITY RATE-LAG (ruling R4) --------------------------------------------------
            // WHAT IT ENUMERATES (rule 14): the effective-rate reversion under a +4-point spot-rate
            // shock, on the two countries the mechanism's boundary runs between - Italy (spot-priced
            // issuance in the premium-loaded shared zone: the lag's actual target) and the USA
            // (override-anchored issuance: the CONTROL - its target ignores the spot drive by
            // construction, and the assert below checks the anchor held rather than assuming it).
            // r_eff is set to the PRE-DRIVE spot explicitly (letting the sentinel initialize AFTER
            // the drive would seed it at the driven rate and leave no gap to close). EXPECTATION:
            // the reversion itself telescopes at constant target, but the target carries the
            // premium, which moves with the daily ratio - Phase-3-class drift, inside the standard
            // tolerance; a large residual means the slice took the wrong shape.
            foreach (CountryId id in new[] { CountryId.Italy, CountryId.USA })
            {
                var turnGo = new GameObject($"AggEq_LagTurn_{id}");
                var dailyGo = new GameObject($"AggEq_LagDaily_{id}");
                World turnWorld = WorldFactory.CreateDefault();
                World dailyWorld = WorldFactory.CreateDefault();
                SimulationManager turnSim = turnGo.AddComponent<SimulationManager>();
                SimulationManager dailySim = dailyGo.AddComponent<SimulationManager>();
                turnSim.SetWorld(turnWorld);
                dailySim.SetWorld(dailyWorld);

                Country turnCountry = turnWorld.GetCountry(id);
                Country dailyCountry = dailyWorld.GetCountry(id);
                foreach (Country x in new[] { turnCountry, dailyCountry })
                {
                    x.EffectiveDebtInterestRate = x.CurrencyZone.InterestRate;
                    x.CurrencyZone.InterestRate += 4f;
                }

                float governmentSpending = turnCountry.State.GDP * (turnCountry.GovernmentSpendingRate / 100f);
                float swfPeriodReturn = turnCountry.SovereignWealthFund != null
                    ? turnCountry.SovereignWealthFund.TotalAssets * 0.02f
                    : 0f;

                turnSim.ApplyPeriodFiscalStepForValidation(turnCountry, governmentSpending, 0f, swfPeriodReturn, TradeSystem.ComputeTariffRevenue(turnCountry, turnWorld));
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)
                {
                    dailySim.AccrueDayForValidation(dailyCountry, governmentSpending, 0f, swfPeriodReturn, TradeSystem.ComputeTariffRevenue(dailyCountry, dailyWorld));
                }

                total += 2;
                passed += Compare($"{id}.EffectiveDebtRate@+4spot", turnCountry.EffectiveDebtInterestRate, dailyCountry.EffectiveDebtInterestRate) ? 1 : 0;
                passed += Compare($"{id}.GovernmentDebt@+4spot", turnCountry.State.GovernmentDebt, dailyCountry.State.GovernmentDebt) ? 1 : 0;

                if (id == CountryId.USA)
                {
                    // The override's fate, asserted not assumed: the USA's issuance target is its
                    // blended override (+ near-zero premium), so a +4 SPOT drive must leave its
                    // effective rate anchored near 3.3 - if this rate chased the spot the general
                    // lag would NOT subsume the override, and the boundary claim would be false.
                    total++;
                    passed += Assert("USA.EffectiveDebtRate ANCHORED (override subsumed)",
                        turnCountry.EffectiveDebtInterestRate < 4f && dailyCountry.EffectiveDebtInterestRate < 4f,
                        $"turn={turnCountry.EffectiveDebtInterestRate:F3} daily={dailyCountry.EffectiveDebtInterestRate:F3} - both must sit near the 3.3 blended anchor, not the +4-driven spot") ? 1 : 0;
                }

                Object.DestroyImmediate(turnGo);
                Object.DestroyImmediate(dailyGo);
            }

            // --- PHASE 4: Demographics --------------------------------------------------------------
            // Two countries with OPPOSITE demographic signs (Sweden grows, Germany shrinks), both
            // driven off baseline through the real policy dials (family pro-natal, immigration
            // restrictive) so the sensitivity non-conversions are exercised, not just the drifts.
            // The rate chain should land near-exact (linear drifts sum identically; DependencyRatio's
            // mid-period reads give a tiny path dependence); Population carries the Phase-3-class
            // EXPECTED residual - the daily path compounds through a rate that reverts daily.
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany })
            {
                World g1 = WorldFactory.CreateDefault();
                World g2 = WorldFactory.CreateDefault();
                Country cg = g1.GetCountry(id);
                Country ch = g2.GetCountry(id);
                foreach (Country x in new[] { cg, ch })
                {
                    x.FamilyPolicyLevel = 80f;
                    x.ImmigrationPolicyLevel = 20f;
                }

                MacroSystem.ApplyDemographicRates(cg);                                  // one turn step
                MacroSystem.ApplyPopulationGrowth(cg);
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)                 // daily steps
                {
                    MacroSystem.ApplyDemographicRatesDaily(ch);
                    MacroSystem.ApplyPopulationGrowthDaily(ch);
                }

                total += 8;
                passed += Compare($"{id}.NaturalBirthRate", cg.State.NaturalBirthRate, ch.State.NaturalBirthRate) ? 1 : 0;
                passed += Compare($"{id}.BirthRate", cg.State.BirthRate, ch.State.BirthRate) ? 1 : 0;
                passed += Compare($"{id}.DeathRate", cg.State.DeathRate, ch.State.DeathRate) ? 1 : 0;
                passed += Compare($"{id}.NaturalNetMigrationRate", cg.State.NaturalNetMigrationRate, ch.State.NaturalNetMigrationRate) ? 1 : 0;
                passed += Compare($"{id}.NetMigrationRate", cg.State.NetMigrationRate, ch.State.NetMigrationRate) ? 1 : 0;
                passed += Compare($"{id}.DependencyRatio", cg.State.DependencyRatio, ch.State.DependencyRatio) ? 1 : 0;
                passed += Compare($"{id}.PopulationGrowthRate", cg.State.PopulationGrowthRate, ch.State.PopulationGrowthRate) ? 1 : 0;
                passed += Compare($"{id}.Population", cg.State.Population, ch.State.Population) ? 1 : 0;
            }

            // --- PHASE 4: the bucket-divergence assert ----------------------------------------------
            // The multi-resolution buckets were built in Phase 0 for daily data and (finding, this
            // pass) never received a daily offer until History.Append moved to AdvanceDay. This
            // section is the first check that can fail if that plumbing regresses. WHAT IT
            // ENUMERATES (rule 14): a real 200-day sim (fresh world, no policy), then for SIX
            // daily-varying series - PovertyRate, DebtToGdpRatio, the daily-native
            // YouthUnemployment (Round 4 batch 1), RealWageIndex (batch 2), HousePriceIndex
            // (batch 3) and Productivity (batch R4-5), each asserted rather than assumed per the
            // batch directives - three assert
            // families:
            // cadence (Daily/Weekly/Monthly/Quarterly counts within one of 200/1, /7, /30, /91,
            // Daily capped at StatHistory.MaxEntries), variation (Daily holds at least two DISTINCT
            // consecutive values - the stat genuinely moves intra-week), and divergence (the Daily
            // series' last value differs from Quarterly's last accepted value - the resolutions no
            // longer mirror each other). It says nothing about bucket CONTENTS being the right
            // values (the trajectory matrix owns value correctness) and nothing about stats that
            // legitimately step per-turn (GDP stays turn-shaped until Phase 5). LifeExpectancy is
            // deliberately NOT here: in a no-policy run PovertyRate never exceeds its baseline, so
            // the target sits exactly at the seeded value and a flat daily series is CORRECT - the
            // variation assert would report that correctness as a plumbing failure. Its buckets ride
            // the same StatHistory.Append line as everything else; the matrix judges its values.
            // Gini (Round 4 batch 2) is excluded on the numerical variant of the same reason: its
            // slow reversion (PerDayReversion of 0.1 ~ 0.0009/day) times a no-policy target
            // wiggle lands the daily increment at float32 epsilon on a ~30-point value, so the
            // variation assert would measure rounding, not plumbing. RealWageIndex is IN - it
            // compounds daily by construction, the clearest daily-native series in the set.
            // HousingOverburden/Homeownership (Round 4 batch 3) are excluded on a THIRD variant:
            // their targets move only when the policy rate steps at a discrete meeting, so whether
            // a 200-day no-policy window shows variation depends on the central-bank calendar, not
            // on bucket plumbing - the assert would test the wrong thing. HousePriceIndex is IN,
            // same reasoning as RealWageIndex (compounds daily regardless of the rate path).
            // Productivity (Round 4 batch R4-5) is IN by the same compounding argument - pure
            // trend growth means strictly monotone dailies, the cleanest variation case possible.
            {
                var bucketGo = new GameObject("AggEq_Buckets");
                try
                {
                    World bw = WorldFactory.CreateDefault();
                    SimulationManager bSim = bucketGo.AddComponent<SimulationManager>();
                    bSim.SetWorld(bw);
                    const int BucketDays = 200;
                    for (int i = 0; i < BucketDays; i++)
                    {
                        bSim.AdvanceDay();
                    }

                    Country bc = bw.GetCountry(CountryId.Sweden);
                    foreach ((string name, MultiResolutionSeries series) in new (string, MultiResolutionSeries)[]
                             { ("PovertyRate", bc.History.PovertyRate), ("DebtToGdpRatio", bc.History.DebtToGdpRatio),
                               ("YouthUnemployment", bc.History.YouthUnemployment),
                               ("RealWageIndex", bc.History.RealWageIndex),
                               ("HousePriceIndex", bc.History.HousePriceIndex),
                               ("Productivity", bc.History.Productivity) })
                    {
                        total += 3;
                        int expectedDaily = Mathf.Min(BucketDays, StatHistory.MaxEntries);
                        bool cadence = Mathf.Abs(series.Daily.Count - expectedDaily) <= 1
                            && Mathf.Abs(series.Weekly.Count - Mathf.CeilToInt(BucketDays / 7f)) <= 1
                            && Mathf.Abs(series.Monthly.Count - Mathf.CeilToInt(BucketDays / 30f)) <= 1
                            && Mathf.Abs(series.Quarterly.Count - Mathf.CeilToInt(BucketDays / 91f)) <= 1;
                        passed += Assert($"Buckets[{name}].cadence", cadence,
                            $"D={series.Daily.Count} W={series.Weekly.Count} M={series.Monthly.Count} Q={series.Quarterly.Count}") ? 1 : 0;

                        bool varies = false;
                        for (int i = 1; i < series.Daily.Count; i++)
                        {
                            if (!Mathf.Approximately(series.Daily[i], series.Daily[i - 1])) { varies = true; break; }
                        }

                        passed += Assert($"Buckets[{name}].daily-variation", varies,
                            varies ? "distinct consecutive dailies found" : "every daily point identical - daily data is not reaching the buckets") ? 1 : 0;

                        bool diverges = series.Daily.Count > 0 && series.Quarterly.Count > 0
                            && !Mathf.Approximately(series.Daily[series.Daily.Count - 1], series.Quarterly[series.Quarterly.Count - 1]);
                        passed += Assert($"Buckets[{name}].resolution-divergence", diverges,
                            "Daily tail vs Quarterly last accepted") ? 1 : 0;
                    }
                }
                finally
                {
                    Object.DestroyImmediate(bucketGo);
                }
            }

            // --- PHASE 5: the core macro engine ---------------------------------------------------
            // Two countries with different profiles (USA healthy, Italy high-debt), both driven off
            // baseline (an 8% output shock plus a 2-point unemployment gap) so the identity has a
            // real gap to close and Okun/Phillips have real gaps to respond to. Both paths get the
            // IDENTICAL plan level and rate. Residual expectations, stated: the identity's affine
            // slice is exact at constant inputs but its attractor moves daily (PotentialGDP compounds
            // under it); Okun's daily growth sums differ from the turn growth at second order
            // (compounding vs sum); the expectations chain adapts along the intra-period path rather
            // than once at its end. All are the Phase 3 class - small by construction, real, and the
            // thing being measured. None of the seven compared quantities is near a zero crossing at
            // these drives, so the relative bar is honest here; the MATRIX is where the signed
            // near-zero quantities (TradeBalance, growth rates, budget balance) live, and those are
            // judged on TrajectoryDiffCheck's absolute column per the Phase 4 verdict.
            foreach ((CountryId id, float gdpShock, float unemploymentShock) in new (CountryId, float, float)[]
                     { (CountryId.USA, 0.92f, 2f), (CountryId.Italy, 0.92f, 2f),
                       (CountryId.USA, 0.98f, 0.5f), (CountryId.Italy, 0.98f, 0.5f) })
            {
                World m1 = WorldFactory.CreateDefault();
                World m2 = WorldFactory.CreateDefault();
                Country cm = m1.GetCountry(id);
                Country cn = m2.GetCountry(id);
                foreach (Country x in new[] { cm, cn })
                {
                    x.State.GDP *= gdpShock;
                    x.State.Unemployment += unemploymentShock;
                }

                float plannedG = cm.State.GDP * (cm.GovernmentSpendingRate / 100f);
                float rate = cm.CurrencyZone.InterestRate;

                // Turn path, AdvanceTurn's exact order.
                float gdpBefore = cm.State.GDP;
                MacroSystem.ApplyNationalAccounts(cm, plannedG, rate);
                MacroSystem.ApplyPotentialGdpGrowth(cm);
                float turnGrowth = (cm.State.GDP - gdpBefore) / Mathf.Max(gdpBefore, 1f) * 100f;
                MacroSystem.ApplyOkunsLaw(cm, turnGrowth);
                MacroSystem.ApplyPhillipsCurveInflation(cm);
                MacroSystem.ApplyInflationExpectations(cm.State);

                // Daily path, AdvanceDay's exact order, with the period-open unemployment as Okun's
                // fixed reversion reference (the shape that replaced the self-referencing form after
                // it failed this very bar - the failure is kept in ApplyOkunsLaw's own comment).
                float unemploymentAtOpen = cn.State.Unemployment;
                float gdpAtOpen = cn.State.GDP;
                float potentialAtOpen = cn.State.PotentialGDP;
                // Q2: the wage-growth gap anchored at period open (the fifth fixed reference) -
                // the LIVE form failed this very bar at 11.8% on the @8%shock unemployment row
                // (2026-08-18), the same divergence class the potential anchor fixed.
                float wageGapAtOpen = MacroSystem.RealWageGrowthGapPerTurnPercent(cn,
                    MacroSystem.ProductivityCycleGrowthPerTurnPercent(cn, cn.State.Unemployment));
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)
                {
                    float dayBefore = cn.State.GDP;
                    MacroSystem.ApplyNationalAccountsDaily(cn, plannedG, rate, potentialAtOpen, wageGapAtOpen);
                    MacroSystem.ApplyPotentialGdpGrowthDaily(cn);
                    float annualized = (cn.State.GDP - dayBefore) / Mathf.Max(gdpAtOpen, 1f) * 100f * SimulationManager.DaysPerTurn;
                    MacroSystem.ApplyOkunsLawDaily(cn, annualized, unemploymentAtOpen);
                    MacroSystem.ApplyPhillipsCurveInflation(cn);
                }

                // Expectations at the boundary, both regimes - a period stance, not a flow (the
                // measured failure of the daily form is recorded at MacroSystem's Phase 5 block).
                MacroSystem.ApplyInflationExpectations(cn.State);

                string label = $"{id}@{(1f - gdpShock) * 100f:F0}%shock";
                total += 5;
                passed += Compare($"{label}.GDP", cm.State.GDP, cn.State.GDP) ? 1 : 0;
                passed += Compare($"{label}.PotentialGDP", cm.State.PotentialGDP, cn.State.PotentialGDP) ? 1 : 0;
                passed += Compare($"{label}.Unemployment", cm.State.Unemployment, cn.State.Unemployment) ? 1 : 0;
                passed += Compare($"{label}.Inflation", cm.State.Inflation, cn.State.Inflation) ? 1 : 0;
                passed += Compare($"{label}.InflationExpectations", cm.State.InflationExpectations, cn.State.InflationExpectations) ? 1 : 0;

                // C and I are INFO, not counted: nothing reads them back (display decompositions),
                // and the two forms report different, individually-correct things - the turn form's
                // levels reflect the period-START GDP, the daily form's the day it is - so under a
                // within-period recovery the comparison measures the recovery, not a defect. Judged
                // here honestly rather than silently: the deviation should track GDP's own
                // within-period movement and nothing else.
                Compare($"{label}.Consumption (INFO)", cm.State.Consumption, cn.State.Consumption);
                Compare($"{label}.Investment (INFO)", cm.State.Investment, cn.State.Investment);
            }

            // --- ROUND 4 BATCH 1 (C3): youth unemployment and life expectancy ----------------------
            // WHAT IT ENUMERATES (rule 14): two countries with opposite seed profiles (Sweden holds
            // the highest youth-U seed at 22.5, USA the lowest life expectancy at 79.0), each driven
            // off baseline through the stats' actual INPUTS - a +3pt headline-unemployment shock
            // (youth-U target moves +6 via the 2x cyclicality) and a +5pt PovertyRate shock (life
            // expectancy target drops 0.4 years) - so both reversions have a real gap to close.
            // Inputs are then HELD (neither Okun nor the poverty system runs here), which makes both
            // stats' targets constant, and a constant-target PerDayReversion is EXACT by
            // construction: DaysPerTurn daily steps at 1-(1-s)^(1/DaysPerTurn) compound to precisely one turn step
            // at s. A residual above float noise therefore means the wrapper took the wrong shape -
            // this section carries no Phase-3-class expected drift. The live path-dependence (targets
            // that move daily under the real sim) is the matrix's to judge, not this section's.
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.USA })
            {
                World r1 = WorldFactory.CreateDefault();
                World r2 = WorldFactory.CreateDefault();
                Country cr = r1.GetCountry(id);
                Country cs = r2.GetCountry(id);
                foreach (Country x in new[] { cr, cs })
                {
                    x.State.Unemployment += 3f;
                    x.State.PovertyRate += 5f;
                }

                MacroSystem.ApplyYouthUnemployment(cr);                                 // one turn step
                MacroSystem.ApplyLifeExpectancy(cr);
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)                 // daily steps
                {
                    MacroSystem.ApplyYouthUnemploymentDaily(cs);
                    MacroSystem.ApplyLifeExpectancyDaily(cs);
                }

                total += 2;
                passed += Compare($"{id}.YouthUnemployment", cr.State.YouthUnemployment, cs.State.YouthUnemployment) ? 1 : 0;
                passed += Compare($"{id}.LifeExpectancy", cr.State.LifeExpectancy, cs.State.LifeExpectancy) ? 1 : 0;
            }

            // --- ROUND 4 BATCH R4-4: the Education competence term ----------------------------------
            // WHAT IT ENUMERATES (rule 14): the one model term a content batch added (ruling R3 - an
            // appointed Education minister's CompetenceBias subtracts from the youth-U reversion
            // target at point-of-use). One country suffices: the term is a constant target shift, so
            // it cannot interact with country structure beyond the baseline it shifts - Sweden,
            // whose 22.5 seed is the highest, driven with the same +3pt unemployment shock as the
            // R4-1 section so gap and term are both live at once. The appointment is picked
            // DETERMINISTICALLY by max CompetenceBias (GenerateCandidates shuffles, and two worlds
            // calling it in sequence would receive different orders from the shared Cabinet
            // stream). Inputs held; constant-target PerDayReversion stays EXACT by construction -
            // float noise only, no drift budget, same as every Round 4 section here.
            {
                World e1 = WorldFactory.CreateDefault();
                World e2 = WorldFactory.CreateDefault();
                Country cy = e1.GetCountry(CountryId.Sweden);
                Country cz = e2.GetCountry(CountryId.Sweden);
                foreach (Country x in new[] { cy, cz })
                {
                    CabinetMinister best = null;
                    foreach (CabinetMinister candidate in CabinetSystem.GenerateCandidates(CabinetPortfolio.Education))
                    {
                        if (best == null || candidate.CompetenceBias > best.CompetenceBias)
                        {
                            best = candidate;
                        }
                    }

                    x.CabinetMinisters[CabinetPortfolio.Education] = best;
                    x.State.Unemployment += 3f;
                }

                MacroSystem.ApplyYouthUnemployment(cy);                                 // one turn step
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)                 // daily steps
                {
                    MacroSystem.ApplyYouthUnemploymentDaily(cz);
                }

                total++;
                passed += Compare("Sweden.YouthU+EducationMinister", cy.State.YouthUnemployment, cz.State.YouthUnemployment) ? 1 : 0;
            }

            // --- ROUND 4 BATCH 2 (C2): Gini and the real wage index ---------------------------------
            // WHAT IT ENUMERATES (rule 14): two countries chosen for their structural opposites on
            // exactly these stats (USA: the [ESTIMATED] Gini outlier WITH a statutory minimum wage;
            // Sweden: near the equality floor WITHOUT one - so both MinimumWageImplemented branches
            // run), each driven through every input channel at once: +3pt unemployment (slack pushes
            // Gini up AND suppresses wage growth), +2pt inflation surprise over expectations (the
            // erosion channel), MeansTestedWelfare implemented at generosity 60 (the strongest
            // transfer pull), and the income-tax line raised 10 points over its seeded anchor (the
            // signed redistribution pull). Inputs HELD (no other system runs): Gini's constant-target
            // PerDayReversion and the wage index's constant-growth POWER SLICE are both EXACT by
            // construction, so this section carries no drift budget - float noise only, same as the
            // R4-1 section above. Live path-dependence is the matrix's to judge.
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.USA })
            {
                World w1 = WorldFactory.CreateDefault();
                World w2 = WorldFactory.CreateDefault();
                Country ct = w1.GetCountry(id);
                Country cu = w2.GetCountry(id);
                foreach (Country x in new[] { ct, cu })
                {
                    x.State.Unemployment += 3f;
                    x.State.Inflation = x.State.InflationExpectations + 2f;
                    foreach (WelfareProgram program in x.WelfarePrograms)
                    {
                        if (program.Type == WelfareProgramType.MeansTestedWelfare)
                        {
                            program.IsImplemented = true;
                            program.GenerosityLevel = 60f;
                        }
                    }
                    foreach (TaxLine line in x.TaxLines)
                    {
                        if (line.Type == TaxType.IncomeTax) { line.Rate += 10f; }
                    }
                }

                // Q5: BOTH regimes take the cycle from the PERIOD-OPEN unemployment - the turn form
                // computes it once from the state it opens at, and the daily form is handed the same
                // anchor every day. That is the equivalence claim under test for the new term: an
                // anchored driver telescopes exactly through the power slice, where a live one would
                // not (Q2's measured failure, avoided by construction here).
                float wageCycleAtOpen = MacroSystem.ProductivityCycleGrowthPerTurnPercent(ct, ct.State.Unemployment);
                float wageAnchorU = cu.State.Unemployment;

                MacroSystem.ApplyGini(ct);                                              // one turn step
                MacroSystem.ApplyRealWageIndex(ct, wageCycleAtOpen);
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)                 // daily steps
                {
                    MacroSystem.ApplyGiniDaily(cu);
                    MacroSystem.ApplyRealWageIndexDaily(cu, wageAnchorU);
                }

                total += 2;
                passed += Compare($"{id}.Gini", ct.State.Gini, cu.State.Gini) ? 1 : 0;
                passed += Compare($"{id}.RealWageIndex", ct.State.RealWageIndex, cu.State.RealWageIndex) ? 1 : 0;
            }

            // --- ROUND 4 BATCH 3 (C1): housing ------------------------------------------------------
            // WHAT IT ENUMERATES (rule 14): Sweden (tracks overburden, own currency zone) and the
            // USA (does NOT track it - the asymmetry ruling), each driven through both input
            // channels at once: the policy rate raised 2 points over the zone's epoch anchor (the
            // arc's first monetary coupling - overburden up, homeownership down, house-price growth
            // dragged) and HousingAssistance implemented at generosity 60 (relief on both reverting
            // stats). Inputs HELD: constant-target reversions and a constant-growth power slice are
            // EXACT by construction - float noise only, no drift budget, same as both prior
            // sections. PLUS the asymmetry assert: the USA's overburden must sit EXACTLY at its
            // untracked 0 after a full turn of daily steps under the same drives - the ruling enforced as a
            // check, not a comment.
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.USA })
            {
                World h1 = WorldFactory.CreateDefault();
                World h2 = WorldFactory.CreateDefault();
                Country cv = h1.GetCountry(id);
                Country cw = h2.GetCountry(id);
                foreach (Country x in new[] { cv, cw })
                {
                    x.CurrencyZone.InterestRate = x.CurrencyZone.HousingRateAnchor + 2f;
                    foreach (WelfareProgram program in x.WelfarePrograms)
                    {
                        if (program.Type == WelfareProgramType.HousingAssistance)
                        {
                            program.IsImplemented = true;
                            program.GenerosityLevel = 60f;
                        }
                    }
                }

                MacroSystem.ApplyHousingOverburden(cv);                                 // one turn step
                MacroSystem.ApplyHomeownership(cv);
                MacroSystem.ApplyHousePriceIndex(cv);
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)                 // daily steps
                {
                    MacroSystem.ApplyHousingOverburdenDaily(cw);
                    MacroSystem.ApplyHomeownershipDaily(cw);
                    MacroSystem.ApplyHousePriceIndexDaily(cw);
                }

                if (id == CountryId.USA)
                {
                    total++;
                    passed += Assert("USA.HousingOverburden UNMOVED (asymmetry ruling)",
                        cv.State.HousingOverburden == 0f && cw.State.HousingOverburden == 0f,
                        $"turn={cv.State.HousingOverburden} daily={cw.State.HousingOverburden} - both must be exactly the untracked 0") ? 1 : 0;
                }
                else
                {
                    total++;
                    passed += Compare($"{id}.HousingOverburden", cv.State.HousingOverburden, cw.State.HousingOverburden) ? 1 : 0;
                }

                total += 2;
                passed += Compare($"{id}.Homeownership", cv.State.Homeownership, cw.State.Homeownership) ? 1 : 0;
                passed += Compare($"{id}.HousePriceIndex", cv.State.HousePriceIndex, cw.State.HousePriceIndex) ? 1 : 0;
            }

            // --- ROUND 4 BATCH R4-5 (C5): productivity ----------------------------------------------
            // WHAT IT ENUMERATES (rule 14): the arc's simplest model - a constant-growth power
            // slice reading exactly one input (PotentialGrowthRate), which never moves in this
            // check, so there is nothing to drive and holding inputs is automatic. Two countries
            // chosen for the structural extremes the seed doc's own qualitative claims name:
            // Germany (the highest level, 94.54) and Poland (the lowest, 54.09, with the fastest
            // catch-up trend - the largest per-turn growth in the set, hence the largest possible
            // telescoping error if the slice took the wrong shape). Constant-growth power slices
            // are EXACT by construction - float noise only, no drift budget, same as every Round 4
            // section here.
            foreach (CountryId id in new[] { CountryId.Germany, CountryId.Poland })
            {
                World p1 = WorldFactory.CreateDefault();
                World p2 = WorldFactory.CreateDefault();
                Country cp = p1.GetCountry(id);
                Country cq = p2.GetCountry(id);

                // Q5: same anchored-cycle treatment as the wage rows above - the hoarding term is
                // the second consumer of the same anchor, so it gets the same equivalence claim.
                float prodCycleAtOpen = MacroSystem.ProductivityCycleGrowthPerTurnPercent(cp, cp.State.Unemployment);
                float prodAnchorU = cq.State.Unemployment;

                MacroSystem.ApplyProductivity(cp, prodCycleAtOpen);                     // one turn step
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)                 // daily steps
                {
                    MacroSystem.ApplyProductivityDaily(cq, prodAnchorU);
                }

                total++;
                passed += Compare($"{id}.Productivity", cp.State.Productivity, cq.State.Productivity) ? 1 : 0;
            }

            Debug.Log($"=== Phases 1-5 aggregation-equivalence: {passed} of {total} within {TolerancePercent}% (plus the bucket asserts) ===");
            CheckExit.Finish(passed == total ? 0 : 1);
        }

        private static bool Assert(string label, bool ok, string detail)
        {
            Debug.Log($"  {(ok ? "ok  " : "FAIL")} {label,-42} {detail}");
            return ok;
        }

        private static bool Compare(string label, float turnValue, float dailyValue)
        {
            float denominator = Mathf.Max(Mathf.Abs(turnValue), 0.0001f);
            float driftPercent = Mathf.Abs(dailyValue - turnValue) / denominator * 100f;
            bool ok = driftPercent <= TolerancePercent;
            Debug.Log($"  {(ok ? "ok  " : "FAIL")} {label,-42} turn={turnValue,9:F5}  daily={dailyValue,9:F5}  drift={driftPercent,7:F4}%");
            return ok;
        }
    }
}
