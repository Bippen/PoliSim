using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// RECALIBRATION PASS MEASUREMENT TOOL (build-order item 1, 2026-08-26). Measures — rather than
    /// hand-derives — each country's seed-state fiscal anatomy and its first three turns' real
    /// flows, so the recalibration's proposed seed pairs are argued from the simulation's own
    /// numbers. The item-4 finding this pass exists to resolve (Sweden's ~32%-of-GDP year-1
    /// structural surplus) is re-measured here before anything is proposed, per "verified, not
    /// inherited".
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.FiscalRecalDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// Reads only public API (Country/TaxLine/SpendingLine/FiscalTurnReport); nothing is
    /// instrumented. PRIMARY BALANCE is reconstructed as BudgetBalance + InterestOnDebt — exact,
    /// because interest is one of the six spending terms the balance subtracts. The implied FRF
    /// multiplier is reconstructed as Revenue / (theoretical × CE + SwfReturns); exact by the same
    /// recorded-not-recomputed identity, modulo the Finance minister competence bias (none is
    /// appointed in a fresh world, so the term is zero here).
    /// </summary>
    public static class FiscalRecalDiagnostic
    {
        private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        private const int Turns = 3;

        private static float TheoreticalRevenue(Country c)
        {
            float revenue = 0f;
            foreach (TaxLine line in c.TaxLines)
            {
                if (!line.IsImplemented || line.Type == TaxType.Tariffs) { continue; }
                revenue += TaxBases.Revenue(c, line);   // P5-B3: the turn's accessor
            }

            return revenue;
        }

        private static float LineTotal(Country c, bool mandatory)
        {
            float total = 0f;
            foreach (SpendingLine line in c.SpendingLines)
            {
                if (line.IsMandatory == mandatory) { total += line.Amount; }
            }

            return total;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            SimulationRandom.Seed(777);

            var go = new GameObject("FiscalRecalDiagnostic");
            World world = WorldFactory.CreateDefault();
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);

            // SELF-TEST FIRST: seeded state a broken run is distinguishable from. Sweden must read
            // 35% debt on GDP 620 or everything below is void.
            Country swedenProbe = world.Countries.Find(c => c.Id == CountryId.Sweden);
            Debug.Log($"SELFTEST sweden gdp={swedenProbe.State.GDP.ToString("F1", Inv)} debtToGdp={swedenProbe.State.DebtToGdpRatio.ToString("F1", Inv)} -> " +
                (Mathf.Abs(swedenProbe.State.GDP - 620f) < 0.5f ? "OK" : "BROKEN - VOID"));

            Debug.Log("=== SEED ANATOMY (percent of GDP unless stated) ===");
            foreach (Country c in world.Countries)
            {
                float gdp = c.State.GDP;
                float theo = TheoreticalRevenue(c);
                float target = theo * c.CollectionEfficiency;
                float disc = LineTotal(c, mandatory: false);
                float mand = LineTotal(c, mandatory: true);
                float legacyG = c.SpendingLines.Count > 0 ? -1f : gdp * (c.GovernmentSpendingRate / 100f);
                float benefits = c.BenefitRatePerUnemployed * c.State.Unemployment / 100f * gdp;
                float swf = c.SovereignWealthFund != null ? c.SovereignWealthFund.TotalAssets : 0f;
                Debug.Log(string.Join(" ", new[]
                {
                    $"SEED {c.Name,-14}",
                    $"gdp={gdp.ToString("F0", Inv)}",
                    $"theoRev%={(theo / gdp * 100f).ToString("F2", Inv)}",
                    $"CE={c.CollectionEfficiency.ToString("F4", Inv)}",
                    $"revTarget%={(target / gdp * 100f).ToString("F2", Inv)}",
                    $"spendRate%={c.GovernmentSpendingRate.ToString("F1", Inv)}",
                    $"discLines%={(disc / gdp * 100f).ToString("F2", Inv)}",
                    $"mandLines%={(mand / gdp * 100f).ToString("F2", Inv)}",
                    $"legacyG%={(legacyG < 0f ? "n/a(lines)" : (legacyG / gdp * 100f).ToString("F2", Inv))}",
                    $"benefits%={(benefits / gdp * 100f).ToString("F2", Inv)}",
                    $"debt%={c.State.DebtToGdpRatio.ToString("F1", Inv)}",
                    $"swfAssets={swf.ToString("F0", Inv)}",
                }));
            }

            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

            for (int turn = 1; turn <= Turns; turn++)
            {
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);

                Debug.Log($"=== TURN {turn} FLOWS (annual accruals; percent of end-of-turn GDP) ===");
                foreach (Country c in world.Countries)
                {
                    FiscalTurnReport r = sim.GetLastFiscalReport(c.Id);
                    if (r == null) { Debug.LogError($"TURN {turn} {c.Name}: NO FISCAL REPORT"); continue; }

                    float gdp = c.State.GDP;
                    float primary = r.BudgetBalance + r.InterestOnDebt;
                    float theo = TheoreticalRevenue(c);
                    // Pass 5: the tariff flow sits inside the multiplier's bracket too - without it the
                    // reconstructed FRF overstated by take/(theo x CE) (Sweden +0.39%).
                    float denom = theo * c.CollectionEfficiency + r.SwfReturns + r.TariffRevenue;
                    float impliedMult = denom > 0.0001f ? r.Revenue / denom : -1f;
                    Debug.Log(string.Join(" ", new[]
                    {
                        $"T{turn} {c.Name,-14}",
                        $"gdp={gdp.ToString("F0", Inv)}",
                        $"rev%={(r.Revenue / gdp * 100f).ToString("F2", Inv)}",
                        $"spend%={(r.TotalSpending / gdp * 100f).ToString("F2", Inv)}",
                        $"bal%={(r.BudgetBalance / gdp * 100f).ToString("F2", Inv)}",
                        $"PRIMARY%={(primary / gdp * 100f).ToString("F2", Inv)}",
                        $"interest%={(r.InterestOnDebt / gdp * 100f).ToString("F2", Inv)}",
                        $"mand%={(r.MandatorySpending / gdp * 100f).ToString("F2", Inv)}",
                        $"benefits%={(r.UnemploymentBenefitCost / gdp * 100f).ToString("F2", Inv)}",
                        $"welfare%={(r.WelfareCost / gdp * 100f).ToString("F2", Inv)}",
                        $"swfContrib%={(r.SwfContribution / gdp * 100f).ToString("F2", Inv)}",
                        // NOTE (first-run correction): FiscalTurnReport.SwfReturns is the accrued
                        // REALISED market return (fund-side only, can be negative) - NOT the 3%/yr
                        // structural draw, which rides inside Revenue and is not separately
                        // reported. The first run's column read "swfDraw%" and was mislabeled.
                        $"swfRealisedRet%={(r.SwfReturns / gdp * 100f).ToString("F2", Inv)}",
                        $"impliedFRF={impliedMult.ToString("F3", Inv)}",
                        $"debt%={c.State.DebtToGdpRatio.ToString("F1", Inv)}",
                    }));
                }
            }

            Debug.Log("=== FISCAL RECAL DIAGNOSTIC COMPLETE ===");
            CheckExit.Finish(0);
        }
    }
}
