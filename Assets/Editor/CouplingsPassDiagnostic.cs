using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// THE COUPLINGS PASS MEASUREMENT (build-order item 2, 2026-08-26): the erosion-standard
    /// decomposition of the two new edge chains, measured on real laws through the real enact path
    /// (SimulationManager's own private ApplyLawBillEffects by reflection - the
    /// LawCompositionDiagnostic idiom, never a reimplementation).
    ///
    /// Two same-seed USA runs, 30 turns: a no-law control, and a sentencing-heavy line (Truth in
    /// Sentencing + Three Strikes, the two laws whose displays the pass moves most). The chains
    /// under measurement: (1) sentencing dial -&gt; prison TARGET (+S x gap) -&gt; the stock filling on
    /// its 0.15/turn reversion (never instantly - the honest lag is the claim); (2) the dial +
    /// prison gaps -&gt; the line-resident enforcement cost on the Justice line -&gt; G and
    /// totalSpending -&gt; budgetBalance -&gt; the debt path, reported against the control rather than
    /// assumed small. Per-turn rows carry the decomposition inputs (dials, prison rate and its
    /// recomputed target, the Justice line amount, the applied-cost tracker, the fiscal report's
    /// own recorded figures) so every step of both chains is checkable against the constants.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.CouplingsPassDiagnostic.Run -logFile &lt;path&gt;`
    /// </summary>
    public static class CouplingsPassDiagnostic
    {
        private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        private const int Turns = 30;
        private static readonly string[] Line = { "truth_in_sentencing_act", "three_strikes_law" };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            // The pane's derived display for the headline law, from the table itself - the
            // before/after the record quotes (the "visible deliverable" in log form; the captures
            // carry it visually).
            LawDefinition tis = LawCatalog.GetById("truth_in_sentencing_act");
            if (tis != null)
            {
                foreach (CrimeJusticeCouplings.LawEffectLine effect in CrimeJusticeCouplings.AggregateLawEffects(tis))
                {
                    Debug.Log($"COUPLINGS pane[truth_in_sentencing_act]: {CrimeJusticeCouplings.DisplayName(effect.Stat)}: " +
                        $"{effect.Amount.ToString("+0.000;-0.000", Inv)} {CrimeJusticeCouplings.Unit(effect.Stat)}{(effect.Contested ? " (contested)" : "")}");
                }
            }

            float[] ctrl = RunLine(enact: false);
            float[] laws = RunLine(enact: true);

            Debug.Log($"COUPLINGS DECOMP t{Turns}: debt% ctrl={ctrl[0].ToString("F2", Inv)} laws={laws[0].ToString("F2", Inv)} " +
                $"delta={(laws[0] - ctrl[0]).ToString("F2", Inv)} | prison ctrl={ctrl[1].ToString("F1", Inv)} laws={laws[1].ToString("F1", Inv)} " +
                $"| justiceLine ctrl={ctrl[2].ToString("F2", Inv)} laws={laws[2].ToString("F2", Inv)}");
            Debug.Log("=== COUPLINGS PASS DIAGNOSTIC COMPLETE ===");
            CheckExit.Finish(0);
        }

        private static float[] RunLine(bool enact)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"COUPLINGS_{(enact ? "laws" : "ctrl")}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country usa = world.GetCountry(CountryId.USA);
                string label = enact ? "laws" : "ctrl";

                if (enact)
                {
                    foreach (string id in Line)
                    {
                        if (LawCatalog.GetById(id) == null)
                        {
                            Debug.LogError($"COUPLINGS: '{id}' not in LawCatalog - the line list is stale.");
                            continue;
                        }

                        ApplyLawBillEffects(sim, usa, new LawBill { LawId = id, IsRepeal = false });
                    }

                    Debug.Log($"COUPLINGS[{label}]: enacted {string.Join(", ", Line)} - dials now " +
                        $"sent={usa.SentencingSeverity.ToString("F0", Inv)} bail={usa.BailReformLevel.ToString("F0", Inv)} " +
                        $"police={usa.PoliceFundingLevel.ToString("F0", Inv)} drug={usa.DrugPolicyLevel.ToString("F0", Inv)} " +
                        $"judicial={usa.JudicialFundingLevel.ToString("F0", Inv)} border={usa.BorderEnforcementLevel.ToString("F0", Inv)}");
                }

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                for (int turn = 1; turn <= Turns; turn++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    if (turn <= 5 || turn % 5 == 0)
                    {
                        FiscalTurnReport r = sim.GetLastFiscalReport(usa.Id);
                        float gdp = usa.State.GDP;
                        // The prison TARGET recomputed from the table's own constants - the
                        // decomposition row a reader checks the stock's approach against.
                        float prisonTarget = usa.BaselinePrisonPopulationRate
                            - CrimeJusticeCouplings.BailReformPrisonPopulationSensitivity * (usa.BailReformLevel - CrimeJusticeCouplings.NeutralDialLevel)
                            + CrimeJusticeCouplings.DrugPolicyPrisonPopulationSensitivity * (usa.DrugPolicyLevel - CrimeJusticeCouplings.NeutralDialLevel)
                            - CrimeJusticeCouplings.JudicialFundingPrisonPopulationSensitivity * (usa.JudicialFundingLevel - CrimeJusticeCouplings.NeutralDialLevel)
                            + CrimeJusticeCouplings.SentencingPrisonPopulationSensitivity * (usa.SentencingSeverity - CrimeJusticeCouplings.NeutralDialLevel);
                        float justiceLine = 0f;
                        foreach (SpendingLine l in usa.SpendingLines)
                        {
                            if (l.Category == SpendingCategory.Justice) { justiceLine = l.Amount; }
                        }

                        Debug.Log(string.Join(" ", new[]
                        {
                            $"COUPLINGS[{label}] t{turn}:",
                            $"prison={usa.State.PrisonPopulationRate.ToString("F1", Inv)}",
                            $"target={prisonTarget.ToString("F1", Inv)}",
                            $"justiceLine={justiceLine.ToString("F2", Inv)}",
                            $"appliedCost={usa.AppliedJusticeEnforcementCost.ToString("F2", Inv)}",
                            $"rev%={(r.Revenue / gdp * 100f).ToString("F2", Inv)}",
                            $"spend%={(r.TotalSpending / gdp * 100f).ToString("F2", Inv)}",
                            $"bal%={(r.BudgetBalance / gdp * 100f).ToString("F2", Inv)}",
                            $"debt%={usa.State.DebtToGdpRatio.ToString("F1", Inv)}",
                        }));
                    }
                }

                float finalJusticeLine = 0f;
                foreach (SpendingLine l in usa.SpendingLines)
                {
                    if (l.Category == SpendingCategory.Justice) { finalJusticeLine = l.Amount; }
                }

                return new[] { usa.State.DebtToGdpRatio, usa.State.PrisonPopulationRate, finalJusticeLine };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>The LawCompositionDiagnostic idiom: the REAL private enact path by reflection.</summary>
        private static void ApplyLawBillEffects(SimulationManager sim, Country country, LawBill bill)
        {
            var method = typeof(SimulationManager).GetMethod("ApplyLawBillEffects",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("COUPLINGS: SimulationManager.ApplyLawBillEffects not found by reflection - renamed?");
            }

            method.Invoke(sim, new object[] { country, bill });
        }
    }
}
