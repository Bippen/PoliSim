using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PRE-AUTHORING MEASUREMENT for "Italy debt start" - the third scenario content pass, same
    /// discipline as the two before it (measure before writing an objective number; a third drop
    /// is an acceptable outcome). Nothing here is scenario code.
    ///
    /// ⚠ CORRECTS A METHODOLOGY ERROR IN THE WAGE BOOM PASS, found while building this diagnostic:
    /// `PolicyDecision.InfrastructureSpendingChange` and its seven siblings are DEAD AS INPUTS for
    /// every seeded country. `ResolveSpendingForTurn` branches on `country.SpendingLines.Count > 0`
    /// (true for all six countries per Country.cs's own doc comment) and in that branch derives
    /// spending ENTIRELY from `ApplySpendingLineChanges`, which reads ONLY
    /// `decision.SpendingLineChanges` (a `Dictionary&lt;SpendingCategory, float&gt;`) - the legacy
    /// float fields are rebuilt as an OUTPUT for `ApplyApprovalRating` to read, never consulted as
    /// an INPUT. Wage Boom's §8 (`decision.InfrastructureSpendingChange = 15f`) was therefore a
    /// silent no-op, not a real stimulus test - see the correction note this pass adds to that
    /// record. This diagnostic uses the correct field, `SpendingLineChanges`.
    /// </summary>
    public static class ItalyDebtMeasurementDiagnostic
    {
        [MenuItem("PoliSim/Run Italy Debt Measurement (pre-authoring)")]
        private static void RunFromMenu() => Run();

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1 (2026-08-25): this advances turns; a measurement taken while the model's self-audit fails is meaningless, so an ATTRIB during it exits nonzero even though this tool exits 0 by design otherwise.
            Debug.Log("ITALYDEBT: --- §1 no-policy baseline: Italy at 165% debt-to-GDP (up from seed 138%) ---");
            RunTrajectory(startDebtToGdpPercent: 165f, spendingCutPercent: 0f, vatHikePoints: 0f, turns: 30, label: "italy_165pct_nopolicy");

            Debug.Log("ITALYDEBT: --- §2 does a ONE-TIME spending cut (persistent, correct field) move the debt path? ---");
            RunTrajectory(startDebtToGdpPercent: 165f, spendingCutPercent: -10f, vatHikePoints: 0f, turns: 30, label: "italy_165pct_cut10");
            RunTrajectory(startDebtToGdpPercent: 165f, spendingCutPercent: -20f, vatHikePoints: 0f, turns: 30, label: "italy_165pct_cut20");
            RunTrajectory(startDebtToGdpPercent: 165f, spendingCutPercent: -30f, vatHikePoints: 0f, turns: 30, label: "italy_165pct_cut30");

            Debug.Log("ITALYDEBT: --- §3 does a VAT hike (one-time, absolute target) move the debt path? ---");
            RunTrajectory(startDebtToGdpPercent: 165f, spendingCutPercent: 0f, vatHikePoints: 3f, turns: 30, label: "italy_165pct_vat25");
            RunTrajectory(startDebtToGdpPercent: 165f, spendingCutPercent: 0f, vatHikePoints: 6f, turns: 30, label: "italy_165pct_vat28");

            Debug.Log("ITALYDEBT: --- §4 combined (a realistic consolidation package) ---");
            RunTrajectory(startDebtToGdpPercent: 165f, spendingCutPercent: -20f, vatHikePoints: 3f, turns: 30, label: "italy_165pct_cut20_vat25");

            CheckExit.Finish(0);
        }

        private static void RunTrajectory(float startDebtToGdpPercent, float spendingCutPercent, float vatHikePoints, int turns, string label)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"ITALYDEBT_{label}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(CountryId.Italy);
                c.State.GovernmentDebt = startDebtToGdpPercent / 100f * c.State.GDP;

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                float startVat = FindVatRate(c);
                Debug.Log($"ITALYDEBT[{label}]: Italy start DebtToGdp={c.State.DebtToGdpRatio:F1}% VAT={startVat:F1}% " +
                          $"Approval={c.State.ApprovalRating:F1} U={c.State.Unemployment:F2} rate={c.CurrencyZone.InterestRate:F2}%");

                for (int turn = 1; turn <= turns; turn++)
                {
                    PolicyDecision d = PolicyDecision.None();
                    if (turn == 1)
                    {
                        if (spendingCutPercent != 0f)
                        {
                            d.SpendingLineChanges[SpendingCategory.InfrastructureAndDevelopment] = spendingCutPercent;
                            d.SpendingLineChanges[SpendingCategory.PublicServices] = spendingCutPercent;
                            d.SpendingLineChanges[SpendingCategory.Administration] = spendingCutPercent;
                        }
                        if (vatHikePoints != 0f)
                        {
                            d.TaxRateOverrides[TaxType.VAT] = startVat + vatHikePoints;
                        }
                    }
                    decisions[CountryId.Italy] = d;

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    if (turn <= 5 || turn % 5 == 0 || turn == turns)
                    {
                        EconomyState s = c.State;
                        Debug.Log($"ITALYDEBT[{label}] t{turn}: DebtToGdp={s.DebtToGdpRatio:F2}% Debt={s.GovernmentDebt:F1} " +
                                  $"Approval={s.ApprovalRating:F2} U={s.Unemployment:F3} pi={s.Inflation:F2} " +
                                  $"rate={c.CurrencyZone.InterestRate:F3} GDP={s.GDP:F1} VAT={FindVatRate(c):F1} " +
                                  $"EffRate={c.EffectiveDebtInterestRate:F3}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static float FindVatRate(Country country)
        {
            foreach (TaxLine line in country.TaxLines)
            {
                if (line.Type == TaxType.VAT) { return line.Rate; }
            }
            return -1f;
        }
    }
}
