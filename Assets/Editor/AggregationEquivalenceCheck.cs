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
    /// it as a bug would be the mistake. Its flows are exact by construction (121 × flow/121), but the
    /// daily path charges interest against a debt stock that is itself moving daily and re-reads that
    /// stock through GetFiscalReactionMultiplier — a within-period feedback loop the turn form could not
    /// have had. A small drift is that loop; a LARGE one means a component took the wrong shape.
    /// </summary>
    public static class AggregationEquivalenceCheck
    {
        private const float TolerancePercent = 3f;

        public static void Run()
        {
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
            for (int d = 0; d < SimulationManager.DaysPerTurn; d++)                     // 121 daily steps
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
            for (int i = 0; i < SimulationManager.DaysPerTurn; i++)                     // 121 daily steps
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

            // Daily path: 121 steps, same order preserved.
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

                float turnBudgetBefore = turnCountry.State.Budget;
                float dailyBudgetBefore = dailyCountry.State.Budget;

                turnSim.ApplyPeriodFiscalStepForValidation(turnCountry, governmentSpending, mandatorySpending, swfPeriodReturn);
                for (int i = 0; i < SimulationManager.DaysPerTurn; i++)
                {
                    dailySim.AccrueDayForValidation(dailyCountry, governmentSpending, mandatorySpending, swfPeriodReturn);
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
            // ENUMERATES (rule 14): a real 200-day sim (fresh world, no policy), then for TWO
            // daily-varying series - PovertyRate and DebtToGdpRatio - three assert families:
            // cadence (Daily/Weekly/Monthly/Quarterly counts within one of 200/1, /7, /30, /91,
            // Daily capped at StatHistory.MaxEntries), variation (Daily holds at least two DISTINCT
            // consecutive values - the stat genuinely moves intra-week), and divergence (the Daily
            // series' last value differs from Quarterly's last accepted value - the resolutions no
            // longer mirror each other). It says nothing about bucket CONTENTS being the right
            // values (the trajectory matrix owns value correctness) and nothing about stats that
            // legitimately step per-turn (GDP stays turn-shaped until Phase 5).
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
                             { ("PovertyRate", bc.History.PovertyRate), ("DebtToGdpRatio", bc.History.DebtToGdpRatio) })
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

            Debug.Log($"=== Phases 1-4 aggregation-equivalence: {passed} of {total} within {TolerancePercent}% (plus the bucket asserts) ===");
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
